using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float doubleTapTime = 0.25f;
    [SerializeField] private float dashCooldown = 0.2f;
    [SerializeField] private bool allowAirDash = false;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCol;

    private float moveInput;
    private bool jumpRequested;

    private float lastLeftTapTime = -10f;
    private float lastRightTapTime = -10f;
    private float dashTimeLeft = 0f;
    private float lastDashTime = -10f;
    private int dashDirection = 0;
    private int facingDirection = 1;

    public float MoveInput => moveInput;
    public bool IsGrounded { get; private set; }
    public float VerticalSpeed => rb.velocity.y;
    public bool IsDashing => dashTimeLeft > 0f;
    public float AnimationSpeed => IsDashing ? 1f : Mathf.Abs(moveInput);
    public int FacingDirection => facingDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCol = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0.01f)
            facingDirection = 1;
        else if (moveInput < -0.01f)
            facingDirection = -1;

        HandleDashInput();

        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        IsGrounded = capsuleCol.IsTouchingLayers(groundLayer);

        float xVelocity = IsDashing ? dashDirection * dashSpeed : moveInput * moveSpeed;
        rb.velocity = new Vector2(xVelocity, rb.velocity.y);

        if (jumpRequested && IsGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        jumpRequested = false;

        if (dashTimeLeft > 0f)
            dashTimeLeft -= Time.fixedDeltaTime;
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftTapTime <= doubleTapTime)
                TryStartDash(-1);

            lastLeftTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightTapTime <= doubleTapTime)
                TryStartDash(1);

            lastRightTapTime = Time.time;
        }
    }

    private void TryStartDash(int direction)
    {
        if (Time.time - lastDashTime < dashCooldown)
            return;

        if (!allowAirDash && !IsGrounded)
            return;

        dashDirection = direction;
        facingDirection = direction;
        dashTimeLeft = dashDuration;
        lastDashTime = Time.time;
    }
}