using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class PlayerMotor2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayers;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float movementSpeed = 6f;
    [SerializeField, Min(0f)] private float groundAcceleration = 60f;
    [SerializeField, Min(0f)] private float groundDeceleration = 80f;
    [SerializeField, Min(0f)] private float airAcceleration = 35f;
    [SerializeField, Min(0f)] private float airDeceleration = 20f;

    [Header("Salto")]
    [SerializeField, Min(0f)] private float jumpSpeed = 11f;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

    private float moveInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpCutRequested;

    public bool IsGrounded { get; private set; }

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f)
        {
            jumpBufferTimer = 0f;
            jumpCutRequested = false;
            return;
        }

        IsGrounded = CheckGrounded();
        UpdateJumpTimers();

        Vector2 velocity = body.linearVelocity;
        float targetSpeed = moveInput * movementSpeed;
        float acceleration = SelectHorizontalAcceleration(targetSpeed);

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        UpdateFacingDirection();

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpSpeed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            IsGrounded = false;
        }

        if (jumpCutRequested && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
        }

        jumpCutRequested = false;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        body.linearVelocity = velocity;
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        Collider2D groundHit = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayers
        );

        return groundHit != null;
    }

    private void UpdateJumpTimers()
    {
        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer = Mathf.Max(
                0f,
                coyoteTimer - Time.fixedDeltaTime
            );
        }

        jumpBufferTimer = Mathf.Max(
            0f,
            jumpBufferTimer - Time.fixedDeltaTime
        );
    }

    private float SelectHorizontalAcceleration(float targetSpeed)
    {
        bool isAccelerating = Mathf.Abs(targetSpeed)
            > Mathf.Abs(body.linearVelocity.x);

        if (IsGrounded)
        {
            return isAccelerating
                ? groundAcceleration
                : groundDeceleration;
        }

        return isAccelerating
            ? airAcceleration
            : airDeceleration;
    }

    private void UpdateFacingDirection()
    {
        if (visual == null || Mathf.Abs(moveInput) < 0.01f)
        {
            return;
        }

        Vector3 localScale = visual.localScale;
        localScale.x = Mathf.Abs(localScale.x)
            * Mathf.Sign(moveInput);
        visual.localScale = localScale;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && Time.timeScale > 0f)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (context.canceled)
        {
            jumpCutRequested = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
