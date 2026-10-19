using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Vosk;
#endif

/// <summary>
/// Captures the Windows default microphone continuously and detects each
/// partial "no" without waiting for the speaker to finish the sentence.
/// </summary>
public sealed class VoskNoStreamRecognizer : IDisposable
{
    private const int SampleRate = 16000;
    private const int MicrophoneBufferSeconds = 2;
    private const int SamplesPerChunk = 512;
    private const string ModelFolderName = "vosk-model-small-es-0.42";
    private const string ModelArchiveName = ModelFolderName + ".zip";
    private const string ExtractionMarkerName = ".extraction-complete";

    private readonly ConcurrentQueue<short[]> audioChunks = new();
    private readonly ConcurrentQueue<int> detections = new();
    private readonly ConcurrentBag<short[]> samplePool = new();
    private readonly AutoResetEvent audioReady = new(false);

    private AudioClip microphoneClip;
    private float[] floatBuffer = new float[SamplesPerChunk];
    private int microphonePosition;
    private Task recognitionTask;
    private volatile bool stopRequested;
    private volatile bool modelReady;
    private volatile bool listeningRequested;
    private string initializationError;
    private bool errorReported;

    public bool IsReady => modelReady;

    public void Start()
    {
        listeningRequested = true;
        EnsureWorker();
    }

    public void Stop()
    {
        listeningRequested = false;
        StopMicrophone();
        ClearAudioQueue();
        while (detections.TryDequeue(out _)) { }
    }

    public int UpdateAndGetDetectionCount()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!string.IsNullOrEmpty(initializationError) && !errorReported)
        {
            errorReported = true;
            Debug.LogError($"No se pudo iniciar Vosk: {initializationError}");
        }

        if (listeningRequested && modelReady && microphoneClip == null)
            StartMicrophone();

        if (microphoneClip != null)
            CaptureAvailableAudio();
#endif

        int total = 0;
        while (detections.TryDequeue(out int count))
            total += count;
        return total;
    }

    public void Dispose()
    {
        listeningRequested = false;
        StopMicrophone();
        stopRequested = true;
        audioReady.Set();

        if (recognitionTask != null)
        {
            try
            {
                recognitionTask.Wait(3000);
            }
            catch (AggregateException) { }
            recognitionTask = null;
        }

        ClearAudioQueue();
        audioReady.Dispose();
    }

    private void EnsureWorker()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (recognitionTask != null)
            return;

        stopRequested = false;
        modelReady = false;
        initializationError = null;
        errorReported = false;
        string modelArchivePath = Path.Combine(
            Application.streamingAssetsPath,
            "Vosk",
            ModelArchiveName);
        string modelCacheRoot = Path.Combine(
            Application.persistentDataPath,
            "Vosk");
        recognitionTask = Task.Run(() => RecognitionLoop(modelArchivePath, modelCacheRoot));
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void RecognitionLoop(string modelArchivePath, string modelCacheRoot)
    {
        Model model = null;
        VoskRecognizer recognizer = null;

        try
        {
            Vosk.Vosk.SetLogLevel(-1);
            string modelPath = PrepareModel(modelArchivePath, modelCacheRoot);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, SampleRate, "[\"no\", \"[unk]\"]");
            modelReady = true;
            int greatestPartialCount = 0;

            while (!stopRequested)
            {
                if (!audioChunks.TryDequeue(out short[] samples))
                {
                    audioReady.WaitOne(50);
                    continue;
                }

                bool phraseFinished = recognizer.AcceptWaveform(samples, samples.Length);
                samplePool.Add(samples);

                if (phraseFinished)
                {
                    int finalCount = CountNoWords(ReadJsonString(recognizer.Result(), "text"));
                    if (finalCount > greatestPartialCount)
                        detections.Enqueue(finalCount - greatestPartialCount);
                    greatestPartialCount = 0;
                    continue;
                }

                int partialCount = CountNoWords(ReadJsonString(recognizer.PartialResult(), "partial"));
                if (partialCount > greatestPartialCount)
                {
                    detections.Enqueue(partialCount - greatestPartialCount);
                    greatestPartialCount = partialCount;
                }
            }
        }
        catch (Exception exception)
        {
            initializationError = exception.Message;
        }
        finally
        {
            modelReady = false;
            recognizer?.Dispose();
            model?.Dispose();
        }
    }

    private static string PrepareModel(string archivePath, string cacheRoot)
    {
        string modelPath = Path.Combine(cacheRoot, ModelFolderName);
        string markerPath = Path.Combine(modelPath, ExtractionMarkerName);
        if (File.Exists(markerPath))
            return modelPath;

        if (!File.Exists(archivePath))
            throw new FileNotFoundException("No se encontró el modelo de voz de Vosk.", archivePath);

        Directory.CreateDirectory(cacheRoot);
        if (Directory.Exists(modelPath))
            Directory.Delete(modelPath, true);

        ZipFile.ExtractToDirectory(archivePath, cacheRoot);
        File.WriteAllText(markerPath, ModelFolderName);
        return modelPath;
    }
