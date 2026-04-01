using UnityEngine;

public class SlowMoZone : MonoBehaviour
{
    [SerializeField] private float slowTimeScale = 0.35f;
    [SerializeField] private bool adjustFixedDeltaTime = true;
    [SerializeField] private bool autoFindCameraShift = true;
    [SerializeField] private CameraSideShift cameraShift;
    [SerializeField] private float minActiveDuration = 1.0f;
    [Header("Coin Multiplier")]
    [SerializeField] private bool grantCoinMultiplier = true;
    [SerializeField] private float coinMultiplier = 2f;
    [SerializeField] private float coinMultiplierDuration = 10f;

    private float defaultFixedDelta;
    private int overlapCount;
    private bool isActive;
    private float cameraSideSign = 1f;
    private bool useCameraOrbit;
    private float cameraOrbitSign = 1f;
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

        ApplyCoinMultiplier();
        NotifyCoinMultiplierPopup();

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
            useCameraOrbit = false;
            if (isActive && cameraShift != null)
            {
                cameraShift.SetSideActive(true, cameraSideSign);
                cameraShift.SetOrbitActive(false);
            }
        }
    }

    public void SetCameraOrbit(bool orbit, float orbitSign = 1f)
    {
        useCameraOrbit = orbit;
        if (Mathf.Abs(orbitSign) > 0.001f)
        {
            cameraOrbitSign = Mathf.Sign(orbitSign);
        }

        if (isActive && cameraShift != null)
        {
            cameraShift.SetOrbitActive(useCameraOrbit, cameraOrbitSign);
            if (useCameraOrbit)
            {
                cameraShift.SetSideActive(false, cameraSideSign);
            }
            else
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
            if (useCameraOrbit)
            {
                cameraShift.SetOrbitActive(true, cameraOrbitSign);
                cameraShift.SetSideActive(false, cameraSideSign);
            }
            else
            {
                cameraShift.SetSideActive(true, cameraSideSign);
                cameraShift.SetOrbitActive(false, cameraOrbitSign);
            }
        }
    }

    private void ApplyCoinMultiplier()
    {
        if (!grantCoinMultiplier || coinMultiplier <= 1f || coinMultiplierDuration <= 0f)
        {
            return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ApplyCoinMultiplier(coinMultiplier, coinMultiplierDuration);
        }
    }

    private void NotifyCoinMultiplierPopup()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ShowCoinMultiplierPopup(2f);
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
            cameraShift.SetOrbitActive(false, cameraOrbitSign);
            cameraShift.SetSideActive(false, cameraSideSign);
        }
    }
}
