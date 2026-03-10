using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipData _data;
    [SerializeField] private float _acceleration = 12f;
    [SerializeField] private float _deceleration = 8f;

    // Isometric axis vectors
    private static readonly Vector2 IsoRight = new Vector2( 1f,  0.5f).normalized;
    private static readonly Vector2 IsoUp    = new Vector2(-1f,  0.5f).normalized;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private Vector2     _input;
    private Vector2     _currentVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        _moveAction.Enable();
    }

    private void Update()
    {
        _input = _moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        float maxSpeed = _data != null ? _data.MoveSpeed : 3f;
        Vector2 targetVelocity = Vector2.zero;

        if (_input.sqrMagnitude > 0.01f)
        {
            targetVelocity = (_input.x * IsoRight + _input.y * IsoUp).normalized * maxSpeed;
        }

        float rate = targetVelocity.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
        _currentVelocity = Vector2.MoveTowards(_currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = _currentVelocity;
    }

    private void OnDestroy()
    {
        _moveAction?.Dispose();
    }
}
