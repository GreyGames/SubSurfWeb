using UnityEngine;

// Compatibility placeholder for older scenes that still reference LightweightMainMenu.
// Actual start/pause flow is handled by SubSurfWeb.
public class LightweightMainMenu : MonoBehaviour
{
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private RunnerLaneController runner;
    [SerializeField] private CameraSideShift cameraShift;
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool startPaused = true;
    [SerializeField] private bool disableScriptAfterPlay = true;
    [SerializeField] private string mainTitle = "Main Menu";
    [SerializeField] private string optionsTitle = "Options";
    [SerializeField] private float panelWidth = 320f;
    [SerializeField] private float panelHeight = 210f;
    [SerializeField] private float buttonHeight = 42f;
    [SerializeField] private float edgePadding = 14f;
    [SerializeField] private float slidePixelsPerSecond = 2200f;
    [SerializeField] private KeyCode playKey = KeyCode.Return;
    [SerializeField] private KeyCode backKey = KeyCode.Escape;
}
