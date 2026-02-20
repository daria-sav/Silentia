using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Parallax")]
    [SerializeField] private float XparallaxValue;
    [SerializeField] private float YparallaxValue;

    [Header("Pixel snap")]
    [SerializeField] private int pixelsPerUnit = 16;   
    [SerializeField] private bool snapToPixelGrid = true;

    [Header("Looping")]
    [SerializeField] private bool loopX = true;

    private float spriteLenght;
    private Camera camera;
    private Vector3 deltaMovement;
    private Vector3 lastCameraPosition;
    void Start()
    {
        camera = Camera.main;
        lastCameraPosition = camera.transform.position;

        spriteLenght = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void LateUpdate()
    {
        Vector3 camPos = camera.transform.position;
        Vector3 delta = camPos - lastCameraPosition;

        Vector3 newPos = transform.position + new Vector3(delta.x * XparallaxValue, delta.y * YparallaxValue, 0f);

        newPos.z = transform.position.z;

        if (snapToPixelGrid)
            newPos = Snap(newPos, pixelsPerUnit);

        transform.position = newPos;
        lastCameraPosition = camPos;

        if (!loopX) return;

        float diffX = camPos.x - transform.position.x;
        if (diffX > spriteLenght) transform.position += new Vector3(spriteLenght, 0f, 0f);
        else if (diffX < -spriteLenght) transform.position -= new Vector3(spriteLenght, 0f, 0f);
    }

    private Vector3 Snap(Vector3 pos, int ppu)
    {
        float unit = 1f / ppu;
        pos.x = Mathf.Round(pos.x / unit) * unit;
        pos.y = Mathf.Round(pos.y / unit) * unit;
        return pos;
    }
}
