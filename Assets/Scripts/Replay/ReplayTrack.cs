using System;
using System.Collections.Generic;
using UnityEngine;

public class ReplayClip
{
    public CharacterProfile profile;
    public string profileId;

    // === SIMULATION SETTINGS (for determinism checks) ===
    public float fixedDeltaTime;
    public int velocityIterations;
    public int positionIterations;

    // === START SNAPSHOT ===
    public Vector2 startPosition;
    public Vector2 startVelocity;
    public bool startFacingRight;
    public PlayerStates.State startState;

    public readonly List<InputFrame> frames = new List<InputFrame>();
    public int FrameCount => frames.Count;

    // === KEYFRAMES (drift correction) ===
    [Serializable]
    public struct ReplayKeyframe
    {
        public int tick;
        public Vector2 pos;
        public Vector2 vel;
    }

    public readonly List<ReplayKeyframe> keyframes = new List<ReplayKeyframe>();

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

    public void AddKeyframe(int tick, Vector2 pos, Vector2 vel)
    {
        keyframes.Add(new ReplayKeyframe { tick = tick, pos = pos, vel = vel });
    }
}