using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.IO;

/// <summary>
/// Run via: Icarus > Setup Scene
/// Builds the entire prototype scene from scratch.
/// </summary>
public static class SceneBootstrapper
{
    private const string TileAssetPath = "Assets/Tilemaps/GroundTile.asset";
    private const string TileSpriteDir = "Assets/Sprites";

    [MenuItem("Icarus/Setup Scene")]
    public static void SetupScene()
    {
        ClearScene();
        CreateGameManager();
        var grid = CreateIsometricGrid();
        var ship = CreateShip();
        SetupCamera(ship.transform);
        EnsureGroundTileAssigned(grid);

        Debug.Log("[Icarus] Scene setup complete. Hit Play!");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void ClearScene()
    {
        // Remove objects we'll recreate so re-running is safe
        string[] names = { "GameManager", "Grid", "Ship" };
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private static Grid CreateIsometricGrid()
    {
        // Grid
        var gridGo = new GameObject("Grid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize   = new Vector3(1f, 0.5f, 1f);

        // Tilemap child
        var tilemapGo = new GameObject("Tilemap");
        tilemapGo.transform.SetParent(gridGo.transform);
        var tilemap         = tilemapGo.AddComponent<Tilemap>();
        var tilemapRenderer = tilemapGo.AddComponent<TilemapRenderer>();
        tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;

        // WorldGrid script
        var worldGrid    = gridGo.AddComponent<WorldGrid>();
        var tilemapField = typeof(WorldGrid).GetField("_tilemap",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tilemapField?.SetValue(worldGrid, tilemap);

        // Ground tile
        var tile = GetOrCreateGroundTile();
        var tileField = typeof(WorldGrid).GetField("_tile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tileField?.SetValue(worldGrid, tile);

        return grid;
    }

    private static GameObject CreateShip()
    {
        var go       = new GameObject("Ship");
        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = GetOrCreateShipSprite();
        sr.sortingOrder = 10;

        var rb            = go.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<ShipController>();
        go.transform.position = Vector3.zero;
        return go;
    }

    private static void SetupCamera(Transform target)
    {
        var cam = Camera.main?.gameObject ?? new GameObject("Main Camera");
        cam.tag = "MainCamera";
        if (cam.GetComponent<Camera>() == null) cam.AddComponent<Camera>();

        cam.transform.position = new Vector3(0f, 0f, -10f);

        var follow = cam.GetComponent<CameraFollow>() ?? cam.AddComponent<CameraFollow>();
        var targetField = typeof(CameraFollow).GetField("_target",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        targetField?.SetValue(follow, target);
    }

    private static void EnsureGroundTileAssigned(Grid grid)
    {
        // Already assigned in CreateIsometricGrid — this is a safety flush
        EditorUtility.SetDirty(grid.gameObject);
    }

    // ── Asset creation ────────────────────────────────────────────────────────

    private static Tile GetOrCreateGroundTile()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
        if (tile != null) return tile;

        Directory.CreateDirectory(Path.GetDirectoryName(TileAssetPath)!);
        tile        = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = GetOrCreateTileSprite();
        tile.color  = new Color(0.25f, 0.45f, 0.35f); // muted green

        AssetDatabase.CreateAsset(tile, TileAssetPath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static Sprite GetOrCreateTileSprite()
    {
        // Use Unity's built-in white square — tinted by Tile.color
        return Resources.Load<Sprite>("Sprites/square") ??
               AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static Sprite GetOrCreateShipSprite()
    {
        const string path = "Assets/Sprites/Ship_Placeholder.png";
        Directory.CreateDirectory(TileSpriteDir);

        if (!File.Exists(path))
        {
            // 16x16 white diamond texture as ship placeholder
            int size   = 16;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            // Fill transparent
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // Draw a diamond
            int cx = size / 2, cy = size / 2, r = size / 2 - 1;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= r)
                    pixels[y * size + x] = Color.cyan;

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            // Set import settings for pixel art
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
