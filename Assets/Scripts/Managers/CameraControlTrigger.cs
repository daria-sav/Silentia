using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Controls camera behavior when the player enters or exits a trigger area.
///
/// The trigger can either switch between two Cinemachine cameras or temporarily
/// pan the current camera in a selected direction. Inspector fields are shown
/// dynamically through a custom editor to keep the component clean and easier
/// to configure.
/// </summary>
public class CameraControlTrigger : MonoBehaviour
{
    public CustomInspectorObjects customInspectorObjects;

    private Collider2D col;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Start()
    {
        col = GetComponent<Collider2D>();
    }
    #endregion

    // ─────────────── TRIGGERS ────────────────

    #region Trigger Events
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Player entered camera control trigger!");
            if (customInspectorObjects.panCameraOnContact)
            {
                if (CameraManager.instance == null)
                    return;
                // pans the camera when the player enters the trigger
                CameraManager.instance.PanCameraOnContact(customInspectorObjects.panDistance, customInspectorObjects.panTime, customInspectorObjects.panDirection, false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (CameraManager.instance == null)
            return;

        Vector2 exitDirection = (collision.bounds.center - col.bounds.center).normalized;

        if (customInspectorObjects.swapCameras &&
            customInspectorObjects.cameraOnLeft != null &&
            customInspectorObjects.cameraOnRight != null)
        {
            // switches between cameras based on the direction in which the player exits the trigger
            CameraManager.instance.SwitchCamera(
                customInspectorObjects.cameraOnLeft,
                customInspectorObjects.cameraOnRight,
                exitDirection,
                customInspectorObjects.swapAxis
            );
        }

        if (customInspectorObjects.panCameraOnContact)
        {
            // restores the camera pan when the player leaves the trigger
            CameraManager.instance.PanCameraOnContact(
                customInspectorObjects.panDistance,
                customInspectorObjects.panTime,
                customInspectorObjects.panDirection,
                true
            );
        }
    }
    #endregion
}

/// <summary>
/// Stores configurable camera trigger settings displayed in the Inspector.
///
/// These values define whether the trigger swaps virtual cameras,
/// pans the active camera, or combines both behaviors.
/// </summary>
[System.Serializable]
public class CustomInspectorObjects
{
    [HideInInspector] public CameraSwapAxis swapAxis = CameraSwapAxis.Horizontal;
    public bool swapCameras = false;
    public bool panCameraOnContact = false;

    [HideInInspector] public CinemachineCamera cameraOnLeft;
    [HideInInspector] public CinemachineCamera cameraOnRight;

    [HideInInspector] public PanDirection panDirection;
    [HideInInspector] public float panDistance = 3f;
    [HideInInspector] public float panTime = 0.35f;

}

/// <summary>
/// Defines the direction in which the camera can be panned.
/// </summary>
public enum PanDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Defines the axis used to determine camera switching direction.
/// </summary>
public enum CameraSwapAxis
{
    Horizontal,
    Vertical
}

#if UNITY_EDITOR 

/// <summary>
/// Custom editor for <see cref="CameraControlTrigger"/>.
///
/// Shows only the camera settings that are relevant to the currently enabled
/// trigger behavior, making the Inspector easier to read and configure.
/// </summary>
[CustomEditor(typeof(CameraControlTrigger))]
public class MyScriotEditor : Editor
{
    CameraControlTrigger cameraControlTrigger;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void OnEnable()
    {
        cameraControlTrigger = (CameraControlTrigger)target;
    }
    #endregion

    // ─────────────── INSPECTOR ───────────────

    #region Inspector GUI
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (cameraControlTrigger.customInspectorObjects.swapCameras)
        {
            cameraControlTrigger.customInspectorObjects.swapAxis = (CameraSwapAxis)EditorGUILayout.EnumPopup("Swap Axis", cameraControlTrigger.customInspectorObjects.swapAxis);
            cameraControlTrigger.customInspectorObjects.cameraOnLeft = EditorGUILayout.ObjectField("Camera on Left", cameraControlTrigger.customInspectorObjects.cameraOnLeft,
                typeof(CinemachineCamera), true) as CinemachineCamera;
            cameraControlTrigger.customInspectorObjects.cameraOnRight = EditorGUILayout.ObjectField("Camera on Right", cameraControlTrigger.customInspectorObjects.cameraOnRight,
                typeof(CinemachineCamera), true) as CinemachineCamera;
        }

        if (cameraControlTrigger.customInspectorObjects.panCameraOnContact)
        {
            cameraControlTrigger.customInspectorObjects.panDirection = (PanDirection)EditorGUILayout.EnumPopup("Pan Direction",
                cameraControlTrigger.customInspectorObjects.panDirection);
            cameraControlTrigger.customInspectorObjects.panDistance = EditorGUILayout.FloatField("Pan Distance",
                cameraControlTrigger.customInspectorObjects.panDistance);
            cameraControlTrigger.customInspectorObjects.panTime = EditorGUILayout.FloatField("Pan Time",
                cameraControlTrigger.customInspectorObjects.panTime);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(cameraControlTrigger);
        }
    }
    #endregion
}
#endif