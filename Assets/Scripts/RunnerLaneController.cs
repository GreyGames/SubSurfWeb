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

    private int laneIndex;
    private float fixedZ;
    private bool inputLocked;

    private void Awake()
    {
        fixedZ = transform.position.z;

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
    }

    private void Update()
    {
        if (inputLocked)
        {
            return;
        }

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

        float targetX = GetLaneX(laneIndex);

        Vector3 p = transform.position;
        float t = 1f - Mathf.Exp(-laneLerpSpeed * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, targetX, t);

        if (lockZPosition)
        {
            p.z = fixedZ;
        }

        transform.position = p;
    }

    public void MoveLeft()
    {
        laneIndex = Mathf.Clamp(laneIndex - 1, 0, laneCount - 1);
    }

    public void MoveRight()
    {
        laneIndex = Mathf.Clamp(laneIndex + 1, 0, laneCount - 1);
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
        if (lockZPosition) p.z = fixedZ;
        transform.position = p;
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
