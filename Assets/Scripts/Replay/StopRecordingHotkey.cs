using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ReplayRecorder))]
public class StopRecordingHotkey : MonoBehaviour
{
    private ReplayRecorder recorder;

    private void Awake()
    {
        recorder = GetComponent<ReplayRecorder>();
    }

    private void Update()
    {
        if (recorder == null) return;
        if (!recorder.IsRecording) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.xKey.wasPressedThisFrame)
        {
            recorder.StopRecording();
        }
    }
}