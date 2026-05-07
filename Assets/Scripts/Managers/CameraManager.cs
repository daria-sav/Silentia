using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

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

        // set the starting position of the tracked object offset
        startingTrackedObjectOffset = positionComposer.TargetOffset;

        StartCoroutine(RebuildAllConfinerCachesDelayed());
    }

    private IEnumerator RebuildAllConfinerCachesDelayed()
    {
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

        // lerp the pan amount
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

    #region Pan the camera

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

        // handle pan from trigger
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

        // handle the direction settings when moving back to the starting position
        else
        {
            startingPos = positionComposer.TargetOffset;
            endPos = startingTrackedObjectOffset;
        }

        Vector3 savedDamping = positionComposer.Damping;
        positionComposer.Damping = new Vector3(savedDamping.x, 0f, savedDamping.z);

        // handle the actual panning of the camera
        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panTime);

            // smoothstep gives ease-in + ease-out
            t = Mathf.SmoothStep(0f, 1f, t);

            positionComposer.TargetOffset = Vector3.Lerp(startingPos, endPos, t);
            yield return null;
        }

        positionComposer.TargetOffset = endPos;

        positionComposer.Damping = savedDamping;
        panCameraCoroutine = null;
    }
    #endregion

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

        // if the current camera is the camera on the left and trigger exit direction was on the right
        if (currentCamera == cameraOnNegativeSide && directionValue > 0f)
        {
            Debug.Log("[Switch] Negative -> Positive");
            cameraOnPositiveSide.enabled = true;

            cameraOnNegativeSide.enabled = false;

            currentCamera = cameraOnPositiveSide;

            positionComposer = currentCamera.GetComponent<CinemachinePositionComposer>();
            UpdateCameraState();
        }

        // if the current camera is the camera on the right and trigger exit direction was on the left
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
        positionComposer.Damping = Vector3.zero;
        currentCamera.InternalUpdateCameraState(Vector3.up, Time.deltaTime);
        positionComposer.Damping = saved;
    }

    #endregion
}