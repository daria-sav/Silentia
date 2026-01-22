using UnityEngine;

public class BodyConnector : MonoBehaviour
{
    [SerializeField] private BodyMarkers currentBody;

    private Player player;
    private PhysicsControl physicsControl;

    private void Awake()
    {
        player = GetComponent<Player>();
        physicsControl = GetComponent<PhysicsControl>();

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

        physicsControl.SetCheckPoints(
            body.leftGroundPoint,
            body.rightGroundPoint,
            body.wallCheckUpper,
            body.wallCheckLower
        );

        player.visual = body.flipRoot;

        if (body.animator != null)
            player.anim = body.animator;
    }
}
