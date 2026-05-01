using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimatorBridge : MonoBehaviour
{
    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool hasSpeed;
    private bool hasHorizontalSpeed;
    private bool hasVerticalSpeed;
    private bool hasMoveX;
    private bool hasMoveY;
    private bool hasAnimationSpeed;

    private bool hasIsGrounded;
    private bool hasIsClimbing;
    private bool hasIsCrouching;
    private bool hasIsRunning;
    private bool hasIsFalling;
    private bool hasIsJumping;
    private bool hasIsOnLadder;
    private bool hasIsInWater;
    private bool hasIsSwimming;
    private bool hasIsWaterJumping;

    private bool hasJumpTrigger;
    private bool hasLandTrigger;
    private bool hasWaterEnterTrigger;

    private bool wasGrounded;
    private bool wasInWater;

    private void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        CacheAnimatorParameters();
    }

    private void Start()
    {
        if (controller == null)
            return;

        wasGrounded = controller.IsGrounded;
        wasInWater = false;
    }

    private void LateUpdate()
    {
        if (controller == null || animator == null)
            return;

        float moveX = controller.MoveInputX;
        float moveY = controller.MoveInputY;

        float horizontalSpeed = Mathf.Abs(controller.Rb.velocity.x);
        float verticalSpeed = controller.VerticalSpeed;
        float animationSpeed = controller.AnimationSpeed;

        /*
         * Speed هنا نخليها normalized مثل AnimationSpeed
         * حتى تبقى مناسبة للـ transitions مثل Speed > 0.05
         */
        float speed = animationSpeed;

        bool isGrounded = controller.IsGrounded;
        bool isClimbing = controller.IsClimbing;
        bool isCrouching = controller.IsCrouching;
        bool isRunning = controller.IsRunning;
        bool isOnLadder = controller.IsOnLadder;

        /*
         * الماء لم نفعّله بعد.
         * نثبتها false الآن حتى لا تدخل حالات الماء بالغلط.
         */
        bool isInWater = false;
        bool isSwimming = false;
        bool isWaterJumping = false;

        bool isJumping =
            !isGrounded &&
            !isClimbing &&
            !isInWater &&
            verticalSpeed > 0.05f;

        bool isFalling =
            !isGrounded &&
            !isClimbing &&
            !isInWater &&
            verticalSpeed < -0.05f;

        if (hasSpeed)
            animator.SetFloat("Speed", speed);

        if (hasHorizontalSpeed)
            animator.SetFloat("HorizontalSpeed", horizontalSpeed);

        if (hasVerticalSpeed)
            animator.SetFloat("VerticalSpeed", verticalSpeed);

        if (hasMoveX)
            animator.SetFloat("MoveX", moveX);

        if (hasMoveY)
            animator.SetFloat("MoveY", moveY);

        if (hasAnimationSpeed)
            animator.SetFloat("AnimationSpeed", animationSpeed);

        if (hasIsGrounded)
            animator.SetBool("IsGrounded", isGrounded);

        if (hasIsClimbing)
            animator.SetBool("IsClimbing", isClimbing);

        if (hasIsCrouching)
            animator.SetBool("IsCrouching", isCrouching);

        if (hasIsRunning)
            animator.SetBool("IsRunning", isRunning);

        if (hasIsFalling)
            animator.SetBool("IsFalling", isFalling);

        if (hasIsJumping)
            animator.SetBool("IsJumping", isJumping);

        if (hasIsOnLadder)
            animator.SetBool("IsOnLadder", isOnLadder);

        if (hasIsInWater)
            animator.SetBool("IsInWater", isInWater);

        if (hasIsSwimming)
            animator.SetBool("IsSwimming", isSwimming);

        if (hasIsWaterJumping)
            animator.SetBool("IsWaterJumping", isWaterJumping);

        if (hasJumpTrigger && isJumping && !wasGrounded)
            animator.ResetTrigger("Jump");

        if (hasJumpTrigger && isJumping && wasGrounded)
            animator.SetTrigger("Jump");

        if (hasLandTrigger && !wasGrounded && isGrounded)
            animator.SetTrigger("Land");

        /*
         * الماء غير مفعّل الآن.
         * لذلك لا نطلق WaterEnter.
         */
        if (hasWaterEnterTrigger && !wasInWater && isInWater)
            animator.SetTrigger("WaterEnter");

        wasGrounded = isGrounded;
        wasInWater = isInWater;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !controller.FacingRight;
    }

    private void CacheAnimatorParameters()
    {
        hasSpeed = HasParameter("Speed", AnimatorControllerParameterType.Float);
        hasHorizontalSpeed = HasParameter("HorizontalSpeed", AnimatorControllerParameterType.Float);
        hasVerticalSpeed = HasParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        hasMoveX = HasParameter("MoveX", AnimatorControllerParameterType.Float);
        hasMoveY = HasParameter("MoveY", AnimatorControllerParameterType.Float);
        hasAnimationSpeed = HasParameter("AnimationSpeed", AnimatorControllerParameterType.Float);

        hasIsGrounded = HasParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        hasIsClimbing = HasParameter("IsClimbing", AnimatorControllerParameterType.Bool);
        hasIsCrouching = HasParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        hasIsRunning = HasParameter("IsRunning", AnimatorControllerParameterType.Bool);
        hasIsFalling = HasParameter("IsFalling", AnimatorControllerParameterType.Bool);
        hasIsJumping = HasParameter("IsJumping", AnimatorControllerParameterType.Bool);
        hasIsOnLadder = HasParameter("IsOnLadder", AnimatorControllerParameterType.Bool);
        hasIsInWater = HasParameter("IsInWater", AnimatorControllerParameterType.Bool);
        hasIsSwimming = HasParameter("IsSwimming", AnimatorControllerParameterType.Bool);
        hasIsWaterJumping = HasParameter("IsWaterJumping", AnimatorControllerParameterType.Bool);

        hasJumpTrigger = HasParameter("Jump", AnimatorControllerParameterType.Trigger);
        hasLandTrigger = HasParameter("Land", AnimatorControllerParameterType.Trigger);
        hasWaterEnterTrigger = HasParameter("WaterEnter", AnimatorControllerParameterType.Trigger);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }
}