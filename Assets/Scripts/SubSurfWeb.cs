using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSurfWeb : MonoBehaviour
{
    private static SubSurfWeb instance;

    private EndlessTrackLooper looper;
    private RunnerLaneController runner;
    private CameraSideShift cameraShift;
    private bool hasStarted;

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
        CacheReferences();
        ApplyPausedState(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheReferences();
        ApplyPausedState(!hasStarted);
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
}
