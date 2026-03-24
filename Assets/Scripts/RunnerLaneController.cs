using UnityEngine;

public class RunnerLaneController : MonoBehaviour
{
    [Header("Lane Setup")]
    [Tooltip("Optional explicit lane centers (left -> right). If assigned, these drive centering.")]
    [SerializeField] private Transform[] laneCenters;
    [SerializeField] private int laneCount = 3;
    [SerializeField] private float laneWidth = 2.5f;
    [SerializeField] private float centerX = 0f;

    [Header("Movement")]
    [SerializeField] private float laneLerpSpeed = 14f;
    [SerializeField] private bool lockZPosition = true;
    [SerializeField] private bool alsoUseArrowKeys = true;

    [Header("Jump")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundSnapVelocity = -2f;
    [SerializeField] private Animator animator;
    [SerializeField] private bool autoFindAnimator = true;
    [SerializeField] private string jumpTriggerName = "Jump";
    [SerializeField] private string groundedBoolName = "IsGrounded";

    [Header("Directional Sprint Anim")]
    [SerializeField] private bool useDirectionalSprintStates = true;
    [SerializeField] private int animatorLayerIndex = 0;
    [SerializeField] private float laneSwitchBlendTime = 0.07f;
    [SerializeField] private float laneSwitchHoldTime = 0.16f;
    [SerializeField] private string sprintForwardStateName = "HumanM@Sprint01_Forward";
    [SerializeField] private string sprintLeftStateName = "HumanM@Sprint01_ForwardLeft";
    [SerializeField] private string sprintRightStateName = "HumanM@Sprint01_ForwardRight";

    private int laneIndex;
    private float fixedZ;
    private float fixedY;
    private float verticalVelocity;
    private bool inputLocked;
    private bool isGrounded;
    private bool hasJumpTriggerParam;
    private bool hasGroundedBoolParam;
    private bool hasSprintForwardState;
    private bool hasSprintLeftState;
    private bool hasSprintRightState;
    private int sprintForwardStateHash;
    private int sprintLeftStateHash;
    private int sprintRightStateHash;
    private int runtimeAnimatorLayerIndex;
    private float laneSwitchAnimTimer;

    private void Awake()
    {
        fixedZ = transform.position.z;
        fixedY = transform.position.y;
        isGrounded = true;
        verticalVelocity = groundSnapVelocity;

        if (laneCenters != null && laneCenters.Length > 0)
        {
            laneCount = laneCenters.Length;
            laneIndex = GetClosestLaneIndex(transform.position.x);
        }
        else
        {
            laneIndex = laneCount / 2;
            if (Mathf.Abs(centerX) < 0.0001f)
            {
                centerX = transform.position.x;
            }
        }

        if (autoFindAnimator && animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorParams();
    }

    private void Update()
    {
        bool allowInput = !inputLocked;

        if (allowInput)
        {
            bool leftPressed = Input.GetKeyDown(KeyCode.A) || (alsoUseArrowKeys && Input.GetKeyDown(KeyCode.LeftArrow));
            bool rightPressed = Input.GetKeyDown(KeyCode.D) || (alsoUseArrowKeys && Input.GetKeyDown(KeyCode.RightArrow));

            if (leftPressed)
            {
                MoveLeft();
            }
            else if (rightPressed)
            {
                MoveRight();
            }

            if (Input.GetKeyDown(jumpKey))
            {
                TriggerJumpAnimation();
                if (isGrounded)
                {
                    StartJump();
                }
            }
        }

        UpdateDirectionalSprintAnimation();

        float targetX = GetLaneX(laneIndex);

        Vector3 p = transform.position;
        float t = 1f - Mathf.Exp(-laneLerpSpeed * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, targetX, t);
        p.y = UpdateVertical(p.y);

        if (lockZPosition)
        {
            p.z = fixedZ;
        }

        transform.position = p;
        UpdateAnimatorGroundedState();
    }

    public void MoveLeft()
    {
        int previous = laneIndex;
        laneIndex = Mathf.Clamp(laneIndex - 1, 0, laneCount - 1);
        if (laneIndex != previous)
        {
            PlayLaneSwitchAnimation(-1);
        }
    }

    public void MoveRight()
    {
        int previous = laneIndex;
        laneIndex = Mathf.Clamp(laneIndex + 1, 0, laneCount - 1);
        if (laneIndex != previous)
        {
            PlayLaneSwitchAnimation(1);
        }
    }

    public void LockInput(bool shouldLock)
    {
        inputLocked = shouldLock;
    }

    public void ForceLane(int index)
    {
        laneIndex = Mathf.Clamp(index, 0, laneCount - 1);
        Vector3 p = transform.position;
        p.x = GetLaneX(laneIndex);
        p.y = Mathf.Max(p.y, fixedY);
        if (lockZPosition) p.z = fixedZ;
        transform.position = p;
    }

    private void StartJump()
    {
        isGrounded = false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void TriggerJumpAnimation()
    {
        if (animator != null && hasJumpTriggerParam)
        {
            animator.SetTrigger(jumpTriggerName);
        }
    }

    private float UpdateVertical(float currentY)
    {
        verticalVelocity += gravity * Time.deltaTime;
        float nextY = currentY + verticalVelocity * Time.deltaTime;

        if (nextY <= fixedY)
        {
            nextY = fixedY;
            isGrounded = true;
            if (verticalVelocity < groundSnapVelocity)
            {
                verticalVelocity = groundSnapVelocity;
            }
        }
        else
        {
            isGrounded = false;
        }

        return nextY;
    }

    private void UpdateAnimatorGroundedState()
    {
        if (animator == null || !hasGroundedBoolParam)
        {
            return;
        }

        animator.SetBool(groundedBoolName, isGrounded);
    }

    private void CacheAnimatorParams()
    {
        hasJumpTriggerParam = false;
        hasGroundedBoolParam = false;
        hasSprintForwardState = false;
        hasSprintLeftState = false;
        hasSprintRightState = false;
        runtimeAnimatorLayerIndex = 0;

        if (animator == null)
        {
            return;
        }

        runtimeAnimatorLayerIndex = Mathf.Clamp(animatorLayerIndex, 0, animator.layerCount - 1);

        if (!string.IsNullOrEmpty(jumpTriggerName))
        {
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == jumpTriggerName)
                {
                    hasJumpTriggerParam = true;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(groundedBoolName))
        {
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.parameters[i];
                if (p.type == AnimatorControllerParameterType.Bool && p.name == groundedBoolName)
                {
                    hasGroundedBoolParam = true;
                    break;
                }
            }
        }

        hasSprintForwardState = TryResolveStateHash(sprintForwardStateName, out sprintForwardStateHash);
        hasSprintLeftState = TryResolveStateHash(sprintLeftStateName, out sprintLeftStateHash);
        hasSprintRightState = TryResolveStateHash(sprintRightStateName, out sprintRightStateHash);
    }

    private void PlayLaneSwitchAnimation(int direction)
    {
        if (!useDirectionalSprintStates || animator == null || !isGrounded)
        {
            return;
        }

        if (direction < 0 && hasSprintLeftState)
        {
            animator.CrossFade(sprintLeftStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
            laneSwitchAnimTimer = laneSwitchHoldTime;
            return;
        }

        if (direction > 0 && hasSprintRightState)
        {
            animator.CrossFade(sprintRightStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
            laneSwitchAnimTimer = laneSwitchHoldTime;
        }
    }

    private void UpdateDirectionalSprintAnimation()
    {
        if (!useDirectionalSprintStates || animator == null || !hasSprintForwardState || !isGrounded)
        {
            return;
        }

        if (laneSwitchAnimTimer > 0f)
        {
            laneSwitchAnimTimer -= Time.deltaTime;
            return;
        }

        if (animator.IsInTransition(runtimeAnimatorLayerIndex))
        {
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(runtimeAnimatorLayerIndex);
        bool isForward = IsStateMatch(info, sprintForwardStateHash);
        if (isForward)
        {
            return;
        }

        bool isLeft = IsStateMatch(info, sprintLeftStateHash);
        bool isRight = IsStateMatch(info, sprintRightStateHash);
        if (!isLeft && !isRight)
        {
            return;
        }

        animator.CrossFade(sprintForwardStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
        laneSwitchAnimTimer = laneSwitchBlendTime;
    }

    private static bool IsStateMatch(AnimatorStateInfo stateInfo, int targetStateHash)
    {
        return stateInfo.shortNameHash == targetStateHash || stateInfo.fullPathHash == targetStateHash;
    }

    private bool TryResolveStateHash(string stateName, out int resolvedHash)
    {
        resolvedHash = 0;
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        int direct = Animator.StringToHash(stateName);
        if (animator.HasState(runtimeAnimatorLayerIndex, direct))
        {
            resolvedHash = direct;
            return true;
        }

        string layerName = animator.GetLayerName(runtimeAnimatorLayerIndex);
        int fullPath = Animator.StringToHash(layerName + "." + stateName);
        if (animator.HasState(runtimeAnimatorLayerIndex, fullPath))
        {
            resolvedHash = fullPath;
            return true;
        }

        return false;
    }

    private float GetLaneX(int index)
    {
        if (laneCenters != null && laneCenters.Length > 0)
        {
            if (index < laneCenters.Length && laneCenters[index] != null)
            {
                return laneCenters[index].position.x;
            }
        }

        float centerLane = (laneCount - 1) * 0.5f;
        return centerX + (index - centerLane) * laneWidth;
    }

    private int GetClosestLaneIndex(float xPos)
    {
        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < laneCenters.Length; i++)
        {
            if (laneCenters[i] == null) continue;
            float d = Mathf.Abs(laneCenters[i].position.x - xPos);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}

