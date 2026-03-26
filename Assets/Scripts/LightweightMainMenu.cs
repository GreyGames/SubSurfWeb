using UnityEngine;

public class LightweightMainMenu : MonoBehaviour
{
    private enum MenuMode
    {
        Main,
        TransitionToOptions,
        Options,
        TransitionToMain,
        TransitionToPlay,
        Hidden
    }

    [Header("References")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private RunnerLaneController runner;
    [SerializeField] private CameraSideShift cameraShift;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Startup")]
    [SerializeField] private bool startPaused = true;
    [SerializeField] private bool disableScriptAfterPlay = true;

    [Header("Layout")]
    [SerializeField] private string mainTitle = "Main Menu";
    [SerializeField] private string optionsTitle = "Options";
    [SerializeField] private float panelWidth = 320f;
    [SerializeField] private float panelHeight = 210f;
    [SerializeField] private float buttonHeight = 42f;
    [SerializeField] private float edgePadding = 14f;
    [SerializeField] private bool scaleMenuOnMobile = true;
    [SerializeField] private float mobileScale = 3f;
    [SerializeField] private float desktopScale = 1f;

    [Header("Animation")]
    [SerializeField] private float slidePixelsPerSecond = 2200f;

    [Header("Input Shortcuts")]
    [SerializeField] private KeyCode playKey = KeyCode.Return;
    [SerializeField] private KeyCode backKey = KeyCode.Escape;

    private MenuMode mode;
    private float mainPanelY;
    private float optionsPanelY;
    private float mainTargetY;
    private float optionsTargetY;
    private Rect safeRect;

    private void Awake()
    {
        CacheRefs();
        RebuildLayout(out float centeredY, out _, out float offBottomY);
        mainPanelY = centeredY;
        optionsPanelY = offBottomY;
        mode = startPaused ? MenuMode.Main : MenuMode.Hidden;
        SetGameplayActive(!startPaused);
        SetIntroCameraActive(startPaused);
    }

    private void Update()
    {
        HandleShortcuts();
        RebuildLayout(out float centeredY, out float offTopY, out float offBottomY);
        ResolveTargets(centeredY, offTopY, offBottomY);

        float scale = GetGuiScale();
        float step = slidePixelsPerSecond * Time.unscaledDeltaTime / Mathf.Max(0.1f, scale);
        mainPanelY = Mathf.MoveTowards(mainPanelY, mainTargetY, step);
        optionsPanelY = Mathf.MoveTowards(optionsPanelY, optionsTargetY, step);

        UpdateModeCompletion(centeredY, offTopY, offBottomY);
    }

    private void OnGUI()
    {
        if (mode == MenuMode.Hidden)
        {
            return;
        }

        float scale = GetGuiScale();
        Matrix4x4 previousMatrix = GUI.matrix;
        if (Mathf.Abs(scale - 1f) > 0.001f)
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        }

        Rect mainRect = GetPanelRect(mainPanelY);
        Rect optionsRect = GetPanelRect(optionsPanelY);

        int previousDepth = GUI.depth;
        GUI.depth = -900;

        if (mode == MenuMode.Main || mode == MenuMode.TransitionToOptions || mode == MenuMode.TransitionToPlay || mode == MenuMode.TransitionToMain)
        {
            DrawMainPanel(mainRect);
        }

        if (mode == MenuMode.Options || mode == MenuMode.TransitionToOptions || mode == MenuMode.TransitionToMain)
        {
            DrawOptionsPanel(optionsRect);
        }

        GUI.depth = previousDepth;
        GUI.matrix = previousMatrix;
    }

    private void DrawMainPanel(Rect panel)
    {
        GUI.Box(panel, mainTitle);

        float buttonWidth = panel.width - edgePadding * 2f;
        float firstY = panel.y + 58f;
        Rect playRect = new Rect(panel.x + edgePadding, firstY, buttonWidth, buttonHeight);
        Rect optionsRect = new Rect(panel.x + edgePadding, firstY + buttonHeight + 10f, buttonWidth, buttonHeight);

        bool canClick = mode == MenuMode.Main;
        if (!canClick)
        {
            GUI.enabled = false;
        }

        if (GUI.Button(playRect, "Play"))
        {
            StartPlayTransition();
        }

        if (GUI.Button(optionsRect, "Options"))
        {
            StartOptionsTransition();
        }

        GUI.enabled = true;
    }

    private void DrawOptionsPanel(Rect panel)
    {
        GUI.Box(panel, optionsTitle);

        Rect infoRect = new Rect(panel.x + edgePadding, panel.y + 56f, panel.width - edgePadding * 2f, 60f);
        GUI.Label(infoRect, "Add your lightweight toggles here.");

        Rect backRect = new Rect(
            panel.x + edgePadding,
            panel.y + panel.height - edgePadding - buttonHeight,
            panel.width - edgePadding * 2f,
            buttonHeight);

        bool canClick = mode == MenuMode.Options;
        if (!canClick)
        {
            GUI.enabled = false;
        }

        if (GUI.Button(backRect, "Back"))
        {
            StartBackTransition();
        }

        GUI.enabled = true;
    }

