using UnityEngine;

public class StateMirrorLogger : MonoBehaviour
{
    private Player p;
    private PlayerStates.State last;

    private void Awake()
    {
        p = GetComponent<Player>();
        if (p != null) last = p.stateMachine.currentState;
    }

    private void Update()
    {
        if (p == null) return;
        var cur = p.stateMachine.currentState;
        if (cur != last)
        {
            Debug.Log($"STATE MIRROR [{gameObject.name}] {last} -> {cur}");
            last = cur;
        }
    }
}