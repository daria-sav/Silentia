using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Manages all active Cinemachine camera behavior in the scene.
///
/// This class controls camera switching, trigger-based camera panning,
/// vertical damping adjustments during player falling, delayed confiner cache
/// rebuilding, and instant snapping to the current target when needed.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineCamera[] allCameras;

    [Header("Controls for lerping the Y Damping during player jump or fall")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreshold = -15f;

    public bool isLerpingYDamping { get; private set; }
    public bool lerpedFromPlayerFalling { get; set; }

    private Coroutine lerpYPanCoroutine;
    private Coroutine panCameraCoroutine;

    private CinemachineCamera currentCamera;
    private CinemachinePositionComposer positionComposer;

    private float normYPanAmount;

    private Vector2 startingTrackedObjectOffset;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        for (int i = 0; i < allCameras.Length; i++)
        {
            if (allCameras[i] != null)
            {
                currentCamera = allCameras[i];
                positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();

                if (positionComposer != null)
                {
                    normYPanAmount = positionComposer.Damping.y;
                    break;
                }
            }
        }

        if (positionComposer == null)
            Debug.LogError("[CameraManager] CinemachinePositionComposer not found!");

        // stores the default camera offset so temporary pans can return to the original position
        startingTrackedObjectOffset = positionComposer.TargetOffset;

        StartCoroutine(RebuildAllConfinerCachesDelayed());
    }
    #endregion

    // ─────────────── CONFINER CACHE ───────────────

    #region Confiner Cache
    private IEnumerator RebuildAllConfinerCachesDelayed()
    {
        // waits until physics and collider data are initialized before baking confiner bounds
        yield return new WaitForFixedUpdate();
        yield return null; 

        if (allCameras == null) yield break;

        foreach (var cam in allCameras)
        {
            if (cam == null) continue;

            var confiner = cam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null) continue;

            confiner.InvalidateBoundingShapeCache();
            confiner.BakeBoundingShape(cam, 1f);
        }
    }
    #endregion

    // ─────────────── Y DAMPING ───────────────

    #region Lerp the Y Damping
    public void LerpYDamping(bool isPlayerFalling)
    {
        if (lerpYPanCoroutine != null)
            StopCoroutine(lerpYPanCoroutine);

        lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        isLerpingYDamping = true;

        float startDamping = positionComposer.Damping.y;
        float endDamping = isPlayerFalling ? fallPanAmount : normYPanAmount;

        if (isPlayerFalling)
            lerpedFromPlayerFalling = true;

        // smoothly adjusts vertical damping depending on whether the player is falling
        float elapsedTime = 0f;
        while (elapsedTime < fallPanTime)
        {
            elapsedTime += Time.deltaTime;

            var damping = positionComposer.Damping;
            damping.y = Mathf.Lerp(startDamping, endDamping, elapsedTime / fallPanTime);
            positionComposer.Damping = damping;


            yield return null;
        }
        var d = positionComposer.Damping;
        d.y = endDamping;
        positionComposer.Damping = d;

        isLerpingYDamping = false;
    }
    #endregion

    // ─────────────── CAMERA PANNING ───────────────

    #region Pan the Camera
    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        if (panCameraCoroutine != null)
            StopCoroutine(panCameraCoroutine);

        panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector2 endPos = Vector2.zero;
        Vector2 startingPos = Vector2.zero;

        // calculates the target offset when the camera is panned away from its default position
        if (!panToStartingPos)
        {
            switch (panDirection)
            {
                case PanDirection.Up:
                    endPos = Vector2.up;
                    break;
                case PanDirection.Down:
                    endPos = Vector2.down;
                    break;
                case PanDirection.Left:
                    endPos = Vector2.left;
                    break;
                case PanDirection.Right:
                    endPos = Vector2.right;
                    break;
                default:
                    break;

            }
            endPos *= panDistance;

            startingPos = startingTrackedObjectOffset;

            endPos += startingPos;
        }

        // returns the camera back to its original tracked object offset
        else
        {
            startingPos = positionComposer.TargetOffset;
            endPos = startingTrackedObjectOffset;
        }

        Vector3 savedDamping = positionComposer.Damping;
        positionComposer.Damping = new Vector3(savedDamping.x, 0f, savedDamping.z);

        // smoothly moves the camera offset between the start and target positions
        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panTime);

            // smoothStep gives the movement an ease-in and ease-out effect
            t = Mathf.SmoothStep(0f, 1f, t);

            positionComposer.TargetOffset = Vector3.Lerp(startingPos, endPos, t);
            yield return null;
        }

        positionComposer.TargetOffset = endPos;

        positionComposer.Damping = savedDamping;
        panCameraCoroutine = null;
    }
    #endregion

    // ─────────────── CAMERA SWITCHING ───────────────

    #region Camera Switching
    public void SwitchCamera(CinemachineCamera cameraOnNegativeSide, CinemachineCamera cameraOnPositiveSide, Vector2 triggerExitDirection, CameraSwapAxis swapAxis)
    {
        float directionValue = swapAxis == CameraSwapAxis.Horizontal
            ? triggerExitDirection.x
            : triggerExitDirection.y;

        if (Mathf.Abs(directionValue) < 0.1f)
        {
            Debug.LogWarning("[Switch] Exit direction is too small for selected axis.");
            return;
        }

        // switches from the negative-side camera to the positive-side camera
        if (currentCamera == cameraOnNegativeSide && directionValue > 0f)
        {
            Debug.Log("[Switch] Negative -> Positive");
            cameraOnPositiveSide.enabled = true;

            cameraOnNegativeSide.enabled = false;

            currentCamera = cameraOnPositiveSide;

            positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
            UpdateCameraState();
        }

        // switches from the positive-side camera to the negative-side camera
        else if (currentCamera == cameraOnPositiveSide && directionValue < 0f)
        {
            Debug.Log("[Switch] Positive -> Negative");
            cameraOnNegativeSide.enabled = true;

            cameraOnPositiveSide.enabled = false;

            currentCamera = cameraOnNegativeSide;

            positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
            UpdateCameraState();
        }
        else
        {
            Debug.LogWarning("[Switch] NO BRANCH — current/direction mismatch");
        }
    }
    #endregion

    // ─────────────── HELPERS ───────────────

    #region Helpers
    private void UpdateCameraState()
    {
        positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
        if (positionComposer != null)
        {
            normYPanAmount = positionComposer.Damping.y;
            startingTrackedObjectOffset = positionComposer.TargetOffset;
        }
    }

    public void SnapToTarget()
    {
        if (currentCamera == null || positionComposer == null) return;

        Vector3 saved = positionComposer.Damping;

        // removes damping for one update so the camera immediately snaps to its target
        positionComposer.Damping = Vector3.zero;
        currentCamera.InternalUpdateCameraState(Vector3.up, Time.deltaTime);
        positionComposer.Damping = saved;
    }

    #endregion
}