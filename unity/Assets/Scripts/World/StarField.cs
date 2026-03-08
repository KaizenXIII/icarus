using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural starfield using SpriteRenderers — works with URP.
/// Spawns stars and faint constellation lines around the camera.
/// </summary>
public class StarField : MonoBehaviour
{
    [SerializeField] private int   _starCount        = 120;
    [SerializeField] private float _fieldRadius      = 35f;
    [SerializeField] private float _parallaxFactor   = 0.12f;
    [SerializeField] private float _constellationDist = 6f;
    [SerializeField] private int   _maxLines         = 45;

    private Transform _camTransform;
    private Sprite    _dotSprite;

    private readonly List<Transform> _starTransforms = new();
    private readonly List<Vector2>   _starPositions  = new();

    private void Awake()
    {
        _camTransform = Camera.main?.transform ?? FindFirstObjectByType<Camera>().transform;
        _dotSprite    = CreateDotSprite();

        SpawnStars();
        SpawnConstellationLines();
    }

    private void LateUpdate()
    {
        if (_camTransform == null) return;
        Vector2 offset = (Vector2)_camTransform.position * _parallaxFactor;
        transform.position = new Vector3(offset.x, offset.y, 0f);
    }

    // ── Stars ─────────────────────────────────────────────────────────────────

    private void SpawnStars()
    {
        for (int i = 0; i < _starCount; i++)
        {
            Vector2 pos    = Random.insideUnitCircle * _fieldRadius;
            float   bright = Random.Range(0.5f, 1f);
            float   size   = Random.Range(0.04f, 0.16f);

            var go   = new GameObject("Star");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale    = Vector3.one * size;

            var sr        = go.AddComponent<SpriteRenderer>();
            sr.sprite     = _dotSprite;
            sr.color      = new Color(bright * 0.85f, bright * 0.9f, 1f, bright);
            sr.sortingOrder = -20;

            _starTransforms.Add(go.transform);
            _starPositions.Add(pos);
        }
    }

    // ── Constellation lines ───────────────────────────────────────────────────

    private void SpawnConstellationLines()
    {
        int count = 0;
        for (int i = 0; i < _starPositions.Count && count < _maxLines; i++)
        for (int j = i + 1; j < _starPositions.Count && count < _maxLines; j++)
        {
            float dist = Vector2.Distance(_starPositions[i], _starPositions[j]);
            if (dist > _constellationDist) continue;

            SpawnLine(_starPositions[i], _starPositions[j]);
            count++;
        }
    }

    private void SpawnLine(Vector2 a, Vector2 b)
    {
        Vector2 mid  = (a + b) * 0.5f;
        float   len  = Vector2.Distance(a, b);
        float   angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        var go = new GameObject("Line");
        go.transform.SetParent(transform, false);
        go.transform.localPosition    = new Vector3(mid.x, mid.y, 0f);
        go.transform.localEulerAngles = new Vector3(0f, 0f, angle);
        go.transform.localScale       = new Vector3(len, 0.012f, 1f);

        var sr        = go.AddComponent<SpriteRenderer>();
        sr.sprite     = _dotSprite;
        sr.color      = new Color(0.35f, 0.5f, 0.9f, 0.18f);
        sr.sortingOrder = -21;
    }

    // ── Sprite ────────────────────────────────────────────────────────────────

    private static Sprite CreateDotSprite()
    {
        int size = 8;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float cx = size / 2f - 0.5f, cy = size / 2f - 0.5f, r = size / 2f - 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float alpha = Mathf.Clamp01(1f - dist / r);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
