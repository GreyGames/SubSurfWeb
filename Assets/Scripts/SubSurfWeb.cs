using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSurfWeb : MonoBehaviour
{
    private static SubSurfWeb instance;

    [Header("Fallback Start Sequence")]
    [SerializeField] private bool enableLocalStartSequence = true;
    [SerializeField] private float maxDelayBetweenSequenceKeys = 1.2f;

    private EndlessTrackLooper looper;
    private RunnerLaneController runner;
    private CameraSideShift cameraShift;
    private bool hasStarted;
    private readonly KeyCode[] localStartSequence = { KeyCode.P, KeyCode.T, KeyCode.G, KeyCode.N };
    private int localStartSequenceIndex;
    private float localStartSequenceDeadline;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject receiver = GameObject.Find("SubSurfWeb");
        if (receiver == null)
        {
            receiver = new GameObject("SubSurfWeb");
        }

        if (receiver.GetComponent<SubSurfWeb>() == null)
        {
            receiver.AddComponent<SubSurfWeb>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        WebGameEvents.ResetRun();
        CacheReferences();
        ApplyPausedState(true);
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    public void StartPlayTransition()
    {
        hasStarted = true;
        localStartSequenceIndex = 0;
        CacheReferences();
        ApplyPausedState(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WebGameEvents.ResetRun();
        localStartSequenceIndex = 0;
        CacheReferences();
        ApplyPausedState(!hasStarted);
    }

    private void Update()
    {
        if (hasStarted || !enableLocalStartSequence)
        {
            return;
        }

        HandleLocalStartSequence();
    }

    private void CacheReferences()
    {
        if (looper == null)
        {
            looper = FindObjectOfType<EndlessTrackLooper>();
        }

        if (runner == null)
        {
            runner = FindObjectOfType<RunnerLaneController>();
        }

        if (cameraShift == null)
        {
            cameraShift = FindObjectOfType<CameraSideShift>();
        }
    }

    private void ApplyPausedState(bool paused)
    {
        if (looper != null)
        {
            looper.PauseWorld(paused);
        }

        if (runner != null)
        {
            runner.LockInput(paused);
        }

        if (cameraShift != null)
        {
            cameraShift.SetIntroActive(paused);
        }
    }

    private void HandleLocalStartSequence()
    {
        if (localStartSequenceIndex > 0 && Time.unscaledTime > localStartSequenceDeadline)
        {
            localStartSequenceIndex = 0;
        }

        if (!Input.anyKeyDown)
        {
            return;
        }

        KeyCode expected = localStartSequence[localStartSequenceIndex];
        if (Input.GetKeyDown(expected))
        {
            localStartSequenceIndex++;
            localStartSequenceDeadline = Time.unscaledTime + maxDelayBetweenSequenceKeys;

            if (localStartSequenceIndex >= localStartSequence.Length)
            {
                StartPlayTransition();
            }

            return;
        }

        if (Input.GetKeyDown(localStartSequence[0]))
        {
            localStartSequenceIndex = 1;
            localStartSequenceDeadline = Time.unscaledTime + maxDelayBetweenSequenceKeys;
            return;
        }

        localStartSequenceIndex = 0;
    }
}
