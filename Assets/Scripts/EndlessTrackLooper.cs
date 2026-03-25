using System.Collections.Generic;
using UnityEngine;

public class EndlessTrackLooper : MonoBehaviour
{
    [Header("Loop Bounds")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float segmentLength = 20f;
    [SerializeField] private float despawnBuffer = 0f;
    [SerializeField] private float spawnBuffer = 0f;

    [Header("Runtime")]
    [SerializeField] private float startSpeed = 10f;
    [SerializeField] private float maxSpeed = 26f;
    [SerializeField] private float acceleration = 0.35f;
    [SerializeField] private bool ensureTrackSegmentComponent = true;
    [SerializeField] private bool autoFindSegmentsFromChildren = true;
    [SerializeField] private bool autoCreateSideEnvironmentSpawner = true;
    [SerializeField] private bool autoCreateScoreManager = true;
    [SerializeField] private bool autoCreateCameraSideShift = true;
    [SerializeField] private List<Transform> segments = new List<Transform>();

    [Header("Visual - Distance Fog")]
    [SerializeField] private bool applyFog = true;
    [SerializeField] private FogMode fogMode = FogMode.Linear;
    [SerializeField] private Color fogColor = new Color(0.75f, 0.8f, 0.88f, 1f);
    [SerializeField] private float fogStartDistance = 45f;
    [SerializeField] private float fogEndDistance = 130f;
    [SerializeField] private float fogDensity = 0.012f;

    private readonly System.Random rng = new System.Random();
    private Vector3 moveAxis;
    private Vector3 spawnAxis;
    private float currentSpeed;
    private bool paused;

    public event System.Action<Transform> SegmentRepositioned;
    public IReadOnlyList<Transform> Segments => segments;
    public Transform StartPoint => startPoint;
    public Transform EndPoint => endPoint;
    public Vector3 MoveAxis => moveAxis;
    public Vector3 SpawnAxis => spawnAxis;
    public float CurrentSpeed => paused ? 0f : currentSpeed;
    public bool IsPaused => paused;

    private void Awake()
    {
        moveAxis = moveDirection.normalized;
        if (moveAxis.sqrMagnitude < 0.99f)
        {
            moveAxis = Vector3.back;
        }
        spawnAxis = -moveAxis;

        if (autoFindSegmentsFromChildren && segments.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == startPoint || child == endPoint)
                {
                    continue;
                }

                // Terrain should never be treated as a recyclable segment.
                if (child.GetComponent("Terrain") != null || child.GetComponent("TerrainCollider") != null)
                {
                    continue;
                }

                segments.Add(child);
            }
        }

        currentSpeed = startSpeed;

        if (ensureTrackSegmentComponent)
        {
            EnsureTrackSegments();
        }

        if (autoCreateSideEnvironmentSpawner && GetComponent<SideEnvironmentSpawner>() == null)
        {
            gameObject.AddComponent<SideEnvironmentSpawner>();
        }

        if (autoCreateScoreManager && GetComponent<ScoreManager>() == null)
        {
            gameObject.AddComponent<ScoreManager>();
        }

        if (autoCreateCameraSideShift)
        {
            Camera main = Camera.main;
            if (main != null && main.GetComponent<CameraSideShift>() == null)
            {
                main.gameObject.AddComponent<CameraSideShift>();
            }
        }
    }

    private void Start()
    {
        ApplyFogSettings();
        AlignSegmentsFromStart();
    }

    private void Update()
    {
        if (paused || segments.Count == 0 || startPoint == null || endPoint == null)
        {
            return;
        }

        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime);
        float moveDistance = currentSpeed * Time.deltaTime;
        Vector3 frameMove = moveAxis * moveDistance;

        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].position += frameMove;
        }

        RecycleSegments();
    }

    public void PauseWorld(bool shouldPause)
    {
        paused = shouldPause;
    }

    public void ResetSpeed()
    {
        currentSpeed = startSpeed;
    }

    [ContextMenu("Align Segments From StartPoint")]
    public void AlignSegmentsFromStart()
    {
        if (startPoint == null || segments.Count == 0)
        {
            return;
        }

        // Segment 0 at StartPoint, then extend toward movement direction.
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];
            Vector3 pos = startPoint.position + moveAxis * (i * segmentLength);
            seg.position = new Vector3(pos.x, seg.position.y, pos.z);
            seg.rotation = Quaternion.identity;

            TrackSegment segmentData = GetTrackSegment(seg);
            if (segmentData != null)
            {
                segmentData.OnRecycled(rng, spawnAxis);
            }

            NotifySegmentRepositioned(seg);
        }
    }

    private void RecycleSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];
            float passedDistance = Vector3.Dot(seg.position - endPoint.position, moveAxis);
            if (passedDistance <= despawnBuffer)
            {
                continue;
            }

            float currentSpawnProjection = Vector3.Dot(seg.position, spawnAxis);
            float frontMostSpawnProjection = GetFrontMostSpawnProjection();
            float newSpawnProjection = frontMostSpawnProjection + segmentLength + spawnBuffer;
            float delta = newSpawnProjection - currentSpawnProjection;

            seg.position += spawnAxis * delta;

            TrackSegment segmentData = GetTrackSegment(seg);
            if (segmentData != null)
            {
                segmentData.OnRecycled(rng, spawnAxis);
            }

            NotifySegmentRepositioned(seg);
        }
    }

    private float GetFrontMostSpawnProjection()
    {
        float best = float.MinValue;
        for (int i = 0; i < segments.Count; i++)
        {
            float projection = Vector3.Dot(segments[i].position, spawnAxis);
            if (projection > best)
            {
                best = projection;
            }
        }

        return best;
    }


    private void ApplyFogSettings()
    {
        RenderSettings.fog = applyFog;
        if (!applyFog)
        {
            return;
        }

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;

        if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }
        else
        {
            RenderSettings.fogDensity = fogDensity;
        }
    }

    private void EnsureTrackSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];
            if (seg == null)
            {
                continue;
            }

            if (seg.GetComponent<TrackSegment>() == null)
            {
                seg.gameObject.AddComponent<TrackSegment>();
            }
        }
    }

    private TrackSegment GetTrackSegment(Transform seg)
    {
        if (seg == null)
        {
            return null;
        }

        TrackSegment existing = seg.GetComponent<TrackSegment>();
        if (existing != null)
        {
            return existing;
        }

        if (!ensureTrackSegmentComponent)
        {
            return null;
        }

        return seg.gameObject.AddComponent<TrackSegment>();
    }

    private void NotifySegmentRepositioned(Transform seg)
    {
        SegmentRepositioned?.Invoke(seg);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPoint.position, 0.4f);
        Vector3 editorMoveAxis = moveDirection.normalized.sqrMagnitude > 0.5f ? moveDirection.normalized : Vector3.back;
        Gizmos.DrawLine(startPoint.position, startPoint.position + editorMoveAxis * 2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPoint.position, startPoint.position - editorMoveAxis * 2f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPoint.position, 0.4f);
    }
#endif
}
