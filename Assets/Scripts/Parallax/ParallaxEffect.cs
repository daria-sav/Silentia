using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [SerializeField] private float XparallaxValue;
    [SerializeField] private float YparallaxValue;
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
        deltaMovement = camera.transform.position - lastCameraPosition;
        transform.position += new Vector3(deltaMovement.x * XparallaxValue, deltaMovement.y*YparallaxValue);
        lastCameraPosition = camera.transform.position;

        if (camera.transform.position.x - transform.position.x >= spriteLenght)
        {
            transform.position = new Vector3(camera.transform.position.x + spriteLenght, transform.position.y);
        }
        else if (transform.position.x - camera.transform.position.x >= spriteLenght)
        {
            transform.position = new Vector3(camera.transform.position.x - spriteLenght, transform.position.y);
        }
    }
}
