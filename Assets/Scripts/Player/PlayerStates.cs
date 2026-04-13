// defines all high-level player states used by the state machine and abilities
public class PlayerStates 
{
    public enum State
    {
        Idle, // default one
        Walk,
        Run, // ??
        Jump,
        DoubleJump, // ??
        WallJump, // ??
        WallSlide,
        Dash,
        Crouch, // ??
        Ladders, // ??
        Ignore, // ??
        KnockBack,
        Death,
    }
}