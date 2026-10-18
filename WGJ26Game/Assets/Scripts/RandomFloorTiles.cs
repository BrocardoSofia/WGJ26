using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws a random, gapless grid of sprites over the existing floor.
/// Tiles using the same sprite share one mesh to keep the floor lightweight.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RandomFloorTiles : MonoBehaviour
{
    private const string GeneratedRootName = "FloorTiles_Runtime";
    private const int MaximumTileCount = 25000;

    [Header("Tiles del piso")]
    [SerializeField, InspectorName("Variantes")]
    private Sprite[] tileVariants = Array.Empty<Sprite>();

    [SerializeField, Min(0.1f), InspectorName("Tamaño del tile")]
    private float tileSize = 5f;

    [SerializeField, InspectorName("Semilla aleatoria")]
    private int randomSeed = 2601;

    [SerializeField, Min(0f), InspectorName("Separación sobre el piso")]
    private float surfaceOffset = 0.01f;

    private int lastConfigurationHash;

    private void OnEnable() => RebuildIfNeeded(true);

    private void OnDisable() => ClearGeneratedTiles();

    private void OnValidate()
    {
        tileSize = Mathf.Max(0.1f, tileSize);
        RebuildIfNeeded(true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RebuildIfNeeded(false);
        }
    }

    [ContextMenu("Regenerar distribución")]
    private void RegenerateTiles()
    {
        randomSeed++;
        RebuildIfNeeded(true);
    }

    private void RebuildIfNeeded(bool force)
    {
        MeshFilter floorFilter = GetComponent<MeshFilter>();
        if (floorFilter == null || floorFilter.sharedMesh == null)
        {
            return;
        }

        Bounds bounds = floorFilter.sharedMesh.bounds;
        int hash = CalculateConfigurationHash(bounds);
        if (!force && hash == lastConfigurationHash && transform.Find(GeneratedRootName) != null)
        {
            return;
        }

        lastConfigurationHash = hash;
        ClearGeneratedTiles();

        Sprite[] variants = GetValidVariants();
        if (variants.Length == 0)
        {
            return;
        }

        float worldWidth = Mathf.Abs(bounds.size.x * transform.lossyScale.x);
        float worldDepth = Mathf.Abs(bounds.size.z * transform.lossyScale.z);
        int columns = Mathf.Max(1, Mathf.CeilToInt(worldWidth / tileSize));
        int rows = Mathf.Max(1, Mathf.CeilToInt(worldDepth / tileSize));
        LimitGridSize(ref columns, ref rows, worldWidth, worldDepth);

        GameObject root = new GameObject(GeneratedRootName);
        root.hideFlags = Application.isPlaying ? HideFlags.DontSave : HideFlags.HideAndDontSave;
        root.transform.SetParent(transform, false);

        var positions = new List<Vector2Int>[variants.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = new List<Vector2Int>();
        }

        System.Random random = new System.Random(randomSeed);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                positions[random.Next(variants.Length)].Add(new Vector2Int(column, row));
            }
        }

        float cellWidth = bounds.size.x / columns;
        float cellDepth = bounds.size.z / rows;
        float floorY = bounds.max.y + surfaceOffset;

        for (int i = 0; i < variants.Length; i++)
        {
            if (positions[i].Count > 0)
            {
                CreateVariantMesh(root.transform, variants[i], positions[i], bounds,
                    cellWidth, cellDepth, floorY, i);
            }
        }
    }

    private static void LimitGridSize(
        ref int columns, ref int rows, float worldWidth, float worldDepth)
    {
        long requestedCount = (long)columns * rows;
        if (requestedCount <= MaximumTileCount)
        {
            return;
        }

        float aspect = worldDepth > 0f ? worldWidth / worldDepth : 1f;
        columns = Mathf.Max(1, Mathf.FloorToInt(Mathf.Sqrt(MaximumTileCount * aspect)));
        rows = Mathf.Max(1, Mathf.FloorToInt((float)MaximumTileCount / columns));
        Debug.LogWarning(
            $"El tamaño solicitado generaba {requestedCount} tiles. " +
            $"La grilla se limitó a {columns * rows} para evitar bloquear Unity.");
    }

    private static void CreateVariantMesh(
        Transform parent,
        Sprite sprite,
        List<Vector2Int> positions,
        Bounds floorBounds,
        float cellWidth,
        float cellDepth,
        float floorY,
        int variantIndex)
    {
        int count = positions.Count;
        var vertices = new Vector3[count * 4];
        var uv = new Vector2[count * 4];
        var triangles = new int[count * 6];

        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;
        float uMin = rect.xMin / texture.width;
        float uMax = rect.xMax / texture.width;
        float vMin = rect.yMin / texture.height;
        float vMax = rect.yMax / texture.height;

        for (int i = 0; i < count; i++)
        {
            float xMin = floorBounds.min.x + positions[i].x * cellWidth;
            float xMax = xMin + cellWidth;
            float zMin = floorBounds.min.z + positions[i].y * cellDepth;
            float zMax = zMin + cellDepth;
            int vertex = i * 4;
            int triangle = i * 6;

            vertices[vertex] = new Vector3(xMin, floorY, zMin);
            vertices[vertex + 1] = new Vector3(xMin, floorY, zMax);
            vertices[vertex + 2] = new Vector3(xMax, floorY, zMax);
            vertices[vertex + 3] = new Vector3(xMax, floorY, zMin);

            uv[vertex] = new Vector2(uMin, vMin);
            uv[vertex + 1] = new Vector2(uMin, vMax);
            uv[vertex + 2] = new Vector2(uMax, vMax);
            uv[vertex + 3] = new Vector2(uMax, vMin);

            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        Mesh mesh = new Mesh
        {
            name = $"FloorTiles_Mesh_{variantIndex}",
            indexFormat = IndexFormat.UInt32,
            hideFlags = HideFlags.HideAndDontSave
        };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        GameObject meshObject = new GameObject($"Variante_{variantIndex + 1}");
        meshObject.hideFlags = parent.gameObject.hideFlags;
        meshObject.transform.SetParent(parent, false);
        meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader)
        {
            name = $"FloorTiles_Material_{variantIndex}",
            mainTexture = sprite.texture,
            hideFlags = HideFlags.HideAndDontSave
        };

        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Sprite[] GetValidVariants()
    {
        if (tileVariants == null || tileVariants.Length == 0)
        {
            return Array.Empty<Sprite>();
        }

        var variants = new List<Sprite>(tileVariants.Length);
        for (int i = 0; i < tileVariants.Length; i++)
        {
            if (tileVariants[i] != null)
            {
                variants.Add(tileVariants[i]);
            }
        }

        return variants.ToArray();
    }

    private int CalculateConfigurationHash(Bounds bounds)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + tileSize.GetHashCode();
            hash = hash * 31 + randomSeed;
            hash = hash * 31 + surfaceOffset.GetHashCode();
            hash = hash * 31 + bounds.GetHashCode();
            hash = hash * 31 + transform.lossyScale.GetHashCode();

            if (tileVariants != null)
            {
                for (int i = 0; i < tileVariants.Length; i++)
                {
                    hash = hash * 31 + (tileVariants[i] != null
                        ? tileVariants[i].GetEntityId().GetHashCode()
                        : 0);
                }
            }

            return hash;
        }
    }

    private void ClearGeneratedTiles()
    {
        Transform root = transform.Find(GeneratedRootName);
        if (root == null)
        {
            return;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            DestroyGeneratedObject(filters[i].sharedMesh);
        }

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            DestroyGeneratedObject(renderers[i].sharedMaterial);
        }

        DestroyGeneratedObject(root.gameObject);
    }

    private static void DestroyGeneratedObject(UnityEngine.Object generatedObject)
    {
        if (generatedObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedObject);
        }
        else
        {
            DestroyImmediate(generatedObject);
        }
    }
}
