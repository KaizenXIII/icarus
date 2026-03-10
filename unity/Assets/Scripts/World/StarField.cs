using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural starfield using SpriteRenderers — works with URP.
/// Stars wrap around the camera so the field is infinite.
/// Constellation lines connect nearby stars.
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

    private readonly List<Transform>       _starTransforms    = new();
    private readonly List<Vector2>         _starBasePositions = new();
    private readonly List<LineData>        _lines             = new();
    private readonly List<SpriteRenderer>  _starRenderers     = new();
    private readonly List<float>           _starBaseAlpha     = new();
    private readonly List<float>           _starFlickerPhase  = new();

    private struct LineData
    {
        public Transform Transform;
        public int StarA;
        public int StarB;
    }

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

        Vector2 camPos = (Vector2)_camTransform.position * _parallaxFactor;
        float diameter = _fieldRadius * 2f;

        float time = Time.time;

        // Wrap each star around the camera and apply flicker
        for (int i = 0; i < _starTransforms.Count; i++)
        {
            Vector2 basePos = _starBasePositions[i];
            float wrappedX = Wrap(basePos.x - camPos.x, diameter) + camPos.x;
            float wrappedY = Wrap(basePos.y - camPos.y, diameter) + camPos.y;
            _starTransforms[i].position = new Vector3(wrappedX, wrappedY, 0f);

            // Per-star brightness flicker (matches JSX: sin(t * 0.0015 + phase))
            float flicker = Mathf.Sin(time * 0.9f + _starFlickerPhase[i]) * 0.18f;
            var c = _starRenderers[i].color;
            _starRenderers[i].color = new Color(c.r, c.g, c.b, Mathf.Clamp01(_starBaseAlpha[i] + flicker));
        }

        // Update constellation lines to follow their stars
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            Vector2 a = _starTransforms[line.StarA].position;
            Vector2 b = _starTransforms[line.StarB].position;

            // Hide lines whose stars wrapped to opposite sides
            float dist = Vector2.Distance(a, b);
            if (dist > _constellationDist * 1.5f)
            {
                line.Transform.gameObject.SetActive(false);
                continue;
            }

            line.Transform.gameObject.SetActive(true);
            Vector2 mid   = (a + b) * 0.5f;
            float   angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            line.Transform.position         = new Vector3(mid.x, mid.y, 0f);
            line.Transform.eulerAngles      = new Vector3(0f, 0f, angle);
            line.Transform.localScale       = new Vector3(dist, 0.012f, 1f);
        }
    }

    private static float Wrap(float value, float range)
    {
        float half = range * 0.5f;
        return ((value + half) % range + range) % range - half;
    }

    // ── Stars ─────────────────────────────────────────────────────────────────

    private void SpawnStars()
    {
        for (int i = 0; i < _starCount; i++)
        {
            Vector2 pos    = Random.insideUnitCircle * _fieldRadius;
            float   bright = Random.Range(0.5f, 1f);
            float   size   = Random.Range(0.04f, 0.16f);

            var go = new GameObject("Star");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale    = Vector3.one * size;

            var sr        = go.AddComponent<SpriteRenderer>();
            sr.sprite     = _dotSprite;
            sr.color      = new Color(bright * 0.85f, bright * 0.9f, 1f, bright);
            sr.sortingOrder = -20;

            _starTransforms.Add(go.transform);
            _starBasePositions.Add(pos);
            _starRenderers.Add(sr);
            _starBaseAlpha.Add(bright);
            _starFlickerPhase.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }

    // ── Constellation lines ───────────────────────────────────────────────────

    private void SpawnConstellationLines()
    {
        int count = 0;
        for (int i = 0; i < _starBasePositions.Count && count < _maxLines; i++)
        for (int j = i + 1; j < _starBasePositions.Count && count < _maxLines; j++)
        {
            float dist = Vector2.Distance(_starBasePositions[i], _starBasePositions[j]);
            if (dist > _constellationDist) continue;

            var go = new GameObject("Line");
            go.transform.SetParent(transform, false);

            var sr        = go.AddComponent<SpriteRenderer>();
            sr.sprite     = _dotSprite;
            sr.color      = new Color(0.35f, 0.5f, 0.9f, 0.18f);
            sr.sortingOrder = -21;

            _lines.Add(new LineData
            {
                Transform = go.transform,
                StarA = i,
                StarB = j
            });
            count++;
        }
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
