using UnityEngine;

public class SlowMoZone : MonoBehaviour
{
    [SerializeField] private float slowTimeScale = 0.35f;
    [SerializeField] private bool adjustFixedDeltaTime = true;
    [SerializeField] private bool autoFindCameraShift = true;
    [SerializeField] private CameraSideShift cameraShift;
    [SerializeField] private float minActiveDuration = 1.0f;

    private float defaultFixedDelta;
    private int overlapCount;
    private bool isActive;
    private float cameraSideSign = 1f;
    private bool pendingDeactivate;
    private float pendingDeactivateAt;
    private float minActiveUntil;

    private void Awake()
    {
        defaultFixedDelta = Time.fixedDeltaTime;
        if (autoFindCameraShift && cameraShift == null)
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        overlapCount++;
        if (!isActive)
        {
            ActivateSlowMo();
        }

        minActiveUntil = Time.unscaledTime + minActiveDuration;
        pendingDeactivate = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        overlapCount = Mathf.Max(0, overlapCount - 1);
        if (overlapCount == 0)
        {
            if (Time.unscaledTime < minActiveUntil)
            {
                pendingDeactivate = true;
                pendingDeactivateAt = minActiveUntil;
            }
            else
            {
                DeactivateSlowMo();
            }
        }
    }

    private void Update()
    {
        if (pendingDeactivate && overlapCount == 0 && Time.unscaledTime >= pendingDeactivateAt)
        {
            pendingDeactivate = false;
            DeactivateSlowMo();
        }
    }

    public void SetSlowScale(float scale)
    {
        slowTimeScale = Mathf.Clamp(scale, 0.05f, 1f);
    }

    public void SetCameraSideSign(float sideSign)
    {
        if (Mathf.Abs(sideSign) > 0.001f)
        {
            cameraSideSign = Mathf.Sign(sideSign);
            if (isActive && cameraShift != null)
            {
                cameraShift.SetSideActive(true, cameraSideSign);
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other != null && other.GetComponentInParent<RunnerLaneController>() != null;
    }

    private void ActivateSlowMo()
    {
        isActive = true;
        Time.timeScale = slowTimeScale;
        if (adjustFixedDeltaTime)
        {
            Time.fixedDeltaTime = defaultFixedDelta * slowTimeScale;
        }

        if (cameraShift != null)
        {
            cameraShift.SetSideActive(true, cameraSideSign);
        }
    }

    private void DeactivateSlowMo()
    {
        isActive = false;
        pendingDeactivate = false;
        Time.timeScale = 1f;
        if (adjustFixedDeltaTime)
        {
            Time.fixedDeltaTime = defaultFixedDelta;
        }

        if (cameraShift != null)
        {
            cameraShift.SetSideActive(false, cameraSideSign);
        }
    }
}
