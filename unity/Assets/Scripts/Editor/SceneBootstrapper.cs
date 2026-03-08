using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Auto-runs on script compilation. Also: Icarus > Setup Scene
/// </summary>
[InitializeOnLoad]
public static class SceneBootstrapper
{
    private const string ShipSpritePath = "Assets/Sprites/Ship_Triangle.png";
    private const string TileSpriteDir  = "Assets/Sprites";
    private const string SetupDoneKey   = "Icarus_SceneSetupDone_v3";

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
        SetupCamera();
        CreateGameManager();
        CreateStarField();
        var ship = CreateShip();
        WireCameraFollow(ship.transform);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Icarus] Scene setup complete. Hit Play!");
    }

    // ── Scene ─────────────────────────────────────────────────────────────────

    private static void ClearScene()
    {
        foreach (var name in new[] { "GameManager", "Grid", "Ship", "StarField" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.backgroundColor  = new Color(0.02f, 0.02f, 0.06f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.orthographicSize = 8f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateGameManager()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private static void CreateStarField()
    {
        new GameObject("StarField").AddComponent<StarField>();
    }

    private static GameObject CreateShip()
    {
        GenerateTriangleShipPng(ShipSpritePath);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShipSpritePath);

        var go = new GameObject("Ship");
        go.transform.position   = Vector3.zero;
        go.transform.localScale = Vector3.one * 0.5f;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = 10;

        var rb                    = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<ShipController>();
        return go;
    }

    private static void WireCameraFollow(Transform target)
    {
        var cam    = Camera.main?.gameObject;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>() ?? cam.AddComponent<CameraFollow>();
        typeof(CameraFollow)
            .GetField("_target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(follow, target);
    }

    // ── Ship sprite ───────────────────────────────────────────────────────────

    private static void GenerateTriangleShipPng(string path)
    {
        Directory.CreateDirectory(TileSpriteDir);

        int size = 64;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.clear);

        var orange     = new Color(1f,   0.5f,  0.1f,  1f);
        var orangeEdge = new Color(0.8f, 0.3f,  0.05f, 1f);
        var glow       = new Color(1f,   0.85f, 0.3f,  1f);

        int tip     = size - 4;   // y of tip (top)
        int baseY   = 4;          // y of base (bottom)
        int cx      = size / 2;

        for (int y = baseY; y <= tip; y++)
        {
            float t         = (float)(y - baseY) / (tip - baseY); // 0=base, 1=tip
            float halfWidth = Mathf.Lerp(size / 2f - 4f, 1f, t);
            int   left      = Mathf.RoundToInt(cx - halfWidth);
            int   right     = Mathf.RoundToInt(cx + halfWidth);

            for (int x = left; x <= right; x++)
            {
                bool edge = x == left || x == right || y == baseY;
                tex.SetPixel(x, y, edge ? orangeEdge : orange);
            }
        }

        // Engine glow — two bright dots at base
        for (int y = baseY; y < baseY + 8; y++)
        {
            float t         = (float)(y - baseY) / (tip - baseY);
            float halfWidth = Mathf.Lerp(size / 2f - 4f, 1f, t);
            int   left      = Mathf.RoundToInt(cx - halfWidth);
            int   right     = Mathf.RoundToInt(cx + halfWidth);
            int   g1 = cx - 4, g2 = cx + 4;
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
}
