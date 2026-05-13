using System;
/// <summary>
/// Stores player input captured for a single fixed replay tick.
/// </summary>
[Serializable]
public struct InputFrame
{
    public int tick;

    public float moveX;
    public float moveY;

    // ───────────── JUMP ─────────────
    public bool jumpDown;
    public bool jumpUp;
    public bool jumpHeld;

    // ───────────── DASH ─────────────
    public bool dashDown;
    public bool dashUp;
    public bool dashHeld;
}