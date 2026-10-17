using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameLanguage
{
    Spanish = 0,
    English = 1
}

public static class GameLocalization
{
    private const string LanguageKey = "settings.language";

    public static event Action LanguageChanged;

    public static GameLanguage CurrentLanguage =>
        (GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.Spanish);

    public static bool IsSpanish => CurrentLanguage == GameLanguage.Spanish;

    public static void SetLanguage(GameLanguage language)
    {
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LocalizeAllLoadedTexts();
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        return IsSpanish ? GetSpanish(key) : GetEnglish(key);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LocalizeAfterSceneLoad()
    {
        LocalizeAllLoadedTexts();
    }

    public static void LocalizeAllLoadedTexts()
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                    text.text = TranslateKnownText(text.text);
            }
        }
    }

    private static string TranslateKnownText(string value)
    {
        return value switch
        {
            "Iniciar juego" or "Start game" => Get("menu.start"),
            "Configuraciones" or "Settings" => Get("menu.settings"),
            "Créditos" or "Credits" => Get("menu.credits"),
            "Salir" or "Exit" => Get("menu.exit"),
            "Reconocimiento de voz" or "Voice recognition" => Get("voice.title"),
            "Cambiar micrófono en Windows" or "Change microphone in Windows" => Get("settings.changeMicrophone"),
            "MODO VOZ" or "VOICE MODE" => Get("voice.modeVoice"),
            "MODO TECLADO" or "KEYBOARD MODE" => Get("voice.modeKeyboard"),
            "Volver a intentar" or "Try again" => Get("game.retry"),
            "Volver al menú" or "Return to menu" => Get("game.menu"),
            "GANASTE" or "YOU WIN" => Get("game.win"),
            "Ir a créditos" or "Go to credits" => Get("game.credits"),
            "Volver a jugar" or "Play again" => Get("game.playAgain"),
            "Control por voz (desactivar: teclado)" or "Voice control (disable: keyboard)" => Get("voice.legacyControl"),
            "GAME OVER\nPor sobredosis de café" or "GAME OVER\nCoffee overdose" => Get("game.coffeeOver"),
            "GAME OVER\nNo llegaste a tiempo al trabajo" or "GAME OVER\nYou did not get to work on time" => Get("game.timeOver"),
            _ => value
        };
    }

    private static string GetSpanish(string key)
    {
        return key switch
        {
            "menu.start" => "Iniciar juego",
            "menu.settings" => "Configuraciones",
            "menu.credits" => "Créditos",
            "menu.exit" => "Salir",
            "settings.title" => "CONFIGURACIONES",
            "settings.microphone" => "Micrófono predeterminado de Windows: {0}",
            "settings.noMicrophone" => "No se detectó ningún micrófono",
            "settings.changeMicrophone" => "Cambiar micrófono en Windows",
            "settings.control" => "Control",
            "settings.voice" => "Micrófono",
            "settings.keyboard" => "Barra espaciadora",
            "settings.music" => "Volumen de música",
            "settings.effects" => "Volumen de efectos",
            "settings.resolution" => "Resolución",
            "settings.fullscreen" => "Pantalla completa",
            "settings.language" => "Idioma",
            "settings.spanish" => "Español",
            "settings.english" => "Inglés",
            "voice.title" => "Reconocimiento de voz",
            "voice.defaultMicrophone" => "Micrófono: entrada predeterminada de Windows",
            "voice.pausedMicrophone" => "Micrófono: predeterminado de Windows (pausado)",
            "voice.unavailable" => "Reconocimiento disponible solamente en Windows.",
            "voice.startError" => "No se pudo iniciar Windows Speech.",
            "voice.detectedOne" => "Detectado: No",
            "voice.detectedMany" => "Detectados: {0} × No",
            "voice.modeVoice" => "MODO VOZ",
            "voice.modeKeyboard" => "MODO TECLADO",
            "voice.legacyControl" => "Control por voz (desactivar: teclado)",
            "game.retry" => "Volver a intentar",
            "game.menu" => "Volver al menú",
            "game.win" => "GANASTE",
            "game.credits" => "Ir a créditos",
            "game.playAgain" => "Volver a jugar",
            "game.coffeeOver" => "GAME OVER\nPor sobredosis de café",
            "game.timeOver" => "GAME OVER\nNo llegaste a tiempo al trabajo",
            "game.goal" => "Meta: {0:0.0} m",
            "game.time" => "Tiempo: {0:00}:{1:00}",
            _ => key
        };
    }

    private static string GetEnglish(string key)
    {
        return key switch
        {
            "menu.start" => "Start game",
            "menu.settings" => "Settings",
            "menu.credits" => "Credits",
            "menu.exit" => "Exit",
            "settings.title" => "SETTINGS",
            "settings.microphone" => "Windows default microphone: {0}",
            "settings.noMicrophone" => "No microphone detected",
            "settings.changeMicrophone" => "Change microphone in Windows",
            "settings.control" => "Control",
            "settings.voice" => "Microphone",
            "settings.keyboard" => "Space bar",
            "settings.music" => "Music volume",
            "settings.effects" => "Effects volume",
            "settings.resolution" => "Resolution",
            "settings.fullscreen" => "Fullscreen",
            "settings.language" => "Language",
            "settings.spanish" => "Spanish",
            "settings.english" => "English",
            "voice.title" => "Voice recognition",
            "voice.defaultMicrophone" => "Microphone: Windows default input",
            "voice.pausedMicrophone" => "Microphone: Windows default input (paused)",
            "voice.unavailable" => "Voice recognition is only available on Windows.",
            "voice.startError" => "Windows Speech could not be started.",
            "voice.detectedOne" => "Detected: No",
            "voice.detectedMany" => "Detected: {0} × No",
            "voice.modeVoice" => "VOICE MODE",
            "voice.modeKeyboard" => "KEYBOARD MODE",
            "voice.legacyControl" => "Voice control (disable: keyboard)",
            "game.retry" => "Try again",
            "game.menu" => "Return to menu",
            "game.win" => "YOU WIN",
            "game.credits" => "Go to credits",
            "game.playAgain" => "Play again",
            "game.coffeeOver" => "GAME OVER\nCoffee overdose",
            "game.timeOver" => "GAME OVER\nYou did not get to work on time",
            "game.goal" => "Goal: {0:0.0} m",
            "game.time" => "Time: {0:00}:{1:00}",
            _ => key
        };
    }
}
