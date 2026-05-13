using UnityEngine;

/// <summary>
/// Logs captured input frames during fixed ticks for debugging.
/// </summary>

public class InputDebugTick : MonoBehaviour
{
    public GatherInput input;

    private int tick;

    // ───────────── LIFECYCLE ─────────────

    #region Lifecycle
    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();
    }

    private void FixedUpdate()
    {
        if (input == null) return;

        var f = input.CaptureFrame(tick);
        if (f.jumpDown || f.jumpUp || f.dashDown || Mathf.Abs(f.moveX) > 0.01f)
        {
            Debug.Log($"T{tick} moveX={f.moveX:0.00} " +
                      $"J(D:{f.jumpDown} H:{f.jumpHeld} U:{f.jumpUp}) " +
                      $"D(D:{f.dashDown} H:{f.dashHeld} U:{f.dashUp})");
        }

        tick++;
    }
    #endregion
}