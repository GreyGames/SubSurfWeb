using UnityEngine;

public class CameraSideShift : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float sideYawDegrees = 90f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private float sideHeightOffset = -0.4f;
    [SerializeField] private bool followTargetWhenInactive = true;
    [SerializeField] private bool followInactiveX = true;
    [SerializeField] private bool followInactiveY = false;
    [SerializeField] private bool followInactiveZ = false;

    private Vector3 defaultOffset;
    private Quaternion defaultRotation;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restTargetPosition;
    private bool sideActive;
    private float sideSign = 1f;
    private bool hasSetup;

    private void Awake()
    {
        TrySetup();
    }

    private void LateUpdate()
    {
        if (!TrySetup())
        {
            return;
        }

        float signedYaw = sideYawDegrees * sideSign;
        Vector3 desiredOffset = sideActive
            ? Quaternion.Euler(0f, signedYaw, 0f) * defaultOffset
            : defaultOffset;
        if (sideActive)
        {
            desiredOffset.y += sideHeightOffset;
        }

        Vector3 desiredPos;
        if (sideActive)
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
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);

        Quaternion desiredRot;
        if (sideActive || followTargetWhenInactive)
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
        if (Mathf.Abs(newSideSign) > 0.001f)
        {
            sideSign = Mathf.Sign(newSideSign);
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

        if (target == null)
        {
            return false;
        }

        restPosition = transform.position;
        restRotation = transform.rotation;
        restTargetPosition = target.position;
        defaultOffset = transform.position - target.position;
        defaultRotation = transform.rotation;
        hasSetup = true;
        return true;
    }
}
