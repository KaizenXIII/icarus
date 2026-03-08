using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Auto-runs on script compilation via [InitializeOnLoad].
/// Also available via: Icarus > Setup Scene
/// </summary>
[InitializeOnLoad]
public static class SceneBootstrapper
{
    private const string TileAssetPath  = "Assets/Tilemaps/GroundTile.asset";
    private const string ShipSpritePath = "Assets/Sprites/Ship_Triangle.png";
    private const string TileSpriteDir  = "Assets/Sprites";
    private const string SetupDoneKey   = "Icarus_SceneSetupDone";

    static SceneBootstrapper()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    private static void AutoSetup()
    {
        if (SessionState.GetBool(SetupDoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        SetupScene();
        SessionState.SetBool(SetupDoneKey, true);
    }

    [MenuItem("Icarus/Setup Scene")]
    public static void SetupScene()
    {
        SessionState.EraseBool(SetupDoneKey);
        ClearScene();
        SetCameraBackground();
        CreateGameManager();
        CreateStarField();
        CreateIsometricGrid();
        var ship = CreateShip();
        SetupCamera(ship.transform);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Icarus] Scene setup complete. Hit Play!");
    }

    // ── Scene setup ───────────────────────────────────────────────────────────

    private static void ClearScene()
    {
        string[] names = { "GameManager", "Grid", "Ship", "StarField" };
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static void SetCameraBackground()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.backgroundColor     = new Color(0.03f, 0.03f, 0.08f); // deep space dark
        cam.clearFlags          = CameraClearFlags.SolidColor;
        cam.orthographicSize    = 8f;
    }

    private static void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private static void CreateStarField()
    {
        var go = new GameObject("StarField");
        go.AddComponent<StarField>();
    }

    private static void CreateIsometricGrid()
    {
        var gridGo = new GameObject("Grid");
        var grid   = gridGo.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize   = new Vector3(1f, 0.5f, 1f);

        var tilemapGo       = new GameObject("Tilemap");
        tilemapGo.transform.SetParent(gridGo.transform);
        var tilemap         = tilemapGo.AddComponent<Tilemap>();
        var tilemapRenderer = tilemapGo.AddComponent<TilemapRenderer>();
        tilemapRenderer.sortOrder   = TilemapRenderer.SortOrder.BottomLeft;
        tilemapRenderer.sortingOrder = 0;

        var worldGrid    = gridGo.AddComponent<WorldGrid>();
        SetPrivateField(worldGrid, "_tilemap", tilemap);
        SetPrivateField(worldGrid, "_tile", GetOrCreateGroundTile());
    }

    private static GameObject CreateShip()
    {
        var go = new GameObject("Ship");

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetOrCreateTriangleShipSprite();
        sr.sortingOrder = 10;

        var rb                       = go.AddComponent<Rigidbody2D>();
        rb.gravityScale              = 0f;
        rb.freezeRotation            = true;
        rb.collisionDetectionMode    = CollisionDetectionMode2D.Continuous;

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
        SetPrivateField(follow, "_target", target);
    }

    // ── Asset creation ────────────────────────────────────────────────────────

    private static Tile GetOrCreateGroundTile()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
        if (tile != null) return tile;

        Directory.CreateDirectory(Path.GetDirectoryName(TileAssetPath)!);
        tile        = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        tile.color  = new Color(0.08f, 0.15f, 0.25f); // dark space-blue

        AssetDatabase.CreateAsset(tile, TileAssetPath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static Sprite GetOrCreateTriangleShipSprite()
    {
        Directory.CreateDirectory(TileSpriteDir);

        // Regenerate if missing
        if (!File.Exists(ShipSpritePath))
            GenerateTriangleShipPng(ShipSpritePath);

        return AssetDatabase.LoadAssetAtPath<Sprite>(ShipSpritePath);
    }

    private static void GenerateTriangleShipPng(string path)
    {
        int size = 32;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Clear to transparent
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.clear);

        // Orange triangle pointing up
        // Tip at top-center, base at bottom
        var orange      = new Color(1f, 0.45f, 0.1f);
        var orangeDark  = new Color(0.7f, 0.28f, 0.05f);

        for (int y = 2; y < size - 2; y++)
        {
            float t        = (float)(y - 2) / (size - 4);   // 0=bottom, 1=top
            float halfWidth = Mathf.Lerp(size / 2f - 1f, 0.5f, t);
            int   cx       = size / 2;
            int   left     = Mathf.RoundToInt(cx - halfWidth);
            int   right    = Mathf.RoundToInt(cx + halfWidth);

            for (int x = left; x <= right; x++)
            {
                bool isEdge = (x == left || x == right || y == 2);
                tex.SetPixel(x, y, isEdge ? orangeDark : orange);
            }
        }

        // Engine glow at base (bottom 4 rows)
        var glow = new Color(1f, 0.75f, 0.2f, 0.8f);
        for (int y = 2; y < 6; y++)
        {
            float t        = (float)(y - 2) / (size - 4);
            float halfWidth = Mathf.Lerp(size / 2f - 1f, 0.5f, t);
            int   cx       = size / 2;
            int   cx1      = cx - 3, cx2 = cx + 3;
            if (cx1 >= (int)(cx - halfWidth) && cx1 <= (int)(cx + halfWidth))
                tex.SetPixel(cx1, y, glow);
            if (cx2 >= (int)(cx - halfWidth) && cx2 <= (int)(cx + halfWidth))
                tex.SetPixel(cx2, y, glow);
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType         = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // ── Reflection helper ─────────────────────────────────────────────────────

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        obj.GetType()
           .GetField(fieldName,
               System.Reflection.BindingFlags.NonPublic |
               System.Reflection.BindingFlags.Instance)
           ?.SetValue(obj, value);
    }
}
