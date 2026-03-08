using UnityEngine;

/// <summary>
/// Procedural starfield with faint constellation lines drawn via GL.
/// Attach to any GameObject in the scene.
/// </summary>
public class StarField : MonoBehaviour
{
    [SerializeField] private int   _starCount       = 150;
    [SerializeField] private float _fieldRadius     = 30f;
    [SerializeField] private float _parallaxFactor  = 0.12f;
    [SerializeField] private float _constellationDist = 5f;
    [SerializeField] private int   _maxLines        = 50;

    private Transform _camTransform;

    private struct StarData
    {
        public Vector2 LocalPos;
        public float   Size;
        public Color   Color;
    }

    private struct LineData
    {
        public Vector2 A, B;
    }

    private StarData[] _stars;
    private LineData[] _lines;
    private Material   _mat;

    private void Awake()
    {
        _camTransform = Camera.main?.transform ?? FindFirstObjectByType<Camera>()?.transform;

        // URP-safe material using hidden sprite shader
        _mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _mat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        _mat.SetInt("_ZWrite",   0);

        GenerateStars();
        GenerateLines();
    }

    private void GenerateStars()
    {
        _stars = new StarData[_starCount];
        for (int i = 0; i < _starCount; i++)
        {
            Vector2 pos   = Random.insideUnitCircle * _fieldRadius;
            float   bright = Random.Range(0.5f, 1f);
            float   blue   = Random.Range(0.8f, 1f);
            _stars[i] = new StarData
            {
                LocalPos = pos,
                Size     = Random.Range(0.04f, 0.14f),
                Color    = new Color(bright * 0.85f, bright * 0.9f, blue, 1f)
            };
        }
    }

    private void GenerateLines()
    {
        var list = new System.Collections.Generic.List<LineData>();
        for (int i = 0; i < _stars.Length && list.Count < _maxLines; i++)
        for (int j = i + 1; j < _stars.Length && list.Count < _maxLines; j++)
        {
            if (Vector2.Distance(_stars[i].LocalPos, _stars[j].LocalPos) < _constellationDist)
                list.Add(new LineData { A = _stars[i].LocalPos, B = _stars[j].LocalPos });
        }
        _lines = list.ToArray();
    }

    private void OnPostRender() => Draw();

    // Also works with cameras that don't call OnPostRender on this object
    private Camera _attachedCam;
    private void Start()
    {
        _attachedCam = Camera.main ?? FindFirstObjectByType<Camera>();
        if (_attachedCam != null)
            _attachedCam.GetComponent<UnityEngine.Camera>().cullingMask = ~0;
    }

    private void OnRenderObject() => Draw();

    private bool _drawn;
    private void LateUpdate() => _drawn = false;

    private void Draw()
    {
        if (_drawn) return;
        _drawn = true;

        if (_mat == null || _stars == null) return;

        // Parallax offset
        Vector2 offset = _camTransform != null
            ? (Vector2)_camTransform.position * _parallaxFactor
            : Vector2.zero;

        _mat.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
        GL.MultMatrix(Camera.main.worldToCameraMatrix);

        // Draw constellation lines
        GL.Begin(GL.LINES);
        GL.Color(new Color(0.35f, 0.45f, 0.75f, 0.2f));
        foreach (var line in _lines)
        {
            GL.Vertex3(line.A.x + offset.x, line.A.y + offset.y, 0f);
            GL.Vertex3(line.B.x + offset.x, line.B.y + offset.y, 0f);
        }
        GL.End();

        // Draw stars as quads
        GL.Begin(GL.QUADS);
        foreach (var star in _stars)
        {
            float s  = star.Size;
            float px = star.LocalPos.x + offset.x;
            float py = star.LocalPos.y + offset.y;
            GL.Color(star.Color);
            GL.Vertex3(px - s, py - s, 0f);
            GL.Vertex3(px + s, py - s, 0f);
            GL.Vertex3(px + s, py + s, 0f);
            GL.Vertex3(px - s, py + s, 0f);
        }
        GL.End();

        GL.PopMatrix();
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
