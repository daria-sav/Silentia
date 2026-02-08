using UnityEngine;

public class SpawnControl : MonoBehaviour
{
    private Transform player;
    [SerializeField] private SpawnIdentifier[] spawnPoints;
    private SpawnData spawnData = new SpawnData();

    void Start()
    {
        // SaveLoadManager.instance.DeleteFolder(SaveLoadManager.instance.folderName);
        player = FindAnyObjectByType<Player>().transform;
        SaveLoadManager.instance.Load(spawnData, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);
        foreach (SpawnIdentifier spawnPoint in spawnPoints)
        {
            if (spawnPoint.spawnKey == spawnData.spawnPintKey)
            {
                player.transform.position = spawnPoint.transform.position;
                break;
            }
        }

        if (spawnData.facingRight == false)
        {
            player.GetComponent<Player>().ForceFlip();
        }
    }

    
}
