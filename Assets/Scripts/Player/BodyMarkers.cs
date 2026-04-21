using UnityEngine;

public class BodyMarkers : MonoBehaviour
{
    [Header("Motor checks (OverlapBox)")]
    public Transform groundCheckPoint;
    public Transform frontWallCheckPoint;
    public Transform backWallCheckPoint;

    [Header("Visual")]
    public Transform visualRoot; // ??
    public Animator animator;

    public Transform flipRoot;
}