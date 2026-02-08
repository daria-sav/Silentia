using UnityEngine;

public class AnimationEventsProxy : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    public void ResetGame()
    {
        if (player == null)
        {
            Debug.LogError("AnimationEventsProxy: Player not found");
            return;
        }

        // player.ResetGame();

        var deathAbility = player.GetComponent<DeathAbility>();
        if (deathAbility != null) deathAbility.ResetGame();
        else LevelManager.instance.RestartLevel(); 
    }
}
