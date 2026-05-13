using UnityEngine;

/// <summary>
/// Stores references to body-specific marker points and visual components.
/// </summary>
public class BodyMarkers : MonoBehaviour
{
    [Header("Motor checks (OverlapBox)")]
    public Transform groundCheckPoint;
    public Transform frontWallCheckPoint;
    public Transform backWallCheckPoint;

    [Header("Visual")]
    public Transform visualRoot; 
    public Animator animator;

    public Transform flipRoot;
}