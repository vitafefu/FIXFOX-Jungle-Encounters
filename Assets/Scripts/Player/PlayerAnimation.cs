using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (controller == null) return;

        animator.SetFloat("Speed", Mathf.Abs(controller.MoveInput));
        animator.SetBool("IsGround", controller.IsGrounded);
        animator.SetFloat("YVelocity", controller.VerticalSpeed);

        if (controller.MoveInput > 0.01f)
            spriteRenderer.flipX = false;
        else if (controller.MoveInput < -0.01f)
            spriteRenderer.flipX = true;
    }
}