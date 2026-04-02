using UnityEngine;

public class BodyConnector : MonoBehaviour
{
    [SerializeField] private BodyMarkers currentBody;

    private Player player; 
    private PlayerMovement motor;

    private void Awake()
    {
        player = GetComponent<Player>();

        if (currentBody == null)
            currentBody = GetComponentInChildren<BodyMarkers>(true);

        ApplyBody(currentBody);
    }

    public void ApplyBody(BodyMarkers body)
    {
        if (body == null)
        {
            Debug.LogError("BodyConnector: BodyMarkers not found!");
            return;
        }

        currentBody = body;

        player.visual = body.flipRoot;

        if (body.animator != null)
            player.anim = body.animator;

        motor = GetComponentInChildren<PlayerMovement>(true);
        if (motor != null)
            motor.SetChecks(body.groundCheckPoint, body.frontWallCheckPoint, body.backWallCheckPoint);

        player.RefreshMotorFromChildren();
        player.RefreshStatsFromChildren();
    }
}
