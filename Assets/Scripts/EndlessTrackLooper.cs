using System;
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
    [SerializeField] private bool autoFindSegmentsFromChildren = true;
    [SerializeField] private List<Transform> segments = new List<Transform>();

    private readonly System.Random rng = new System.Random();
    private Vector3 moveAxis;
    private Vector3 spawnAxis;
    private float currentSpeed;
    private bool paused;

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

                segments.Add(child);
            }
        }

        currentSpeed = startSpeed;
    }

    private void Start()
    {
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

            TrackSegment segmentData = seg.GetComponent<TrackSegment>();
            if (segmentData != null)
            {
                segmentData.OnRecycled(rng);
            }
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

            TrackSegment segmentData = seg.GetComponent<TrackSegment>();
            if (segmentData != null)
            {
                segmentData.OnRecycled(rng);
            }
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
