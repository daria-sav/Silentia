using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Transform followTarget;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (player.motor != null)
        {
            transform.position = player.motor.transform.position;
        }
    }
}