    private void StartPlayTransition()
    {
        if (mode != MenuMode.Main)
        {
            return;
        }

        SetGameplayActive(true);
        SetIntroCameraActive(false);
        mode = MenuMode.TransitionToPlay;
    }

    private void StartOptionsTransition()
    {
        if (mode != MenuMode.Main)
        {
            return;
        }

        mode = MenuMode.TransitionToOptions;
    }

    private void StartBackTransition()
    {
        if (mode != MenuMode.Options)
        {
            return;
        }

        mode = MenuMode.TransitionToMain;
    }

    private void ResolveTargets(float centeredY, float offTopY, float offBottomY)
    {
        switch (mode)
        {
            case MenuMode.Main:
                mainTargetY = centeredY;
                optionsTargetY = offBottomY;
                break;
            case MenuMode.TransitionToOptions:
                mainTargetY = offBottomY;
                optionsTargetY = centeredY;
                break;
            case MenuMode.Options:
                mainTargetY = offBottomY;
                optionsTargetY = centeredY;
                break;
            case MenuMode.TransitionToMain:
                mainTargetY = centeredY;
                optionsTargetY = offBottomY;
                break;
            case MenuMode.TransitionToPlay:
                mainTargetY = offTopY;
                optionsTargetY = offBottomY;
                break;
            case MenuMode.Hidden:
                mainTargetY = offTopY;
                optionsTargetY = offBottomY;
                break;
        }
    }

    private void UpdateModeCompletion(float centeredY, float offTopY, float offBottomY)
    {
        const float epsilon = 0.5f;
        switch (mode)
        {
            case MenuMode.TransitionToOptions:
                if (Mathf.Abs(mainPanelY - offBottomY) <= epsilon && Mathf.Abs(optionsPanelY - centeredY) <= epsilon)
                {
                    mode = MenuMode.Options;
                }
                break;
            case MenuMode.TransitionToMain:
                if (Mathf.Abs(mainPanelY - centeredY) <= epsilon && Mathf.Abs(optionsPanelY - offBottomY) <= epsilon)
                {
                    mode = MenuMode.Main;
                }
                break;
            case MenuMode.TransitionToPlay:
                if (Mathf.Abs(mainPanelY - offTopY) <= epsilon)
                {
                    mode = MenuMode.Hidden;
                    if (disableScriptAfterPlay)
                    {
                        enabled = false;
                    }
                }
                break;
        }
    }

    private void HandleShortcuts()
    {
        if (mode == MenuMode.Main && Input.GetKeyDown(playKey))
        {
            StartPlayTransition();
        }
        else if (mode == MenuMode.Options && Input.GetKeyDown(backKey))
        {
            StartBackTransition();
        }
    }

    private Rect GetPanelRect(float y)
    {
        float width = Mathf.Min(panelWidth, safeRect.width - 12f);
        float x = safeRect.x + (safeRect.width - width) * 0.5f;
        return new Rect(x, y, width, panelHeight);
    }

    private void RebuildLayout(out float centeredY, out float offTopY, out float offBottomY)
    {
        float scale = GetGuiScale();
        safeRect = Screen.safeArea;
        if (safeRect.width <= 1f || safeRect.height <= 1f)
        {
            safeRect = new Rect(0f, 0f, Screen.width, Screen.height);
        }
        if (Mathf.Abs(scale - 1f) > 0.001f)
        {
            safeRect = new Rect(
                safeRect.x / scale,
                safeRect.y / scale,
                safeRect.width / scale,
                safeRect.height / scale);
        }

        centeredY = safeRect.y + (safeRect.height - panelHeight) * 0.5f;
        offTopY = safeRect.y - panelHeight - 16f;
        offBottomY = safeRect.yMax + 16f;
    }

    private float GetGuiScale()
    {
        float scale = desktopScale;
        if (scaleMenuOnMobile && Application.isMobilePlatform)
        {
            scale = mobileScale;
        }

        return Mathf.Max(0.1f, scale);
    }

    private void CacheRefs()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (looper == null)
        {
            looper = FindObjectOfType<EndlessTrackLooper>();
        }

        if (runner == null)
        {
            runner = FindObjectOfType<RunnerLaneController>();
        }
    }

    private void SetGameplayActive(bool isActive)
    {
        if (looper == null || runner == null)
        {
            CacheRefs();
        }

        if (looper != null)
        {
            looper.PauseWorld(!isActive);
        }

        if (runner != null)
        {
            runner.LockInput(!isActive);
        }
    }

    public void ShowMenu()
    {
        enabled = true;
        mode = MenuMode.Main;
        SetGameplayActive(false);
        SetIntroCameraActive(true);
        RebuildLayout(out float centeredY, out _, out float offBottomY);
        mainPanelY = centeredY;
        optionsPanelY = offBottomY;
    }

    private void SetIntroCameraActive(bool isActive)
    {
        if (cameraShift == null)
        {
            Camera main = Camera.main;
            if (main != null)
            {
                cameraShift = main.GetComponent<CameraSideShift>();
                if (cameraShift == null)
                {
                    cameraShift = main.gameObject.AddComponent<CameraSideShift>();
                }
            }
        }

        if (cameraShift != null)
        {
            cameraShift.SetIntroActive(isActive);
        }
    }
}
