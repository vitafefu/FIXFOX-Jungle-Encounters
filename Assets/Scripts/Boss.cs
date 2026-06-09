using UnityEngine;

public class Boss : Enemy
{
    [Header("Boss: Patrol")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private bool startMovingRight = false;

    [Header("Boss: Vision & Chase")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Boss: Attack")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackPauseDuration = 0.5f;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite idleSprite1;
    [SerializeField] private Sprite idleSprite2;
    [SerializeField] private float animationSpeed = 0.2f;
    [SerializeField] private bool spriteLooksRightByDefault = false;

    [Header("Safe Zone (Player Start X only)")]
    [SerializeField] private float playerStartX = -122.08f;
    [SerializeField] private float safeZoneRadiusX = 2f;
    [SerializeField] private float retreatDistance = 30f;
    [SerializeField] private float retreatSpeed = 5f;

    [Header("Boss: Fireball")]
    public GameObject fireballPrefab;
    public Transform firePoint;

    private Transform player;
    private SpriteRenderer spriteRenderer;

    private bool isAttacking;
    private float attackTimer;
    private float attackPauseTimer;
    private float animationTimer;
    private bool showFirstSprite = true;

    private float leftBoundary;
    private float rightBoundary;
    private float startX;

    private bool isRetreating = false;
    private float retreatTargetX;
    private bool retreatingRight;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;
    private bool movingRight;

    private float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (idleSprite1 != null)
            spriteRenderer.sprite = idleSprite1;

        startX = transform.position.x;
        leftBoundary = startX - patrolDistance;
        rightBoundary = startX + patrolDistance;

        movingRight = startMovingRight;
        UpdateFacing();

        lastAttackTime = Time.time;
    }

    private void Update()
    {
        if (IsDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        if (attackPauseTimer > 0)
            attackPauseTimer -= Time.deltaTime;

        if (!isRetreating)
        {
            float distanceX = Mathf.Abs(transform.position.x - playerStartX);
            if (distanceX <= safeZoneRadiusX)
            {
                StartRetreat();
            }
        }

        if (!isRetreating)
        {
            DetermineState();
        }

        if (!isRetreating && player != null && fireballPrefab != null && firePoint != null)
        {
            float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);
            bool canSeePlayer = distanceToPlayer <= detectionRange;

            if (canSeePlayer && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                ShootFireball();
            }
        }

        if (!isAttacking && !isRetreating)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0;
                showFirstSprite = !showFirstSprite;

                if (idleSprite1 != null && idleSprite2 != null)
                    spriteRenderer.sprite = showFirstSprite ? idleSprite1 : idleSprite2;
            }
        }
        else if (isAttacking)
        {
            if (spriteRenderer.sprite != idleSprite1 && idleSprite1 != null)
                spriteRenderer.sprite = idleSprite1;
        }
        else if (isRetreating)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0;
                showFirstSprite = !showFirstSprite;

                if (idleSprite1 != null && idleSprite2 != null)
                    spriteRenderer.sprite = showFirstSprite ? idleSprite1 : idleSprite2;
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (isRetreating)
        {
            Retreat();
        }
        else
        {
            switch (currentState)
            {
                case State.Patrol:
                    Patrol();
                    break;

                case State.Chase:
                    ChasePlayer();
                    break;

                case State.Attack:
                    ChasePlayer();
                    break;
            }
        }

        UpdateFacing();
    }

    private void DetermineState()
    {
        if (player == null) return;

        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);
        bool canAttack = attackTimer <= 0 && !isAttacking && attackPauseTimer <= 0;

        if (distanceToPlayer <= attackRange && canAttack)
        {
            currentState = State.Attack;
            isAttacking = true;
            attackPauseTimer = attackPauseDuration;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = State.Chase;
            isAttacking = false;
        }
        else
        {
            currentState = State.Patrol;
            isAttacking = false;
        }

        if (isAttacking && attackPauseTimer <= 0)
        {
            isAttacking = false;
            attackTimer = attackCooldown;
            DetermineState();
        }
    }

    private void StartRetreat()
    {
        if (isRetreating) return;

        isRetreating = true;

        float bossX = transform.position.x;

        if (bossX < playerStartX)
        {
            retreatingRight = false;
            retreatTargetX = bossX - retreatDistance;
        }
        else
        {
            retreatingRight = true;
            retreatTargetX = bossX + retreatDistance;
        }
    }

    private void Retreat()
    {
        float direction = retreatingRight ? 1f : -1f;
        rb.velocity = new Vector2(direction * retreatSpeed, rb.velocity.y);
        movingRight = retreatingRight;

        if ((retreatingRight && transform.position.x >= retreatTargetX) ||
            (!retreatingRight && transform.position.x <= retreatTargetX))
        {
            isRetreating = false;
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void Patrol()
    {
        if (movingRight)
        {
            rb.velocity = new Vector2(patrolSpeed, rb.velocity.y);

            if (transform.position.x >= rightBoundary)
                movingRight = false;
        }
        else
        {
            rb.velocity = new Vector2(-patrolSpeed, rb.velocity.y);

            if (transform.position.x <= leftBoundary)
                movingRight = true;
        }
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(direction * chaseSpeed, rb.velocity.y);
        movingRight = direction > 0;
    }

    private void UpdateFacing()
    {
        bool shouldFaceRight = movingRight;

        if (spriteLooksRightByDefault)
            spriteRenderer.flipX = !shouldFaceRight;
        else
            spriteRenderer.flipX = shouldFaceRight;
    }

    private void ShootFireball()
    {
        if (player == null || fireballPrefab == null || firePoint == null) return;

        Collider2D bossCollider = GetComponent<Collider2D>();
        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }

        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

        Vector2 direction = (player.position - firePoint.position).normalized;

        Rigidbody2D rbFireball = fireball.GetComponent<Rigidbody2D>();
        if (rbFireball != null)
        {
            rbFireball.velocity = direction * 12f;
            rbFireball.gravityScale = 0f;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        fireball.transform.rotation = Quaternion.Euler(0, 0, angle);

        if (bossCollider != null)
        {
            Invoke(nameof(EnableBossCollider), 0.1f);
        }
    }

    private void EnableBossCollider()
    {
        Collider2D bossCollider = GetComponent<Collider2D>();
        if (bossCollider != null)
        {
            bossCollider.enabled = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        float left;
        float right;

        if (Application.isPlaying)
        {
            left = leftBoundary;
            right = rightBoundary;
        }
        else
        {
            float center = transform.position.x;
            left = center - patrolDistance;
            right = center + patrolDistance;
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            new Vector3(left, transform.position.y - 0.5f),
            new Vector3(left, transform.position.y + 0.5f)
        );

        Gizmos.DrawLine(
            new Vector3(right, transform.position.y - 0.5f),
            new Vector3(right, transform.position.y + 0.5f)
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Vector3 safeCenter = new Vector3(playerStartX, transform.position.y, 0);
        Gizmos.DrawWireSphere(safeCenter, safeZoneRadiusX);

        Gizmos.DrawLine(
            new Vector3(playerStartX, transform.position.y - 0.5f),
            new Vector3(playerStartX, transform.position.y + 0.5f)
        );
    }
}