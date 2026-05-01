public class WaterState : PlayerMovementState
{
    public WaterState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        // Water movement will be added later.
    }

    public override void FixedTick()
    {
        // Temporary fallback until water movement is implemented.
        controller.ChangeState(controller.Sensors.IsGrounded ? controller.GroundState : controller.AirState);
    }
}
