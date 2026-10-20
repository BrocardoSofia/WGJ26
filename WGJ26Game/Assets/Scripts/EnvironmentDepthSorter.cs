using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Changes only the drawing order of grass and stones according to their
/// position on the Z axis relative to the player.
/// </summary>
public sealed class EnvironmentDepthSorter : MonoBehaviour
{
    private sealed class SortableItem
    {
        public Transform DepthReference;
        public SpriteRenderer[] Renderers;
        public bool WasInFrontOfPlayer;
        public bool HasPreviousState;
    }

    private readonly List<SortableItem> items = new();
    private Transform player;
    private SpriteRenderer playerRenderer;
    private int lastPlayerOrder = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "2.Nivel")
            return;

        GameObject manager = new GameObject("EnvironmentDepthSorter_Runtime");
        manager.AddComponent<EnvironmentDepthSorter>();
    }

    private void Awake()
    {
        GameObject playerObject = GameObject.Find("Player");
        GameObject environmentObject = GameObject.Find("Ambiente");
        if (playerObject == null || environmentObject == null)
        {
            enabled = false;
            return;
        }

        player = playerObject.transform;
        Transform playerVisual = player.Find("PlayerVisual");
        playerRenderer = playerVisual != null
            ? playerVisual.GetComponent<SpriteRenderer>()
            : player.GetComponentInChildren<SpriteRenderer>(true);

        RegisterChildren(environmentObject.transform.Find("Pastos"));
        RegisterChildren(environmentObject.transform.Find("Piedras"));
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        int playerOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;
        bool playerOrderChanged = playerOrder != lastPlayerOrder;
        foreach (SortableItem item in items)
        {
            if (item.DepthReference == null)
                continue;

            // The camera looks toward positive Z: a greater Z means that the
            // player is behind the environmental object.
            bool objectInFrontOfPlayer = player.position.z > item.DepthReference.position.z;
            if (!playerOrderChanged && item.HasPreviousState && item.WasInFrontOfPlayer == objectInFrontOfPlayer)
                continue;

            int sortingOrder = objectInFrontOfPlayer ? playerOrder + 1 : playerOrder - 1;
            foreach (SpriteRenderer spriteRenderer in item.Renderers)
            {
                if (spriteRenderer != null)
                    spriteRenderer.sortingOrder = sortingOrder;
            }

            item.WasInFrontOfPlayer = objectInFrontOfPlayer;
            item.HasPreviousState = true;
        }

        lastPlayerOrder = playerOrder;
    }

    private void RegisterChildren(Transform group)
    {
        if (group == null)
            return;

        foreach (Transform child in group)
        {
            SpriteRenderer[] renderers = child.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
                continue;

            items.Add(new SortableItem
            {
                DepthReference = child,
                Renderers = renderers
            });
        }
    }
}
