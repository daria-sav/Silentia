using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Moves a background layer relative to camera movement to create a parallax effect.
///
/// Supports separate parallax strength on X and Y axes, optional pixel-grid snapping
/// for cleaner pixel-art rendering, and optional horizontal looping for repeating
/// background layers.
/// </summary>
public class ParallaxEffect : MonoBehaviour
{
    [Header("Parallax")]
    [SerializeField] private float xParallaxValue;
    [SerializeField] private float yParallaxValue;

    [Header("Pixel snap")]
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private bool snapToPixelGrid = true;

    [Header("Looping")]
    [SerializeField] private bool loopX = true;

    private float spriteLenght;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Vector3 lastCameraPosition;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
        {
            Debug.LogWarning($"{nameof(ParallaxEffect)}: Main camera was not found.");
            enabled = false;
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"{nameof(ParallaxEffect)}: SpriteRenderer was not found.");
            enabled = false;
            return;
        }

        lastCameraPosition = mainCamera.transform.position;
        spriteLenght = spriteRenderer.bounds.size.x;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 cameraDelta = cameraPosition - lastCameraPosition;

        Vector3 newPosition = transform.position + new Vector3(cameraDelta.x * xParallaxValue, cameraDelta.y * yParallaxValue, 0f);

        newPosition.z = transform.position.z;

        if (snapToPixelGrid)
            newPosition = Snap(newPosition, pixelsPerUnit);

        transform.position = newPosition;
        lastCameraPosition = cameraPosition;

        if (!loopX)
            return;

        float distanceToCameraX = cameraPosition.x - transform.position.x;

        if (distanceToCameraX > spriteLenght) 
            transform.position += new Vector3(spriteLenght, 0f, 0f);
        else if (distanceToCameraX < -spriteLenght) 
            transform.position -= new Vector3(spriteLenght, 0f, 0f);

        if (snapToPixelGrid)
            transform.position = Snap(transform.position, pixelsPerUnit);
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    /// <summary>
    /// Snaps world position to the nearest pixel-grid step based on pixels per unit.
    /// </summary>
    private Vector3 Snap(Vector3 position, int ppu)
    {
        if (ppu <= 0)
            return position;

        float unit = 1f / ppu;

        position.x = Mathf.Round(position.x / unit) * unit;
        position.y = Mathf.Round(position.y / unit) * unit;

        return position;
    }
    #endregion
}