using UnityEngine;

/// <summary>
/// Simple enemy movement.
/// This script is responsible only for left-right patrol movement.
/// Health and contact damage are handled by Enemy.cs.
/// 
/// Important:
/// - The enemy patrols only while grounded.
/// - If it falls, gravity controls the fall.
/// - Movement uses Rigidbody2D velocity, not MovePosition.
/// - This prevents hovering / walking in the air / weird sliding.
/// </summary>
public class SimpleEnemy : Enemy
{
    [Header("Patrol Movement")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float startOffset = 0f;
    [SerializeField] private float waitTimeAtEdge = 1.5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.65f, 0.12f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

    [Header("Physics Control")]
    [SerializeField] private bool stopHorizontalVelocityInAir = true;
    [SerializeField] private bool stopHorizontalVelocityWhileWaiting = true;

    [Header("Start Stabilization")]
    [SerializeField] private float startMoveDelay = 0.15f;
    [SerializeField] private bool lockHorizontalPositionAtStart = true;

    [Header("Visual")]
    [SerializeField] private bool flipSprite = true;

    private bool movingRight = true;
    private bool isWaiting;
    private float waitTimer;

    private float leftBoundary;
    private float rightBoundary;

    private float startMoveTimer;
    private float lockedStartX;

    private Animator animator;
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();

        float centerX = transform.position.x + startOffset;

        leftBoundary = centerX - patrolDistance;
        rightBoundary = centerX + patrolDistance;

        startMoveTimer = startMoveDelay;

        if (rb != null)
        {
            lockedStartX = rb.position.x;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            rb.angularVelocity = 0f;
        }
        else
        {
            lockedStartX = transform.position.x;
        }

        FaceDirection(movingRight);
        SetRunningState(false);
    }

    private void FixedUpdate()
    {
        if (IsDead)
            return;

        bool grounded = IsGrounded();

        if (startMoveTimer > 0f)
        {
            StabilizeAtStart(grounded);
            return;
        }

        if (!grounded)
        {
            HandleAirborneState();
            return;
        }

        if (isWaiting)
        {
            if (stopHorizontalVelocityWhileWaiting)
                StopHorizontalMovement();

            waitTimer -= Time.fixedDeltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                movingRight = !movingRight;

                FaceDirection(movingRight);
                SetRunningState(true);
            }

            return;
        }

        Move();
        CheckBoundaries();
    }

    private void StabilizeAtStart(bool grounded)
    {
        SetRunningState(false);

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            rb.angularVelocity = 0f;

            if (lockHorizontalPositionAtStart)
            {
                rb.position = new Vector2(lockedStartX, rb.position.y);
                transform.position = new Vector3(
                    lockedStartX,
                    transform.position.y,
                    transform.position.z
                );
            }
        }

        // لا يبدأ العدو الحركة إلا بعد أن يكون فعلاً على الأرض
        if (grounded)
            startMoveTimer -= Time.fixedDeltaTime;
    }

    private bool IsGrounded()
    {
        Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;

        return Physics2D.OverlapBox(
            checkCenter,
            groundCheckSize,
            0f,
            groundLayer
        ) != null;
    }

    private void HandleAirborneState()
    {
        SetRunningState(false);

        if (stopHorizontalVelocityInAir)
            StopHorizontalMovement();
    }

    private void Move()
    {
        float direction = movingRight ? 1f : -1f;

        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            rb.velocity = new Vector2(direction * MoveSpeed, rb.velocity.y);
        }
        else
        {
            transform.position += Vector3.right * direction * MoveSpeed * Time.fixedDeltaTime;
        }

        SetRunningState(true);
    }

    private void CheckBoundaries()
    {
        float currentX = transform.position.x;

        if (movingRight && currentX >= rightBoundary)
        {
            SnapToBoundary(rightBoundary);
            EnterWaitState();
        }
        else if (!movingRight && currentX <= leftBoundary)
        {
            SnapToBoundary(leftBoundary);
            EnterWaitState();
        }
    }

    private void SnapToBoundary(float boundaryX)
    {
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            rb.angularVelocity = 0f;
            rb.position = new Vector2(boundaryX, rb.position.y);

            transform.position = new Vector3(
                boundaryX,
                transform.position.y,
                transform.position.z
            );

            Physics2D.SyncTransforms();
        }
        else
        {
            transform.position = new Vector3(
                boundaryX,
                transform.position.y,
                transform.position.z
            );
        }
    }

    private void EnterWaitState()
    {
        isWaiting = true;
        waitTimer = waitTimeAtEdge;

        StopHorizontalMovement();
        SetRunningState(false);
    }

    private void StopHorizontalMovement()
    {
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            rb.angularVelocity = 0f;
        }
    }

    private void SetRunningState(bool isRunning)
    {
        if (animator != null)
            animator.SetBool(IsRunningParam, isRunning);
    }

    private void FaceDirection(bool faceRight)
    {
        if (!flipSprite)
            return;

        Vector3 scale = transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        float centerX;
        float left;
        float right;

        if (Application.isPlaying)
        {
            centerX = (leftBoundary + rightBoundary) * 0.5f;
            left = leftBoundary;
            right = rightBoundary;
        }
        else
        {
            centerX = transform.position.x + startOffset;
            left = centerX - patrolDistance;
            right = centerX + patrolDistance;
        }

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            new Vector3(left, transform.position.y - 0.5f, transform.position.z),
            new Vector3(left, transform.position.y + 0.5f, transform.position.z)
        );

        Gizmos.DrawLine(
            new Vector3(right, transform.position.y - 0.5f, transform.position.z),
            new Vector3(right, transform.position.y + 0.5f, transform.position.z)
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            new Vector3(centerX, transform.position.y, transform.position.z),
            new Vector3(patrolDistance * 2f, 1f, 0f)
        );

        Gizmos.color = Color.green;

        Vector3 groundCheckCenter = transform.position + (Vector3)groundCheckOffset;
        Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);
    }
}