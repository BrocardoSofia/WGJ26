using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NpcIdleAnimationGroup : MonoBehaviour
{
    [Header("Animación idle compartida")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;

    private readonly List<SpriteRenderer> npcRenderers = new();
    private int currentFrame;
    private float frameTimer;

    private void Awake()
    {
        CollectNpcRenderers();
        ApplyCurrentFrame();
    }

    private void Update()
    {
        if (idleFrames == null || idleFrames.Length < 2 || npcRenderers.Count == 0)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.1f, framesPerSecond);

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % idleFrames.Length;
            ApplyCurrentFrame();
        }
    }

    private void CollectNpcRenderers()
    {
        npcRenderers.Clear();

        foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if ((candidate.name == "NPCVisual" || candidate.name == "visual")
                && !npcRenderers.Contains(candidate))
            {
                npcRenderers.Add(candidate);
            }
        }
    }

    private void ApplyCurrentFrame()
    {
        if (idleFrames == null || idleFrames.Length == 0)
            return;

        Sprite frame = idleFrames[Mathf.Clamp(currentFrame, 0, idleFrames.Length - 1)];
        if (frame == null)
            return;

        for (int index = npcRenderers.Count - 1; index >= 0; index--)
        {
            SpriteRenderer npcRenderer = npcRenderers[index];
            if (npcRenderer == null)
            {
                npcRenderers.RemoveAt(index);
                continue;
            }

            npcRenderer.sprite = frame;
        }
    }

    private void OnValidate()
    {
        framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
    }
}
