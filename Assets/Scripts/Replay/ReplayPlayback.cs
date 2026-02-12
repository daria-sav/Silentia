using UnityEngine;

[DefaultExecutionOrder(-400)]
public class ReplayPlayback : MonoBehaviour
{
    public GatherInput input;

    private ReplayClip clip;
    private int tick;
    public bool IsPlaying { get; private set; }

    private Player player;
    private PhysicsControl physics;
    private MultipleJumpAbility jumpAbility;

    [Header("Drift correction")]
    [SerializeField] private float positionEpsilon = 0.15f;      // ignore tiny drift
    [SerializeField] private float hardSnapThreshold = 0.75f;    // snap if huge drift
    [SerializeField] private float pullStrength = 0.5f;          // 0..1 soft pull

    private int kfIndex;

    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();

        player = GetComponent<Player>();
        physics = GetComponent<PhysicsControl>();
        jumpAbility = GetComponent<MultipleJumpAbility>();
    }

    public void StartPlayback(ReplayClip replayClip)
    {
        clip = replayClip;
        tick = 0;
        IsPlaying = true;
        RestartPolicy.AllowLevelRestart = false;

        if (input != null)
            input.SetMode(GatherInput.InputMode.Replay);

        //if (clip != null && clip.FrameCount > 0 && input != null)
        //{
        //    input.ApplyReplayFrame(clip.GetFrame(0));
        //    tick = 1;
        //}

        ApplyStartSnapshot(clip);

        kfIndex = 0;

        Debug.Log($"REPLAY: Playback started. Profile={clip?.profileId}, frames={clip?.FrameCount}");
    }

    private void FixedUpdate()
    {
        if (!IsPlaying || clip == null || input == null) return;

        if (tick >= clip.FrameCount)
        {
            IsPlaying = false;

            if (input != null)
            {
                input.ApplyReplayFrame(default); // clears held/down/up + moveX
            }

            if (physics != null && physics.rb != null)
            {
                // Stop horizontal drifting, keep vertical as-is (usually 0 on ground)
                physics.rb.linearVelocity = new Vector2(0f, physics.rb.linearVelocityY);
            }

            if (player != null && player.stateMachine != null)
            {
                player.stateMachine.ForceChange(PlayerStates.State.Idle);
            }
            Debug.Log("REPLAY: Playback finished.");
            return;
        }

        var frame = clip.GetFrame(tick);
        input.ApplyReplayFrame(frame);

        ApplyDriftCorrection(tick);

        tick++;

        if (frame.jumpDown || frame.dashDown)
            Debug.Log($"GHOST APPLY T{tick}: jumpDown={frame.jumpDown} dashDown={frame.dashDown}");
    }

    private void ApplyStartSnapshot(ReplayClip c)
    {
        if (c == null) return;

        // position
        transform.position = c.startPosition;

        // velocity
        if (physics != null && physics.rb != null)
            physics.rb.linearVelocity = c.startVelocity;

        // facing + visual flip
        if (player != null)
        {
            player.facingRight = c.startFacingRight;

            if (player.visual != null)
            {
                var s = player.visual.localScale;
                s.x = c.startFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                player.visual.localScale = s;
            }

            if (player.stateMachine != null)
                player.stateMachine.ForceChange(c.startState);
        }

        // reset ability internal state (important for determinism)
        if (jumpAbility != null)
            jumpAbility.ResetJumpState();

        // normalize physics state
        if (physics != null)
        {
            // start with a stable coyote timer state; prevents random extra coyote jump
            physics.coyoteTimer = -1f;

            // ensure gravity is enabled to base value
            physics.EnableGravity();
        }
    }

    private void ApplyDriftCorrection(int currentTick)
    {
        if (clip == null || clip.keyframes == null || clip.keyframes.Count == 0) return;
        if (physics == null || physics.rb == null) return;

        // move pointer to latest keyframe tick <= currentTick
        while (kfIndex + 1 < clip.keyframes.Count && clip.keyframes[kfIndex + 1].tick <= currentTick)
            kfIndex++;

        var kf = clip.keyframes[kfIndex];

        // only correct exactly on keyframe ticks (keeps motion smooth)
        if (kf.tick != currentTick) return;

        Vector2 curPos = transform.position;
        Vector2 targetPos = kf.pos;

        float err = Vector2.Distance(curPos, targetPos);
        if (err < positionEpsilon) return;

        if (err >= hardSnapThreshold)
        {
            // hard snap if drift is large (collisions may have diverged)
            transform.position = targetPos;
            physics.rb.linearVelocity = kf.vel;
        }
        else
        {
            // soft pull to avoid visible teleport
            Vector2 newPos = Vector2.Lerp(curPos, targetPos, pullStrength);
            transform.position = newPos;

            physics.rb.linearVelocity = Vector2.Lerp(physics.rb.linearVelocity, kf.vel, pullStrength);
        }
    }
}
