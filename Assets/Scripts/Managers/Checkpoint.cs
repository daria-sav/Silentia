using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private CheckpointData checkpointData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // save data
            SaveLoadManager.Instance.SaveCheckpointData(checkpointData.sceneToLoad, checkpointData.checkPointKey, checkpointData.facingRight);
        }
    }
}
