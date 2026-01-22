using UnityEngine;

public class BodyMarkers : MonoBehaviour
{
    [Header("Checks")]
    public Transform leftGroundPoint;
    public Transform rightGroundPoint;
    public Transform wallCheckUpper;
    public Transform wallCheckLower;

    [Header("Visual")]
    public Transform visualRoot;     
    public Animator animator;

    public Transform flipRoot;
}
