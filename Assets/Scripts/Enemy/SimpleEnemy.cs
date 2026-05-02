using UnityEngine;

/// <summary>
/// Простой враг, который двигается вправо и влево в пределах заданных зон относительно стартовой позиции.
/// При достижении границы стоит заданное время, затем бежит обратно.
/// </summary>
public class SimpleEnemy : Enemy
{
    [Header("Движение влево-вправо")]
    [SerializeField] private float moveDistance = 5f;
    [SerializeField] private float startOffset = 0f;
    [SerializeField] private float waitTimeAtEdge = 1.5f;

    [Header("Визуал")]
    [SerializeField] private bool flipSprite = true;

    private bool movingRight = true;
    private Vector3 startPosition;
    private float leftBoundary;
    private float rightBoundary;

    private bool isWaiting = false;
    private float waitTimer = 0f;

    private Animator animator;
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");

    protected override void Start()
    {
        base.Start();

        // Ищем аниматор на объекте и в дочерних (например, в Graphics)
        animator = GetComponentInChildren<Animator>();

        startPosition = transform.position;
        leftBoundary = startPosition.x - moveDistance + startOffset;
        rightBoundary = startPosition.x + moveDistance + startOffset;

        SetRunningState(true);
    }

    private void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                movingRight = !movingRight;
                if (flipSprite) FlipSprite(movingRight);
                SetRunningState(true);
            }
            return;
        }

        Move();
        CheckBoundaries();
    }

    private void Move()
    {
        float direction = movingRight ? 1f : -1f;
        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);
    }

    private void CheckBoundaries()
    {
        if (movingRight && transform.position.x >= rightBoundary)
        {
            EnterWaitState();
        }
        else if (!movingRight && transform.position.x <= leftBoundary)
        {
            EnterWaitState();
        }
    }

    private void EnterWaitState()
    {
        isWaiting = true;
        waitTimer = waitTimeAtEdge;
        SetRunningState(false);
    }

    private void SetRunningState(bool isRunning)
    {
        if (animator != null)
            animator.SetBool(IsRunningParam, isRunning);
    }

    private void FlipSprite(bool faceRight)
    {
        Vector3 scale = transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Vector3 pos = transform.position;
            float left = pos.x - moveDistance + startOffset;
            float right = pos.x + moveDistance + startOffset;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(left, pos.y - 0.5f, pos.z), new Vector3(left, pos.y + 0.5f, pos.z));
            Gizmos.DrawLine(new Vector3(right, pos.y - 0.5f, pos.z), new Vector3(right, pos.y + 0.5f, pos.z));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(pos.x + startOffset, pos.y, pos.z), new Vector3(moveDistance * 2, 1f, 0));
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(leftBoundary, transform.position.y - 0.5f, transform.position.z),
                           new Vector3(leftBoundary, transform.position.y + 0.5f, transform.position.z));
            Gizmos.DrawLine(new Vector3(rightBoundary, transform.position.y - 0.5f, transform.position.z),
                           new Vector3(rightBoundary, transform.position.y + 0.5f, transform.position.z));
        }
    }
}