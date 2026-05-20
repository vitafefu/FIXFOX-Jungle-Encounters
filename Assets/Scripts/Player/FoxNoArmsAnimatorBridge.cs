using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class FoxNoArmsAnimatorBridge : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D playerRb;

    private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (controller != null)
            playerRb = controller.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (animator == null)
            return;

        bool isRunning = false;

        // Способ 1: через PlayerController
        if (controller != null)
        {
            isRunning = controller.IsRunning;
        }
        // Способ 2: если controller недоступен — проверяем скорость Rigidbody2D
        else if (playerRb != null)
        {
            isRunning = Mathf.Abs(playerRb.velocity.x) > 0.1f;
        }
        // Способ 3: проверяем Input
        else
        {
            isRunning = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
        }

        animator.SetBool(IsRunningParam, isRunning);

        // Поворот спрайта
        if (spriteRenderer != null)
        {
            if (controller != null)
                spriteRenderer.flipX = !controller.FacingRight;
            else if (playerRb != null && Mathf.Abs(playerRb.velocity.x) > 0.1f)
                spriteRenderer.flipX = playerRb.velocity.x < 0;
        }
    }
}