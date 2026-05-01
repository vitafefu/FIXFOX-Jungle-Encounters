using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerSensors))]
public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float groundAcceleration = 55f;
    [SerializeField] private float groundDeceleration = 65f;
    [SerializeField] private float airAcceleration = 28f;
    [SerializeField] private float airDeceleration = 20f;

    [Header("Run")]
    [SerializeField] private bool allowShiftRun = true;
    [SerializeField] private bool allowDoubleTapRun = true;
    [SerializeField] private float doubleTapTime = 0.25f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float jumpCutMultiplier = 0.50f;
    [SerializeField] private float maxFallSpeed = 18f;

    [Header("Startup Safety")]
    [SerializeField] private float jumpLockTimeOnStart = 0.20f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [SerializeField] private float crouchColliderHeightMultiplier = 0.55f;
    [SerializeField] private bool blockJumpWhileCrouching = true;

    [Header("Ladder")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float ladderHorizontalSpeed = 2.5f;
    [SerializeField] private bool jumpExitsLadder = true;

    [Header("Debug")]
    [SerializeField] private string debugCurrentStateName;
    [SerializeField] private bool debugSensorGrounded;
    [SerializeField] private bool debugControllerGrounded;
    [SerializeField] private bool debugIsClimbing;
    [SerializeField] private bool debugIsCrouching;
    [SerializeField] private bool debugIsOnLadder;
    [SerializeField] private bool debugIsLadderAtBody;
    [SerializeField] private bool debugIsLadderBelowFeet;
    [SerializeField] private bool debugIsAtLadderTop;
    [SerializeField] private bool debugGroundCollisionIgnored;
    [SerializeField] private int debugIgnoredGroundColliderCount;
    [SerializeField] private float debugVerticalSpeed;
    [SerializeField] private float debugHorizontalSpeed;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCol;
    private PlayerSensors sensors;

    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private Vector2 crouchingColliderSize;
    private Vector2 crouchingColliderOffset;

    private float moveInputX;
    private float moveInputY;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool crouchHeld;

    private float lastGroundedTime = -10f;
    private float lastJumpPressedTime = -10f;
    private float lastLeftTapTime = -10f;
    private float lastRightTapTime = -10f;
    private float startupJumpLockTimer;

    private bool isRunning;
    private int runDirection;
    private bool facingRight = true;
    private bool isClimbing;
    private bool isCrouching;
    private bool isGroundCollisionIgnored;
    private float defaultGravityScale;

    private readonly Collider2D[] groundIgnoreHits = new Collider2D[16];
    private readonly Collider2D[] ignoredGroundColliders = new Collider2D[16];
    private int ignoredGroundColliderCount;

    private PlayerMovementState currentState;
    private GroundState groundState;
    private AirState airState;
    private LadderState ladderState;
    private WaterState waterState;

    public Rigidbody2D Rb => rb;
    public CapsuleCollider2D CapsuleCol => capsuleCol;
    public PlayerSensors Sensors => sensors;

    public GroundState GroundState => groundState;
    public AirState AirState => airState;
    public LadderState LadderState => ladderState;
    public WaterState WaterState => waterState;

    public float MoveInputX => moveInputX;
    public float MoveInputY => moveInputY;
    public bool JumpPressed => jumpPressed;
    public bool JumpReleased => jumpReleased;
    public bool CrouchHeld => crouchHeld;
    public bool FacingRight => facingRight;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float GroundAcceleration => groundAcceleration;
    public float GroundDeceleration => groundDeceleration;
    public float AirAcceleration => airAcceleration;
    public float AirDeceleration => airDeceleration;
    public float JumpForce => jumpForce;
    public float JumpCutMultiplier => jumpCutMultiplier;
    public float MaxFallSpeed => maxFallSpeed;
    public float CrouchSpeedMultiplier => crouchSpeedMultiplier;
    public float ClimbSpeed => climbSpeed;
    public float LadderHorizontalSpeed => ladderHorizontalSpeed;
    public bool JumpExitsLadder => jumpExitsLadder;
    public float DefaultGravityScale => defaultGravityScale;

    public bool IsRunning => isRunning;
    public bool IsClimbing => isClimbing;
    public bool IsCrouching => isCrouching;

    public string CurrentStateName => currentState != null ? currentState.GetType().Name : "None";
    public bool DebugSensorGrounded => sensors != null && sensors.IsGrounded;

    public float AnimationSpeed
    {
        get
        {
            if (isClimbing)
                return Mathf.Clamp01(Mathf.Abs(rb.velocity.y) / Mathf.Max(0.01f, climbSpeed));

            return Mathf.Clamp01(Mathf.Abs(rb.velocity.x) / Mathf.Max(0.01f, runSpeed));
        }
    }

    public bool IsGrounded => sensors != null && sensors.IsGrounded && !isClimbing;
    public float VerticalSpeed => rb != null ? rb.velocity.y : 0f;

    public bool IsInWater => false;
    public bool IsInWaterZone => false;
    public bool WillLandInWater => false;
    public bool IsWaterJumpLocked => false;

    public bool IsOnLadder => sensors != null && sensors.IsLadderAtBody;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCol = GetComponent<CapsuleCollider2D>();
        sensors = GetComponent<PlayerSensors>();

        defaultGravityScale = rb.gravityScale;

        standingColliderSize = capsuleCol.size;
        standingColliderOffset = capsuleCol.offset;

        crouchingColliderSize = new Vector2(
            standingColliderSize.x,
            standingColliderSize.y * crouchColliderHeightMultiplier
        );

        float heightDifference = standingColliderSize.y - crouchingColliderSize.y;

        /*
         * نحسب أوفست الكروش بحيث يصغر الكابسول من الأعلى غالبًا.
         * ومع ذلك، الدالة ApplyColliderSizePreserveFeet ستضمن أن القدم لا تتحرك نهائيًا.
         */
        crouchingColliderOffset = standingColliderOffset - new Vector2(0f, heightDifference * 0.5f);

        startupJumpLockTimer = jumpLockTimeOnStart;
        lastGroundedTime = -10f;
        lastJumpPressedTime = -10f;

        groundState = new GroundState(this);
        airState = new AirState(this);
        ladderState = new LadderState(this);
        waterState = new WaterState(this);
    }

    private void Start()
    {
        sensors.Refresh();
        ChangeState(sensors.IsGrounded ? groundState : airState);
        UpdateDebugInfo();
    }

    private void Update()
    {
        if (startupJumpLockTimer > 0f)
            startupJumpLockTimer -= Time.deltaTime;

        ReadInput();
        UpdateFacingDirection();
        UpdateRunInput();
        StopRunIfNeeded();

        currentState?.Tick();
    }

    private void FixedUpdate()
    {
        sensors.Refresh();

        if (sensors.IsGrounded)
            lastGroundedTime = Time.time;

        ResolveLandingStateBeforeTick();

        currentState?.FixedTick();

        UpdateDebugInfo();

        jumpPressed = false;
        jumpReleased = false;
    }

    private void ResolveLandingStateBeforeTick()
    {
        if (isClimbing)
            return;

        if (!sensors.IsGrounded)
            return;

        if (currentState == groundState)
            return;

        Vector2 velocity = rb.velocity;

        if (velocity.y < 0f)
            velocity.y = 0f;

        rb.velocity = velocity;

        ChangeState(groundState);
    }

    private void UpdateDebugInfo()
    {
        debugCurrentStateName = currentState != null ? currentState.GetType().Name : "None";
        debugSensorGrounded = sensors != null && sensors.IsGrounded;
        debugControllerGrounded = IsGrounded;
        debugIsClimbing = isClimbing;
        debugIsCrouching = isCrouching;
        debugIsOnLadder = IsOnLadder;

        debugIsLadderAtBody = sensors != null && sensors.IsLadderAtBody;
        debugIsLadderBelowFeet = sensors != null && sensors.IsLadderBelowFeet;
        debugIsAtLadderTop = sensors != null && sensors.IsAtLadderTop;

        debugGroundCollisionIgnored = isGroundCollisionIgnored;
        debugIgnoredGroundColliderCount = ignoredGroundColliderCount;

        debugVerticalSpeed = rb != null ? rb.velocity.y : 0f;
        debugHorizontalSpeed = rb != null ? rb.velocity.x : 0f;
    }

    public void ChangeState(PlayerMovementState newState)
    {
        if (newState == null || currentState == newState)
            return;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void ReadInput()
    {
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputY = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveInputY = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveInputY = -1f;

        crouchHeld =
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.LeftControl);

        if (Input.GetButtonDown("Jump") && startupJumpLockTimer <= 0f)
        {
            jumpPressed = true;
            lastJumpPressedTime = Time.time;
        }

        if (Input.GetButtonUp("Jump"))
            jumpReleased = true;
    }

    private void UpdateFacingDirection()
    {
        if (moveInputX > 0.01f)
            facingRight = true;
        else if (moveInputX < -0.01f)
            facingRight = false;
    }

    public float GetTargetHorizontalSpeed()
    {
        float speed = isRunning ? runSpeed : walkSpeed;

        if (isCrouching)
            speed *= crouchSpeedMultiplier;

        return moveInputX * speed;
    }

    public float GetHorizontalAcceleration(float targetSpeed, bool grounded)
    {
        bool wantsMove = Mathf.Abs(targetSpeed) > 0.01f;

        if (grounded)
            return wantsMove ? groundAcceleration : groundDeceleration;

        return wantsMove ? airAcceleration : airDeceleration;
    }

    public bool CanUseBufferedJump()
    {
        if (startupJumpLockTimer > 0f)
            return false;

        bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool hasCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;

        if (!hasBufferedJump || !hasCoyoteTime)
            return false;

        if (blockJumpWhileCrouching && isCrouching)
            return false;

        return true;
    }

    public void ConsumeJump()
    {
        lastJumpPressedTime = -10f;
        lastGroundedTime = -10f;
    }

    public void UpdateCrouchState()
    {
        if (!sensors.IsGrounded)
        {
            if (isCrouching && CanStandUp())
            {
                isCrouching = false;
                ApplyStandingCollider();
            }

            return;
        }

        if (crouchHeld)
        {
            if (!isCrouching)
            {
                isCrouching = true;
                ApplyCrouchingCollider();
            }

            return;
        }

        if (isCrouching && CanStandUp())
        {
            isCrouching = false;
            ApplyStandingCollider();
        }
    }

    public bool CanStandUp()
    {
        return sensors.CanUseStandingCollider(standingColliderSize, standingColliderOffset);
    }

    public void ApplyStandingCollider()
    {
        ApplyColliderSizePreserveFeet(
            standingColliderSize,
            standingColliderOffset
        );
    }

    public void ApplyCrouchingCollider()
    {
        ApplyColliderSizePreserveFeet(
            crouchingColliderSize,
            crouchingColliderOffset
        );
    }

    private void ApplyColliderSizePreserveFeet(Vector2 newSize, Vector2 newOffset)
    {
        /*
         * هذا هو حل مشكلة الكروش:
         * نحفظ مكان أسفل الكابسول قبل تغيير الحجم،
         * ثم بعد التغيير نرجع اللاعب بحيث تبقى القدم في نفس المكان.
         */

        float bottomBefore = capsuleCol.bounds.min.y;

        capsuleCol.size = newSize;
        capsuleCol.offset = newOffset;

        Physics2D.SyncTransforms();

        float bottomAfter = capsuleCol.bounds.min.y;
        float deltaY = bottomBefore - bottomAfter;

        if (Mathf.Abs(deltaY) > 0.0001f)
        {
            rb.position += new Vector2(0f, deltaY);
            Physics2D.SyncTransforms();
        }

        if (sensors != null)
            sensors.Refresh();

        if (sensors != null && sensors.IsGrounded)
        {
            Vector2 velocity = rb.velocity;

            if (velocity.y < 0f)
                velocity.y = 0f;

            rb.velocity = velocity;
            lastGroundedTime = Time.time;
        }
    }

    public bool ShouldEnterLadder()
    {
        if (isClimbing)
            return false;

        if (sensors == null)
            return false;

        bool wantsClimbUp = moveInputY > 0.01f;
        bool wantsClimbDown = moveInputY < -0.01f;

        if (wantsClimbUp)
        {
            return sensors.IsLadderAtBody && !sensors.IsAtLadderTop;
        }

        if (wantsClimbDown)
        {
            /*
             * إذا جسم اللاعب أصلًا على السلم، اسمح له بالنزول.
             */
            if (sensors.IsLadderAtBody)
                return true;

            /*
             * إذا اللاعب واقف فوق فتحة السلم:
             * لا ندخل السلم إلا عندما يكون شبه ثابت أفقيًا.
             * هذا يمنع زر S من تفعيل السلم بالغلط أثناء محاولة الكروش أو الحركة.
             */
            bool isTryingToMoveHorizontally = Mathf.Abs(moveInputX) > 0.01f;
            bool isMovingHorizontally = Mathf.Abs(rb.velocity.x) > 0.15f;

            if (sensors.IsLadderBelowFeet && !isTryingToMoveHorizontally && !isMovingHorizontally)
                return true;
        }

        return false;
    }

    public void SetGroundCollisionIgnored(bool ignored)
    {
        if (sensors == null)
            return;

        if (ignored)
        {
            if (isGroundCollisionIgnored)
                return;

            IgnoreGroundLayerCollision(true);
            IgnoreNearbyGroundColliders(true);

            isGroundCollisionIgnored = true;
            return;
        }

        RestoreIgnoredGroundColliders();
        IgnoreGroundLayerCollision(false);

        isGroundCollisionIgnored = false;
    }

    private void IgnoreGroundLayerCollision(bool ignored)
    {
        int playerLayer = gameObject.layer;
        int groundMask = sensors.GroundLayer.value;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((groundMask & (1 << layer)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, layer, ignored);
        }
    }

    private void IgnoreNearbyGroundColliders(bool ignored)
    {
        Bounds b = capsuleCol.bounds;

        Vector2 boxCenter = new Vector2(
            b.center.x,
            b.center.y - 0.15f
        );

        Vector2 boxSize = new Vector2(
            b.size.x + 0.20f,
            b.size.y + 0.35f
        );

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = sensors.GroundLayer;
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapBox(
            boxCenter,
            boxSize,
            0f,
            filter,
            groundIgnoreHits
        );

        ignoredGroundColliderCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = groundIgnoreHits[i];

            if (hit == null)
                continue;

            Physics2D.IgnoreCollision(capsuleCol, hit, ignored);

            if (ignoredGroundColliderCount < ignoredGroundColliders.Length)
            {
                ignoredGroundColliders[ignoredGroundColliderCount] = hit;
                ignoredGroundColliderCount++;
            }

            groundIgnoreHits[i] = null;
        }
    }

    private void RestoreIgnoredGroundColliders()
    {
        for (int i = 0; i < ignoredGroundColliderCount; i++)
        {
            if (ignoredGroundColliders[i] != null)
            {
                Physics2D.IgnoreCollision(
                    capsuleCol,
                    ignoredGroundColliders[i],
                    false
                );

                ignoredGroundColliders[i] = null;
            }
        }

        ignoredGroundColliderCount = 0;
    }

    public void SetClimbing(bool value)
    {
        isClimbing = value;
    }

    public void SetCrouching(bool value)
    {
        isCrouching = value;
    }

    public void ResetRun()
    {
        isRunning = false;
        runDirection = 0;
    }

    private void UpdateRunInput()
    {
        if (allowShiftRun)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                isRunning = Mathf.Abs(moveInputX) > 0.01f && !isCrouching;
                runDirection = moveInputX > 0f ? 1 : moveInputX < 0f ? -1 : 0;
                return;
            }
        }

        if (!allowDoubleTapRun)
            return;

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
        if (allowShiftRun && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            return;

        if (Mathf.Abs(moveInputX) < 0.01f || isCrouching)
        {
            ResetRun();
            return;
        }

        if ((runDirection == 1 && moveInputX < 0f) || (runDirection == -1 && moveInputX > 0f))
            ResetRun();
    }

    private void OnDisable()
    {
        SetGroundCollisionIgnored(false);
    }
}