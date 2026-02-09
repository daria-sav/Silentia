using UnityEngine;

public class Gate : MonoBehaviour
{
    [SerializeField] private string levelToLoad;
    public SpawnData spawnDataForOtherLevel;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            spawnDataForOtherLevel.sceneName = levelToLoad;

            SaveLoadManager.instance.Save(spawnDataForOtherLevel, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);

            Player player = collision.GetComponent<Player>();
            player.gatherInput.DisablePlayerMap();
            player.physicsControl.ResetVelocity();
            LevelManager.instance.LoadLevelString(levelToLoad);
            GetComponent<Collider2D>().enabled = false;
        }
    }
}
