using UnityEngine;

/// <summary>
/// Pulses the ship's sprite color to simulate engine glow breathing.
/// Mirrors the JSX prototype: sin(t * 0.01) * 0.3 + 0.7 engine intensity.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EngineGlow : MonoBehaviour
{
    [SerializeField] private float _pulseSpeed    = 1.8f;
    [SerializeField] private float _pulseAmplitude = 0.12f;
    [SerializeField] private Color _baseColor     = new Color(1f, 0.55f, 0.15f);
    [SerializeField] private Color _glowColor     = new Color(1f, 0.85f, 0.45f);

    private SpriteRenderer _sr;

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void Update()
    {
        float t = Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 0.5f; // 0..1
        _sr.color = Color.Lerp(_baseColor, _glowColor, t * _pulseAmplitude * 2f);
    }
}
