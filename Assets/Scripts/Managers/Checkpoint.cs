using UnityEngine;

/// <summary>
/// Saves checkpoint progress when the player reaches this checkpoint.
///
/// The checkpoint stores the target scene, checkpoint key, and player facing
/// direction through <see cref="SaveLoadManager"/> so the player can respawn
/// from this position later.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private CheckpointData checkpointData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // saves the checkpoint data associated with this checkpoint object
            SaveLoadManager.Instance.SaveCheckpointData(checkpointData.sceneToLoad, checkpointData.checkPointKey, checkpointData.facingRight);
        }
    }
}