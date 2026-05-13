using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores recorded input, start state, and keyframes for replay playback.
/// </summary>
public class ReplayClip
{
    public CharacterProfile profile;
    public string profileId;

    // ───────────── SIMULATION SETTINGS ─────────────
    public float fixedDeltaTime;
    public int velocityIterations;
    public int positionIterations;

    // ───────────── START SNAPSHOT ─────────────
    public Vector2 startPosition;
    public Vector2 startVelocity;
    public bool startFacingRight;
    public PlayerStates.State startState;

    // ───────────── MOTOR SNAPSHOT ─────────────
    [Serializable]
    public struct MotorSnapshot
    {
        // jump / ground / wall flags
        public bool isJumping;
        public bool isWallJumping;
        public bool isSliding;
        public bool isDashing;
        public bool isJumpCut;
        public bool isJumpFalling;

        // counters
        public int dashesLeft;
        public int airJumpsLeft;
        public int lastWallJumpDir;

        // timers
        public float lastOnGroundTime;
        public float lastOnWallTime;
        public float lastOnWallRightTime;
        public float lastOnWallLeftTime;
        public float lastPressedJumpTime;
        public float lastPressedDashTime;
        public float wallJumpTimeLeft;

        // dash refill
        public bool dashRefillActive;
        public float dashRefillTimer;

        // dash phase
        public int dashPhase;
        public float dashPhaseTimer;
        public Vector2 dashDir;

        // facing
        public bool isFacingRight;
    }

    public MotorSnapshot startMotorSnapshot;

    // ───────────── INPUT FRAMES ─────────────

    public readonly List<InputFrame> frames = new List<InputFrame>();
    public int FrameCount => frames.Count;

    // ───────────── JUMP STATE ─────────────
    public bool startIsJumping;
    public float startLastOnGroundTime;
    public int startAirJumpsLeft;

    // ───────────── KEYFRAMES ─────────────
    [Serializable]
    public struct ReplayKeyframe
    {
        public int tick;
        public Vector2 pos;
        public Vector2 vel;
    }

    public readonly List<ReplayKeyframe> keyframes = new List<ReplayKeyframe>();

    // ───────────── CONSTRUCTOR ─────────────

    #region Constructor
    public ReplayClip(CharacterProfile profile)
    {
        this.profile = profile;
        this.profileId = profile != null ? profile.id : "";
    }
    #endregion

    // ───────────── PUBLIC API ─────────────

    #region Public API
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
    #endregion
}