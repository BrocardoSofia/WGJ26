using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWalkAnimation : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField, InspectorName("Imagen quieto")]
    private Sprite stillSprite;

    [SerializeField, InspectorName("Frames de caminata")]
    private Sprite[] walkFrames;

    [Header("Velocidad de la animación (frames por segundo)")]
    [SerializeField, Min(0.1f), InspectorName("Caminata normal")]
    private float normalWalkFps = 8f;

    [SerializeField, Min(0.1f), InspectorName("Con 1 café")]
    private float oneCoffeeWalkFps = 10f;

    [SerializeField, Min(0.1f), InspectorName("Con 2 cafés")]
    private float twoCoffeeWalkFps = 12f;

    [SerializeField, Min(0.1f), InspectorName("Con 3 cafés")]
    private float threeCoffeeWalkFps = 14f;

    [SerializeField, Min(0f), InspectorName("Movimiento mínimo para animar")]
    private float movementThreshold = 0.0001f;

    [Header("Audio de caminata")]
    [SerializeField, InspectorName("Sonido de pasos")]
    private AudioClip walkAudio;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual")]
    private float walkAudioVolume = 1f;

    [SerializeField, Range(0.1f, 3f), InspectorName("Velocidad normal")]
    private float normalAudioSpeed = 1f;

    [SerializeField, Range(0.1f, 3f), InspectorName("Velocidad con 1 café")]
    private float oneCoffeeAudioSpeed = 1.15f;

    [SerializeField, Range(0.1f, 3f), InspectorName("Velocidad con 2 cafés")]
    private float twoCoffeeAudioSpeed = 1.3f;

    [SerializeField, Range(0.1f, 3f), InspectorName("Velocidad con 3 cafés")]
    private float threeCoffeeAudioSpeed = 1.45f;

    private SpriteRenderer playerVisual;
    private PlayerCoffeeInventory coffeeInventory;
    private ConstantPlaneMover playerMovement;
    private AudioSource walkAudioSource;
    private Vector3 previousPosition;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        Transform visual = transform.Find("PlayerVisual");
        if (visual != null)
        {
            playerVisual = visual.GetComponent<SpriteRenderer>();
        }

        coffeeInventory = GetComponent<PlayerCoffeeInventory>();
        playerMovement = GetComponent<ConstantPlaneMover>();
        previousPosition = transform.position;

        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.playOnAwake = false;
        walkAudioSource.loop = true;
        walkAudioSource.spatialBlend = 0f;

        if (stillSprite == null && playerVisual != null)
        {
            stillSprite = playerVisual.sprite;
        }
    }

    private void LateUpdate()
    {
        if (playerVisual == null)
        {
            return;
        }

        Vector3 movement = transform.position - previousPosition;
        previousPosition = transform.position;

        bool isWalking = movement.sqrMagnitude > movementThreshold * movementThreshold;
        if (playerMovement != null && playerMovement.IsInteractingWithNpc)
        {
            isWalking = false;
        }

        UpdateWalkAudio(isWalking);

        if (!isWalking || walkFrames == null || walkFrames.Length == 0)
        {
            ShowStillSprite();
            return;
        }

        float framesPerSecond = GetCurrentFramesPerSecond();
        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.1f, framesPerSecond);

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % walkFrames.Length;
        }

        Sprite frame = walkFrames[frameIndex];
        if (frame != null)
        {
            playerVisual.sprite = frame;
        }
    }

    private float GetCurrentFramesPerSecond()
    {
        int coffeeCount = coffeeInventory != null ? coffeeInventory.CoffeeCount : 0;
        return coffeeCount switch
        {
            1 => oneCoffeeWalkFps,
            2 => twoCoffeeWalkFps,
            >= 3 => threeCoffeeWalkFps,
            _ => normalWalkFps
        };
    }

    private void UpdateWalkAudio(bool isWalking)
    {
        if (walkAudioSource == null)
        {
            return;
        }

        if (!isWalking || walkAudio == null)
        {
            if (walkAudioSource.isPlaying)
            {
                walkAudioSource.Stop();
            }

            return;
        }

        walkAudioSource.clip = walkAudio;
        walkAudioSource.pitch = GetCurrentAudioSpeed();
        walkAudioSource.volume = Mathf.Max(0f, walkAudioVolume) * AudioManager.SfxOutputVolume;

        if (!walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
    }

    private float GetCurrentAudioSpeed()
    {
        int coffeeCount = coffeeInventory != null ? coffeeInventory.CoffeeCount : 0;
        return coffeeCount switch
        {
            1 => oneCoffeeAudioSpeed,
            2 => twoCoffeeAudioSpeed,
            >= 3 => threeCoffeeAudioSpeed,
            _ => normalAudioSpeed
        };
    }

    private void ShowStillSprite()
    {
        frameTimer = 0f;
        frameIndex = 0;
        if (stillSprite != null)
        {
            playerVisual.sprite = stillSprite;
        }
    }

    private void OnValidate()
    {
        normalWalkFps = Mathf.Max(0.1f, normalWalkFps);
        oneCoffeeWalkFps = Mathf.Max(0.1f, oneCoffeeWalkFps);
        twoCoffeeWalkFps = Mathf.Max(0.1f, twoCoffeeWalkFps);
        threeCoffeeWalkFps = Mathf.Max(0.1f, threeCoffeeWalkFps);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        walkAudioVolume = Mathf.Max(0f, walkAudioVolume);
        normalAudioSpeed = Mathf.Clamp(normalAudioSpeed, 0.1f, 3f);
        oneCoffeeAudioSpeed = Mathf.Clamp(oneCoffeeAudioSpeed, 0.1f, 3f);
        twoCoffeeAudioSpeed = Mathf.Clamp(twoCoffeeAudioSpeed, 0.1f, 3f);
        threeCoffeeAudioSpeed = Mathf.Clamp(threeCoffeeAudioSpeed, 0.1f, 3f);
    }

    private void OnDisable()
    {
        if (walkAudioSource != null)
        {
            walkAudioSource.Stop();
        }
    }
}
