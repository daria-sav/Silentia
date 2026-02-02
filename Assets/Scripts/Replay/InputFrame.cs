using System;

[Serializable]
public struct InputFrame
{
    public int tick;

    public float moveX;

    // Jump
    public bool jumpDown;
    public bool jumpUp;
    public bool jumpHeld;

    // Dash
    public bool dashDown;
    public bool dashUp;
    public bool dashHeld;
}