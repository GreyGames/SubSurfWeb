using UnityEngine;

public class CameraSideShift : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float sideYawDegrees = 90f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;
    [Header("Orbit Speed Scaling")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private bool autoFindLooper = true;
    [SerializeField] private bool scaleOrbitWithSpeed = true;
    [SerializeField] private float orbitSpeedReference = 10f;
    [SerializeField] private float minOrbitDegreesPerSecond = 220f;
    [SerializeField] private float maxOrbitDegreesPerSecond = 720f;
    [Header("Side / Orbit")]
    [SerializeField] private float sideHeightOffset = -0.4f;
    [SerializeField] private float orbitDegreesPerSecond = 280f;
    [SerializeField] private float orbitHeightOffset = 0f;
    [SerializeField] private bool orbitSingleRotation = true;
    [SerializeField] private float orbitSingleRotationDegrees = 360f;
    [Header("Inactive Follow")]
    [SerializeField] private bool followTargetWhenInactive = true;
    [SerializeField] private bool followInactiveX = true;
    [SerializeField] private bool followInactiveY = false;
    [SerializeField] private bool followInactiveZ = false;
    [Header("Intro Pose")]
    [SerializeField] private bool introActiveOnStart = false;
    [SerializeField] private float introYawDegrees = 225f;
    [SerializeField] private float introHeightOffset = 0f;

    private Vector3 defaultOffset;
    private Quaternion defaultRotation;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private Vector3 restTargetPosition;
    private bool targetIsDirectParent;
    private bool sideActive;
    private float sideSign = 1f;
    private bool orbitActive;
    private float orbitSign = 1f;
    private float orbitYaw;
    private float orbitAccumDegrees;
    private bool introActive;
    private bool hasSetup;

    private void Awake()
    {
        introActive = introActiveOnStart;
        TrySetup();
    }

    private void LateUpdate()
    {
        if (!TrySetup())
        {
            return;
        }

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        bool useLocalInactiveFollow =
            !orbitActive &&
            !sideActive &&
            !introActive &&
            followTargetWhenInactive &&
            targetIsDirectParent;

        if (useLocalInactiveFollow)
        {
            // Camera is parented to target, so local-space follow avoids world-space jitter.
            transform.localPosition = Vector3.Lerp(transform.localPosition, restLocalPosition, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, restLocalRotation, t);
            return;
        }

        Vector3 desiredOffset = defaultOffset;
        if (orbitActive)
        {
            float orbitSpeed = orbitDegreesPerSecond;
            if (scaleOrbitWithSpeed)
            {
                float speed = looper != null ? looper.CurrentSpeed : 0f;
                if (speed > 0f)
                {
                    float refSpeed = Mathf.Max(0.1f, orbitSpeedReference);
                    orbitSpeed = orbitDegreesPerSecond * (speed / refSpeed);
                }
                orbitSpeed = Mathf.Clamp(orbitSpeed, minOrbitDegreesPerSecond, maxOrbitDegreesPerSecond);
            }
            float delta = orbitSpeed * orbitSign * Time.unscaledDeltaTime;
            if (orbitSingleRotation)
            {
                float limit = Mathf.Max(1f, orbitSingleRotationDegrees);
                float remaining = Mathf.Max(0f, limit - orbitAccumDegrees);
                float deltaAbs = Mathf.Abs(delta);
                if (remaining <= 0.0001f)
                {
                    delta = 0f;
                }
                else if (deltaAbs > remaining)
                {
                    delta = Mathf.Sign(delta) * remaining;
                    orbitAccumDegrees = limit;
                }
                else
                {
                    orbitAccumDegrees += deltaAbs;
                }
            }
            orbitYaw += delta;
            desiredOffset = Quaternion.Euler(0f, orbitYaw, 0f) * defaultOffset;
            desiredOffset.y += orbitHeightOffset;
        }
        else if (sideActive)
        {
            float signedYaw = sideYawDegrees * sideSign;
            desiredOffset = Quaternion.Euler(0f, signedYaw, 0f) * defaultOffset;
            desiredOffset.y += sideHeightOffset;
        }
        else if (introActive)
        {
            desiredOffset = Quaternion.Euler(0f, introYawDegrees, 0f) * defaultOffset;
            desiredOffset.y += introHeightOffset;
        }

        Vector3 desiredPos;
        if (orbitActive || sideActive || introActive)
        {
            desiredPos = target.position + desiredOffset;
        }
        else if (followTargetWhenInactive)
        {
            Vector3 delta = target.position - restTargetPosition;
            Vector3 followDelta = new Vector3(
                followInactiveX ? delta.x : 0f,
                followInactiveY ? delta.y : 0f,
                followInactiveZ ? delta.z : 0f);
            desiredPos = restPosition + followDelta;
        }
        else
        {
            desiredPos = restPosition;
        }
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);

        Quaternion desiredRot;
        if (orbitActive || sideActive || introActive || followTargetWhenInactive)
        {
            desiredRot = lookAtTarget
                ? Quaternion.LookRotation(target.position - transform.position, Vector3.up)
                : defaultRotation;
        }
        else
        {
            desiredRot = restRotation;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
    }

    public void SetSideActive(bool active, float newSideSign = 1f)
    {
        sideActive = active;
        if (active)
        {
            orbitActive = false;
            introActive = false;
        }
        if (Mathf.Abs(newSideSign) > 0.001f)
        {
            sideSign = Mathf.Sign(newSideSign);
        }
    }

    public void SetOrbitActive(bool active, float newOrbitSign = 1f)
    {
        if (active && !orbitActive)
        {
            orbitYaw = 0f;
            orbitAccumDegrees = 0f;
        }
        orbitActive = active;
        if (active)
        {
            sideActive = false;
            introActive = false;
        }
        if (Mathf.Abs(newOrbitSign) > 0.001f)
        {
            orbitSign = Mathf.Sign(newOrbitSign);
        }
    }

    public void SetIntroActive(bool active)
    {
        introActive = active;
        if (active)
        {
            sideActive = false;
            orbitActive = false;
        }
    }

    private bool TrySetup()
    {
        if (hasSetup)
        {
            return target != null;
        }

        if (autoFindTarget && target == null)
        {
            RunnerLaneController runner = FindObjectOfType<RunnerLaneController>();
            if (runner != null)
            {
                target = runner.transform;
            }
        }

        if (autoFindLooper && looper == null)
        {
            looper = FindObjectOfType<EndlessTrackLooper>();
        }

        if (target == null)
        {
            return false;
        }

        restPosition = transform.position;
        restRotation = transform.rotation;
        restLocalPosition = transform.localPosition;
        restLocalRotation = transform.localRotation;
        restTargetPosition = target.position;
        targetIsDirectParent = transform.parent == target;
        defaultOffset = transform.position - target.position;
        defaultRotation = transform.rotation;
        hasSetup = true;
        return true;
    }
}