#endif

    private void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            initializationError = "Windows no informa ningún micrófono disponible.";
            return;
        }

        microphoneClip = Microphone.Start(null, true, MicrophoneBufferSeconds, SampleRate);
        microphonePosition = 0;
    }

    private void StopMicrophone()
    {
        if (microphoneClip == null)
            return;

        if (Microphone.IsRecording(null))
            Microphone.End(null);
        microphoneClip = null;
        microphonePosition = 0;
    }

    private void CaptureAvailableAudio()
    {
        int currentPosition = Microphone.GetPosition(null);
        if (currentPosition < 0 || currentPosition == microphonePosition)
            return;

        int available = currentPosition >= microphonePosition
            ? currentPosition - microphonePosition
            : microphoneClip.samples - microphonePosition + currentPosition;

        while (available >= SamplesPerChunk)
        {
            ReadMicrophoneChunk(floatBuffer, microphonePosition);
            if (!samplePool.TryTake(out short[] samples))
                samples = new short[SamplesPerChunk];

            for (int index = 0; index < SamplesPerChunk; index++)
                samples[index] = (short)Mathf.Clamp(floatBuffer[index] * short.MaxValue, short.MinValue, short.MaxValue);

            audioChunks.Enqueue(samples);
            audioReady.Set();
            microphonePosition = (microphonePosition + SamplesPerChunk) % microphoneClip.samples;
            available -= SamplesPerChunk;
        }
    }

    private void ReadMicrophoneChunk(float[] destination, int startPosition)
    {
        int samplesUntilEnd = microphoneClip.samples - startPosition;
        if (samplesUntilEnd >= SamplesPerChunk)
        {
            microphoneClip.GetData(destination, startPosition);
            return;
        }

        float[] tail = new float[samplesUntilEnd];
        float[] head = new float[SamplesPerChunk - samplesUntilEnd];
        microphoneClip.GetData(tail, startPosition);
        microphoneClip.GetData(head, 0);
        Array.Copy(tail, 0, destination, 0, tail.Length);
        Array.Copy(head, 0, destination, tail.Length, head.Length);
    }

    private void ClearAudioQueue()
    {
        while (audioChunks.TryDequeue(out short[] samples))
            samplePool.Add(samples);
    }

    private static string ReadJsonString(string json, string property)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        string marker = $"\"{property}\"";
        int propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
        if (propertyIndex < 0)
            return string.Empty;

        int colonIndex = json.IndexOf(':', propertyIndex + marker.Length);
        int openingQuote = colonIndex >= 0 ? json.IndexOf('"', colonIndex + 1) : -1;
        int closingQuote = openingQuote >= 0 ? json.IndexOf('"', openingQuote + 1) : -1;
        return openingQuote >= 0 && closingQuote > openingQuote
            ? json.Substring(openingQuote + 1, closingQuote - openingQuote - 1)
            : string.Empty;
    }

    private static int CountNoWords(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return 0;

        int count = 0;
        string[] words = phrase.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string word in words)
        {
            if (string.Equals(word, "no", StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }
}
