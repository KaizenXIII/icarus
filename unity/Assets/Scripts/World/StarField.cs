using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural infinite starfield with constellation lines.
/// Attach to any GameObject. Stars recycle as the camera moves.
/// </summary>
public class StarField : MonoBehaviour
{
    [SerializeField] private int   _starCount        = 120;
    [SerializeField] private float _fieldRadius      = 25f;
    [SerializeField] private float _constellationDist = 4f;
    [SerializeField] private int   _maxLines         = 40;
    [SerializeField] private float _parallaxFactor   = 0.15f;

    private Camera          _cam;
    private Transform       _camTransform;
    private List<Transform> _stars        = new();
    private List<LineRenderer> _lines     = new();
    private Sprite          _starSprite;

    private void Awake()
    {
        _cam          = Camera.main;
        _camTransform = _cam.transform;
        _starSprite   = CreateStarSprite();

        SpawnStars();
        DrawConstellations();
    }

    private void LateUpdate()
    {
        // Parallax: background moves slower than camera
        Vector3 camPos = _camTransform.position * _parallaxFactor;
        transform.position = new Vector3(camPos.x, camPos.y, 1f);
    }

    // ── Star spawning ─────────────────────────────────────────────────────────

    private void SpawnStars()
    {
        for (int i = 0; i < _starCount; i++)
        {
            var go = new GameObject($"Star_{i}");
            go.transform.SetParent(transform);

            Vector2 pos  = Random.insideUnitCircle * _fieldRadius;
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            var sr      = go.AddComponent<SpriteRenderer>();
            sr.sprite   = _starSprite;
            sr.sortingOrder = -10;

            float size  = Random.Range(0.03f, 0.12f);
            go.transform.localScale = Vector3.one * size;

            float bright = Random.Range(0.55f, 1f);
            sr.color = new Color(bright, bright, Random.Range(bright * 0.8f, 1f), 1f);

            _stars.Add(go.transform);
        }
    }

    private void DrawConstellations()
    {
        int lines = 0;
        for (int i = 0; i < _stars.Count && lines < _maxLines; i++)
        {
            for (int j = i + 1; j < _stars.Count && lines < _maxLines; j++)
            {
                float dist = Vector3.Distance(
                    _stars[i].localPosition,
                    _stars[j].localPosition);

                if (dist < _constellationDist)
                {
                    CreateLine(_stars[i].localPosition, _stars[j].localPosition);
                    lines++;
                }
            }
        }
    }

    private void CreateLine(Vector3 a, Vector3 b)
    {
        var go = new GameObject("ConstellationLine");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var lr               = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth        = 0.015f;
        lr.endWidth          = 0.015f;
        lr.useWorldSpace     = false;
        lr.sortingOrder      = -11;
        lr.material          = new Material(Shader.Find("Sprites/Default"));
        lr.startColor        = new Color(0.4f, 0.5f, 0.8f, 0.25f);
        lr.endColor          = new Color(0.4f, 0.5f, 0.8f, 0.25f);

        _lines.Add(lr);
    }

    // ── Sprite creation ───────────────────────────────────────────────────────

    private static Sprite CreateStarSprite()
    {
        int size  = 4;
        var tex   = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.clear);

        // Simple cross/dot
        tex.SetPixel(1, 1, Color.white);
        tex.SetPixel(2, 1, Color.white);
        tex.SetPixel(1, 2, Color.white);
        tex.SetPixel(2, 2, Color.white);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }
}
