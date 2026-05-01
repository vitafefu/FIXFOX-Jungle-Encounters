using UnityEngine;

public class LadderState : PlayerMovementState
{
    private const float TopEntryGraceTime = 0.35f;

    private bool enteringFromTop;
    private float topEntryTimer;

    public LadderState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        controller.SetClimbing(true);
        controller.SetCrouching(false);
        controller.ApplyStandingCollider();
        controller.ResetRun();

        controller.SetGroundCollisionIgnored(true);
        controller.Rb.gravityScale = 0f;

        controller.Sensors.Refresh();

        enteringFromTop =
            controller.MoveInputY < -0.01f &&
            controller.Sensors.IsLadderBelowFeet &&
            !controller.Sensors.IsLadderAtBody;

        topEntryTimer = enteringFromTop ? TopEntryGraceTime : 0f;

        Vector2 velocity = controller.Rb.velocity;
        velocity.x = 0f;
        velocity.y = enteringFromTop ? -controller.ClimbSpeed : 0f;

        controller.Rb.velocity = velocity;
    }

    public override void Exit()
    {
        controller.SetGroundCollisionIgnored(false);
        controller.SetClimbing(false);
        controller.Rb.gravityScale = controller.DefaultGravityScale;

        enteringFromTop = false;
        topEntryTimer = 0f;
    }

    public override void FixedTick()
    {
        controller.Sensors.Refresh();

        if (controller.JumpPressed && controller.JumpExitsLadder)
        {
            Vector2 jumpVelocity = controller.Rb.velocity;
            jumpVelocity.x = controller.MoveInputX * controller.LadderHorizontalSpeed;
            jumpVelocity.y = controller.JumpForce;

            controller.Rb.velocity = jumpVelocity;
            controller.ConsumeJump();
            controller.ChangeState(controller.AirState);
            return;
        }

        if (enteringFromTop)
        {
            topEntryTimer -= Time.fixedDeltaTime;

            Vector2 entryVelocity = controller.Rb.velocity;
            entryVelocity.x = controller.MoveInputX * controller.LadderHorizontalSpeed;
            entryVelocity.y = -controller.ClimbSpeed;

            controller.Rb.velocity = entryVelocity;

            if (controller.Sensors.IsLadderAtBody)
            {
                enteringFromTop = false;
                topEntryTimer = 0f;
                return;
            }

            if (topEntryTimer <= 0f)
            {
                controller.ChangeState(controller.Sensors.IsGrounded ? controller.GroundState : controller.AirState);
                return;
            }

            return;
        }

        bool hasLadderConnection =
            controller.Sensors.IsLadderAtBody ||
            controller.Sensors.IsLadderBelowFeet;

        if (!hasLadderConnection)
        {
            controller.ChangeState(controller.Sensors.IsGrounded ? controller.GroundState : controller.AirState);
            return;
        }

        float verticalInput = controller.MoveInputY;

        if (verticalInput > 0.01f && controller.Sensors.IsAtLadderTop)
            verticalInput = 0f;

        Vector2 velocity = controller.Rb.velocity;
        velocity.x = controller.MoveInputX * controller.LadderHorizontalSpeed;
        velocity.y = verticalInput * controller.ClimbSpeed;

        controller.Rb.velocity = velocity;
    }
}