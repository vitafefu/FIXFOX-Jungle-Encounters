using UnityEngine;

public class AirState : PlayerMovementState
{
    public AirState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Rb.gravityScale = controller.DefaultGravityScale;
        controller.SetClimbing(false);
    }

    public override void FixedTick()
    {
        if (controller.ShouldEnterLadder())
        {
            controller.ChangeState(controller.LadderState);
            return;
        }

        /*
         * If the sensors say we are grounded, we immediately return to GroundState.
         * Do not check velocity.y here.
         * On edges and side landings Unity can resolve the collision with velocity.y = 0,
         * or with a value that is not reliable for deciding the movement state.
         */
        if (controller.Sensors.IsGrounded)
        {
            Vector2 groundedVelocity = controller.Rb.velocity;

            if (groundedVelocity.y < 0f)
                groundedVelocity.y = 0f;

            controller.Rb.velocity = groundedVelocity;
            controller.ChangeState(controller.GroundState);
            return;
        }

        controller.UpdateCrouchState();

        Vector2 velocity = controller.Rb.velocity;

        float targetSpeed = controller.GetTargetHorizontalSpeed();
        float acceleration = controller.GetHorizontalAcceleration(targetSpeed, false);

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        if (controller.CanUseBufferedJump())
        {
            velocity.y = controller.JumpForce;
            controller.PlayerAudio?.PlayJump();

            controller.ConsumeJump();
            controller.SetCrouching(false);
            controller.ApplyStandingCollider();

            controller.Rb.velocity = velocity;
            return;
        }

        if (controller.JumpReleased && velocity.y > 0f)
            velocity.y *= controller.JumpCutMultiplier;

        if (velocity.y < -controller.MaxFallSpeed)
            velocity.y = -controller.MaxFallSpeed;

        controller.Rb.velocity = velocity;
    }
}