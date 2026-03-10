using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

/// <summary>
/// Auto-runs on compile. Also: Icarus > Setup Scene
/// Sets up: Pixel Perfect Camera, Y-sort, Bloom/Vignette, Isometric Grid, Ship, Starfield, ScriptableObjects.
/// </summary>
[InitializeOnLoad]
public static class SceneBootstrapper
{
    // ── Paths ─────────────────────────────────────────────────────────────────
    private const string SpritesDir      = "Assets/Sprites";
    private const string ShipSpritePath  = "Assets/Sprites/Ship_Triangle.png";
    private const string TileSpritePath  = "Assets/Sprites/IsoDiamond.png";
    private const string TileAssetPath   = "Assets/Tilemaps/GroundTile.asset";
    private const string PostProcPath    = "Assets/Settings/PostProcessProfile.asset";
    private const string ShipDataPath    = "Assets/ScriptableObjects/DefaultShipData.asset";
    private const string SetupKey        = "Icarus_Setup_v4";

    static SceneBootstrapper()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    private static void AutoSetup()
    {
        if (SessionState.GetBool(SetupKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SetupScene();
        SessionState.SetBool(SetupKey, true);
    }

    [MenuItem("Icarus/Setup Scene")]
    public static void SetupScene()
    {
        SessionState.EraseBool(SetupKey);
        ClearScene();

        // 1. Camera — Pixel Perfect + Y-sort
        SetupCamera();

        // 2. Post-processing — Bloom + Vignette
        CreatePostProcessing();

        // 3. StarField
        new GameObject("StarField").AddComponent<StarField>();

        // 4. Isometric Grid
        CreateIsometricGrid();

        // 5. Ship
        var ship = CreateShip();
        WireCameraFollow(ship.transform);

        // 6. GameManager
        new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Icarus] Scene setup complete — v4. Hit Play!");
    }

    // ── 1. Camera ─────────────────────────────────────────────────────────────

    private static void SetupCamera()
    {
        var camGo = Camera.main?.gameObject;
        if (camGo == null) return;

        var cam = camGo.GetComponent<Camera>();
        cam.backgroundColor  = new Color(0.02f, 0.02f, 0.06f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.orthographicSize = 8f;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // Isometric Y-sort
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0f, 1f, 0f);

        // Pixel Perfect Camera
        var ppc = camGo.GetComponent<PixelPerfectCamera>()
               ?? camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU       = 32;
        ppc.refResolutionX  = 640;
        ppc.refResolutionY  = 360;
        ppc.upscaleRT       = false;
        ppc.pixelSnapping   = true;
        ppc.cropFrameX      = false;
        ppc.cropFrameY      = false;

        EditorUtility.SetDirty(camGo);
    }

    // ── 2. Post-processing ────────────────────────────────────────────────────

    private static void CreatePostProcessing()
    {
        // Remove old volume
        var old = GameObject.Find("PostProcessVolume");
        if (old != null) Object.DestroyImmediate(old);

        // Create or load profile
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcPath);
        if (profile == null)
        {
            Directory.CreateDirectory("Assets/Settings");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.5f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.4f);

            AssetDatabase.CreateAsset(profile, PostProcPath);
            AssetDatabase.SaveAssets();
        }

        var go     = new GameObject("PostProcessVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile  = profile;
    }

    // ── 3. Isometric Grid ─────────────────────────────────────────────────────

    private static void CreateIsometricGrid()
    {
        var old = GameObject.Find("Grid");
        if (old != null) Object.DestroyImmediate(old);

        // Grid
        var gridGo = new GameObject("Grid");
        var grid   = gridGo.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize   = new Vector3(1f, 0.5f, 1f);

        // Tilemap
        var tilemapGo = new GameObject("Tilemap");
        tilemapGo.transform.SetParent(gridGo.transform);
        var tilemap    = tilemapGo.AddComponent<Tilemap>();
        var tmRenderer = tilemapGo.AddComponent<TilemapRenderer>();
        tmRenderer.sortOrder    = TilemapRenderer.SortOrder.BottomLeft;
        tmRenderer.sortingOrder = -5;

        // WorldGrid
        var wg = gridGo.AddComponent<WorldGrid>();
        SetField(wg, "_tilemap", tilemap);
        SetField(wg, "_tile",    GetOrCreateGroundTile());
    }

    private static Tile GetOrCreateGroundTile()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
        if (tile != null) return tile;

        Directory.CreateDirectory("Assets/Tilemaps");
        tile        = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = GetOrCreateDiamondSprite();
        tile.color  = Color.white;

