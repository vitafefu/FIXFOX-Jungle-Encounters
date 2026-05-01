public abstract class PlayerMovementState
{
    protected readonly PlayerController controller;

    protected PlayerMovementState(PlayerController controller)
    {
        this.controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick() { }
    public virtual void FixedTick() { }
}
