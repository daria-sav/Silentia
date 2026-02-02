using System.Collections.Generic;
using UnityEngine;

public class ReplayClip
{
    public CharacterProfile profile;
    public string profileId;

    // === START SNAPSHOT ===
    public Vector2 startPosition;
    public Vector2 startVelocity;
    public bool startFacingRight;
    public PlayerStates.State startState;

    public readonly List<InputFrame> frames = new List<InputFrame>();
    public int FrameCount => frames.Count;

    public ReplayClip(CharacterProfile profile)
    {
        this.profile = profile;
        this.profileId = profile != null ? profile.id : "";
    }

    public InputFrame GetFrame(int tick)
    {
        if (tick < 0 || tick >= frames.Count)
            return default;
        return frames[tick];
    }
}