        AssetDatabase.CreateAsset(tile, TileAssetPath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static Sprite GetOrCreateDiamondSprite()
    {
        GenerateIsoDiamondPng(TileSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
    }

    private static void GenerateIsoDiamondPng(string path)
    {
        Directory.CreateDirectory(SpritesDir);
        int w = 64, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tex.SetPixel(x, y, Color.clear);

        float cx = w / 2f - 0.5f, cy = h / 2f - 0.5f;
        float rx = w / 2f - 1f,   ry = h / 2f - 1f;

        // Top face (lighter blue-grey)
        var topCol  = new Color(0.12f, 0.18f, 0.32f);
        // Edge highlight
        var edgeCol = new Color(0.22f, 0.32f, 0.50f);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x - cx) / rx;
            float ny = Mathf.Abs(y - cy) / ry;
            if (nx + ny > 1f) continue;

            // Edge pixel
            float distToEdge = 1f - (nx + ny);
            bool  isEdge     = distToEdge < 0.08f;
            tex.SetPixel(x, y, isEdge ? edgeCol : topCol);
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 64;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.spritePivot         = new Vector2(0.5f, 0.5f);
        imp.SaveAndReimport();
    }

    // ── 4. Ship ───────────────────────────────────────────────────────────────

    private static GameObject CreateShip()
    {
        GenerateTriangleShipPng(ShipSpritePath);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShipSpritePath);

        var go = new GameObject("Ship");
        go.transform.position   = Vector3.zero;
        go.transform.localScale = Vector3.one * 0.4f;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 10;

        var rb                    = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var sc = go.AddComponent<ShipController>();
        SetField(sc, "_data", GetOrCreateShipData());

        go.AddComponent<EngineGlow>();

        return go;
    }

    private static ShipData GetOrCreateShipData()
    {
        var data = AssetDatabase.LoadAssetAtPath<ShipData>(ShipDataPath);
        if (data != null) return data;

        Directory.CreateDirectory("Assets/ScriptableObjects");
        data = ScriptableObject.CreateInstance<ShipData>();
        AssetDatabase.CreateAsset(data, ShipDataPath);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static void GenerateTriangleShipPng(string path)
    {
        Directory.CreateDirectory(SpritesDir);
        int size = 64;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.clear);

        var orange = new Color(1f,   0.50f, 0.10f, 1f);
        var edge   = new Color(0.8f, 0.30f, 0.05f, 1f);
        var glow   = new Color(1f,   0.85f, 0.30f, 1f);

        int tip   = size - 6;
        int baseY = 4;
        int cx    = size / 2;

        for (int y = baseY; y <= tip; y++)
        {
            float t         = (float)(y - baseY) / (tip - baseY);
            float halfWidth = Mathf.Lerp(size / 2f - 5f, 1f, t);
            int   left      = Mathf.RoundToInt(cx - halfWidth);
            int   right     = Mathf.RoundToInt(cx + halfWidth);

            for (int x = left; x <= right; x++)
                tex.SetPixel(x, y, (x == left || x == right || y == baseY) ? edge : orange);
        }

        // Engine glow
        for (int y = baseY; y < baseY + 10; y++)
        {
            float t         = (float)(y - baseY) / (tip - baseY);
            float halfWidth = Mathf.Lerp(size / 2f - 5f, 1f, t);
            int   left      = Mathf.RoundToInt(cx - halfWidth);
            int   right     = Mathf.RoundToInt(cx + halfWidth);
            int   g1 = cx - 5, g2 = cx + 5;
            if (g1 >= left && g1 <= right) tex.SetPixel(g1, y, glow);
            if (g2 >= left && g2 <= right) tex.SetPixel(g2, y, glow);
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 64;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }

    // ── Camera follow ─────────────────────────────────────────────────────────

    private static void WireCameraFollow(Transform target)
    {
        var camGo = Camera.main?.gameObject;
        if (camGo == null) return;
        var follow = camGo.GetComponent<CameraFollow>() ?? camGo.AddComponent<CameraFollow>();
        SetField(follow, "_target", target);
    }

    // ── Scene clear ───────────────────────────────────────────────────────────

    private static void ClearScene()
    {
        foreach (var name in new[] { "GameManager", "Grid", "Ship", "StarField", "PostProcessVolume" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ── Reflection helper ─────────────────────────────────────────────────────

    private static void SetField(object obj, string fieldName, object value)
    {
        obj.GetType()
           .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
           ?.SetValue(obj, value);
    }
}
