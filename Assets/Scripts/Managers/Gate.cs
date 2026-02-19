using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Gate : MonoBehaviour
{
    [SerializeField] private string levelToLoad;
    public SpawnData spawnDataForOtherLevel;

    private Collider2D gateCollider;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BodyMarkers markers = collision.GetComponentInParent<BodyMarkers>();

        if (markers == null)
        {
                Debug.LogWarning("Gate: No Player component found in parent of collider: " + collision.name);
                return;
        }

        if (markers.CompareTag("Player"))
        {
            spawnDataForOtherLevel.sceneName = levelToLoad;

            SaveLoadManager.instance.Save(spawnDataForOtherLevel, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);

            Player player = collision.GetComponentInParent<Player>();
            player.gatherInput.DisablePlayerMap();
            player.physicsControl.ResetVelocity();
            LevelManager.instance.LoadLevelString(levelToLoad);
            GetComponent<Collider2D>().enabled = false;
        }
    }
}
