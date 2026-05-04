using UnityEngine;

/// <summary>
/// Place this on an invisible trigger collider at the end of the last level.
/// When the player walks into it, the Credits scene loads.
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string creditsSceneName = "Credits";

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        LoadCredits();
    }

    private void LoadCredits()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(creditsSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(creditsSceneName);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = new Color(1f, 0.84f, 0f, 0.4f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(transform.position, col.bounds.size);
    }
}