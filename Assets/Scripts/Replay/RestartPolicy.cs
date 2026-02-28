using UnityEngine;

public static class RestartPolicy
{
    private static int locks = 0;

    public static bool AllowLevelRestart => locks <= 0;

    public static void Lock()
    {
        locks++;
        // Debug.Log($"[RestartPolicy] Lock -> {locks}");
    }

    public static void Unlock()
    {
        locks = Mathf.Max(0, locks - 1);
        // Debug.Log($"[RestartPolicy] Unlock -> {locks}");
    }

    public static void ResetAll()
    {
        locks = 0;
        // Debug.Log("[RestartPolicy] ResetAll");
    }
}