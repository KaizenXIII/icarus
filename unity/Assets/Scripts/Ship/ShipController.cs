using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;

    // Isometric axis vectors: right/left = diagonal, up/down = diagonal
    private static readonly Vector2 IsoRight = new Vector2(1f, 0.5f).normalized;
    private static readonly Vector2 IsoUp    = new Vector2(-1f, 0.5f).normalized;

    private Rigidbody2D _rb;
    private Vector2 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _input = new Vector2(h, v);
    }

    private void FixedUpdate()
    {
        Vector2 move = (_input.x * IsoRight + _input.y * IsoUp).normalized * _moveSpeed;
        _rb.linearVelocity = move;
    }
}
