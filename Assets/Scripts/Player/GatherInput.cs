using UnityEngine;
using UnityEngine.InputSystem;

public class GatherInput : MonoBehaviour
{

    public PlayerInput playerInput;

    private InputActionMap playerMap;
    private InputActionMap uiMap;

    public InputActionReference jumpActionRef;
    public InputActionReference moveActionRef;
    [HideInInspector]
    public float horizontalInput;

    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        playerMap.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMap = playerInput.actions.FindActionMap("Player");
        uiMap = playerInput.actions.FindActionMap("UI");
        playerMap.Enable();

        // also good stuff
        //playerInput.actions.Enable();
        //jumpActionRef.action.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        horizontalInput = moveActionRef.action.ReadValue<float>();
        Debug.Log("Horizontal Input: " + horizontalInput);
    }
}
