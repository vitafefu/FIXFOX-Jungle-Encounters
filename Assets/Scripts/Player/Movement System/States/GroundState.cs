using UnityEngine;

public class GroundState : PlayerMovementState
{
    public GroundState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Rb.gravityScale = controller.DefaultGravityScale;
        controller.SetClimbing(false);

        Vector2 velocity = controller.Rb.velocity;

        if (velocity.y < 0f)
            velocity.y = 0f;

        controller.Rb.velocity = velocity;
    }

    public override void FixedTick()
    {
        /*
         * مهم:
         * نفحص السلم قبل الكروش.
         * لأن زر S يستخدم للكروش وللنزول من السلم.
         * ShouldEnterLadder هو المسؤول عن التمييز:
         * - إذا اللاعب فوق فتحة سلم فعلًا → يدخل LadderState
         * - إذا ليس فوق فتحة سلم → يكمل للكروش طبيعي
         */
        if (controller.ShouldEnterLadder())
        {
            controller.ChangeState(controller.LadderState);
            return;
        }

        controller.UpdateCrouchState();

        if (!controller.Sensors.IsGrounded)
        {
            controller.ChangeState(controller.AirState);
            return;
        }

        Vector2 velocity = controller.Rb.velocity;

        if (velocity.y < 0f)
            velocity.y = 0f;

        float targetSpeed = controller.GetTargetHorizontalSpeed();
        float acceleration = controller.GetHorizontalAcceleration(targetSpeed, true);

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        if (controller.CanUseBufferedJump())
        {
            velocity.y = controller.JumpForce;
            controller.ConsumeJump();
            controller.SetCrouching(false);
            controller.ApplyStandingCollider();
            controller.Rb.velocity = velocity;
            controller.ChangeState(controller.AirState);
            return;
        }

        controller.Rb.velocity = velocity;
    }
}