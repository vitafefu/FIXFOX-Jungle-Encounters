using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Double Tap Run")]
    [SerializeField] private float doubleTapTime = 0.25f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCol;

    private float moveInput;
    private bool jumpRequested;

    private float lastLeftTapTime = -10f;
    private float lastRightTapTime = -10f;

    private int facingDirection = 1;

    private bool isRunning = false;
    private int runDirection = 0; // -1 = left, 1 = right

    public float MoveInput => moveInput;
    public bool IsGrounded { get; private set; }
    public float VerticalSpeed => rb.velocity.y;
    public int FacingDirection => facingDirection;
    public bool IsRunning => isRunning;
    public float AnimationSpeed => Mathf.Abs(rb.velocity.x);

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

        HandleRunInput();

        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;

        StopRunIfNeeded();
    }

    private void FixedUpdate()
    {
        IsGrounded = capsuleCol.IsTouchingLayers(groundLayer);

        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        if (jumpRequested && IsGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        jumpRequested = false;
    }

    private void HandleRunInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftTapTime <= doubleTapTime)
            {
                isRunning = true;
                runDirection = -1;
            }

            lastLeftTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightTapTime <= doubleTapTime)
            {
                isRunning = true;
                runDirection = 1;
            }

            lastRightTapTime = Time.time;
        }
    }

    private void StopRunIfNeeded()
    {
        // إذا وقف اللاعب
        if (Mathf.Abs(moveInput) < 0.01f)
        {
            isRunning = false;
            runDirection = 0;
            return;
        }

        // إذا غيّر الاتجاه
        if ((runDirection == 1 && moveInput < 0) || (runDirection == -1 && moveInput > 0))
        {
            isRunning = false;
            runDirection = 0;
        }
    }
}