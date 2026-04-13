using UnityEngine;

public class BodyMarkers : MonoBehaviour
{
    [Header("Motor checks (OverlapBox)")]
    public Transform groundCheckPoint;
    public Transform frontWallCheckPoint;
    public Transform backWallCheckPoint;

    [Header("Checks")] // ??
    public Transform leftGroundPoint;
    public Transform rightGroundPoint;
    public Transform wallCheckUpper;
    public Transform wallCheckLower;

    [Header("Visual")]
    public Transform visualRoot; // ??
    public Animator animator;

    public Transform flipRoot;
}