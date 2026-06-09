using UnityEngine;

public class Boss : Enemy
{
    [Header("Boss: Patrol")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private bool startMovingRight = false;

    [Header("Boss: Vision & Chase")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Boss: Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackPauseDuration = 1f;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite idleSprite1;
    [SerializeField] private Sprite idleSprite2;
    [SerializeField] private float animationSpeed = 0.2f;
    [SerializeField] private bool spriteLooksRightByDefault = false;

    [Header("Safe Zone (Player Start X only)")]
    [SerializeField] private float playerStartX = -122.08f;   // только X координата
    [SerializeField] private float safeZoneRadiusX = 2f;      // расстояние по X, при котором срабатывает отступление
    [SerializeField] private float retreatDistance = 30f;
    [SerializeField] private float retreatSpeed = 5f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private bool isAttacking;
    private float attackTimer;
    private float attackPauseTimer;
    private float animationTimer;
    private bool showFirstSprite = true;

    private float leftBoundary;
    private float rightBoundary;
    private float startX;          // стартовая позиция босса (X)

    private bool isRetreating = false;
    private float retreatTargetX;
    private bool retreatingRight;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;
    private bool movingRight;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (idleSprite1 != null) spriteRenderer.sprite = idleSprite1;

        startX = transform.position.x;
        leftBoundary = startX - patrolDistance;
        rightBoundary = startX + patrolDistance;

        movingRight = startMovingRight;
        UpdateFacing();
    }

    private void Update()
    {
        if (IsDead) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (attackPauseTimer > 0) attackPauseTimer -= Time.deltaTime;

        // Проверка безопасной зоны только по X
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

        // Анимация спрайтов
        if (!isAttacking && !isRetreating)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0;
                showFirstSprite = !showFirstSprite;
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
                case State.Patrol: Patrol(); break;
                case State.Chase: ChasePlayer(); break;
                case State.Attack: StopMoving(); break;
            }
        }
        UpdateFacing();
    }

    private void DetermineState()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
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
            PerformAttack();
            DetermineState();
        }
    }

    private void StartRetreat()
    {
        if (isRetreating) return;
        isRetreating = true;

        float bossX = transform.position.x;

        // Отступаем в сторону, противоположную направлению к стартовой позиции игрока
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

        Debug.Log($"Retreat started. Target X: {retreatTargetX}");
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
            Debug.Log("Retreat finished.");
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

    private void StopMoving()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private void UpdateFacing()
    {
        bool shouldFaceRight = movingRight;
        if (spriteLooksRightByDefault)
            spriteRenderer.flipX = !shouldFaceRight;
        else
            spriteRenderer.flipX = shouldFaceRight;
    }

    private void PerformAttack()
    {
        Debug.Log("BOSS ATTACK (placeholder)");
    }

    private void OnDrawGizmosSelected()
    {
        float left, right;
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
        Gizmos.DrawLine(new Vector3(left, transform.position.y - 0.5f), new Vector3(left, transform.position.y + 0.5f));
        Gizmos.DrawLine(new Vector3(right, transform.position.y - 0.5f), new Vector3(right, transform.position.y + 0.5f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Визуализация безопасной зоны (только по X) – вертикальная линия с кружками
        Gizmos.color = Color.green;
        Vector3 safeCenter = new Vector3(playerStartX, transform.position.y, 0);
        Gizmos.DrawWireSphere(safeCenter, safeZoneRadiusX);
        // Дополнительно: вертикальная линия для наглядности
        Gizmos.DrawLine(new Vector3(playerStartX, transform.position.y - 0.5f), new Vector3(playerStartX, transform.position.y + 0.5f));
    }
}