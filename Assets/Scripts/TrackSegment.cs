using System.Collections.Generic;
using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    [Header("Layout Variants")]
    [Tooltip("Only one of these roots is active at a time. Put different obstacle layouts here.")]
    [SerializeField] private GameObject[] layoutVariants;
    [SerializeField] private bool randomizeYaw;
    [SerializeField] private float[] yawOptions = { 0f };

    [Header("Random Obstacle (Single)")]
    [Tooltip("Use a simple cube prefab for now. One obstacle is reused and moved each recycle.")]
    [SerializeField] private GameObject obstaclePrefab;
    [Tooltip("Lane center points in this segment, ordered Left -> Mid -> Right.")]
    [SerializeField] private Transform[] obstacleLaneCenters;
    [SerializeField] private bool autoDetectLaneCenters = true;
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSpawnChance = 0.7f;
    [SerializeField] private Vector2 obstacleForwardDistanceRange = new Vector2(4f, 10f);
    [SerializeField] private Vector3 obstacleLocalOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private bool obstacleZeroRotation = true;
    [SerializeField] private Vector3 obstacleLocalScale = Vector3.one;

    private GameObject pooledObstacle;
    private readonly List<Vector3> runtimeLaneLocalPositions = new List<Vector3>(3);

    public void OnRecycled(System.Random rng)
    {
        OnRecycled(rng, transform.forward);
    }

    public void OnRecycled(System.Random rng, Vector3 worldSpawnAxis)
    {
        if (layoutVariants != null && layoutVariants.Length > 0)
        {
            int selected = rng.Next(layoutVariants.Length);
            for (int i = 0; i < layoutVariants.Length; i++)
            {
                if (layoutVariants[i] != null)
                {
                    layoutVariants[i].SetActive(i == selected);
                }
            }
        }

        if (randomizeYaw && yawOptions != null && yawOptions.Length > 0)
        {
            float y = yawOptions[rng.Next(yawOptions.Length)];
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        RandomizeObstacle(rng, worldSpawnAxis);
    }

    private void RandomizeObstacle(System.Random rng, Vector3 worldSpawnAxis)
    {
        if (!TryBuildLaneCenters())
        {
            SetObstacleActive(false);
            return;
        }

        bool shouldSpawn = rng.NextDouble() <= obstacleSpawnChance;
        if (!shouldSpawn)
        {
            SetObstacleActive(false);
            return;
        }

        if (pooledObstacle == null)
        {
            pooledObstacle = CreateObstacleInstance();
            pooledObstacle.name = "Obstacle_Runtime";
        }

        int laneIndex = rng.Next(runtimeLaneLocalPositions.Count);
        Vector3 laneLocalPos = runtimeLaneLocalPositions[laneIndex];
        Vector3 localSpawnDirection = transform.InverseTransformDirection(worldSpawnAxis);
        localSpawnDirection.y = 0f;
        if (localSpawnDirection.sqrMagnitude < 0.0001f)
        {
            localSpawnDirection = Vector3.forward;
        }
        localSpawnDirection.Normalize();

        float minForward = Mathf.Min(obstacleForwardDistanceRange.x, obstacleForwardDistanceRange.y);
        float maxForward = Mathf.Max(obstacleForwardDistanceRange.x, obstacleForwardDistanceRange.y);
        float t = (float)rng.NextDouble();
        float forwardDistance = Mathf.Lerp(minForward, maxForward, t);

        pooledObstacle.SetActive(true);
        pooledObstacle.transform.localPosition = laneLocalPos + obstacleLocalOffset + localSpawnDirection * forwardDistance;
        pooledObstacle.transform.localRotation = obstacleZeroRotation ? Quaternion.identity : pooledObstacle.transform.localRotation;
        pooledObstacle.transform.localScale = obstacleLocalScale;
    }

    private void SetObstacleActive(bool state)
    {
        if (pooledObstacle != null)
        {
            pooledObstacle.SetActive(state);
        }
    }

    private bool TryBuildLaneCenters()
    {
        runtimeLaneLocalPositions.Clear();

        if (obstacleLaneCenters != null && obstacleLaneCenters.Length > 0)
        {
            for (int i = 0; i < obstacleLaneCenters.Length; i++)
            {
                if (obstacleLaneCenters[i] != null)
                {
                    runtimeLaneLocalPositions.Add(obstacleLaneCenters[i].localPosition);
                }
            }
        }

        if (runtimeLaneLocalPositions.Count > 0)
        {
            return true;
        }

        if (!autoDetectLaneCenters)
        {
            return false;
        }

        List<Transform> candidates = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child.name == "Obstacle_Runtime" || child.name.StartsWith("SideBuilding_Runtime"))
            {
                continue;
            }

            if (child.GetComponent<Renderer>() != null)
            {
                candidates.Add(child);
            }
        }

        candidates.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        for (int i = 0; i < candidates.Count; i++)
        {
            runtimeLaneLocalPositions.Add(candidates[i].localPosition);
            if (runtimeLaneLocalPositions.Count >= 3)
            {
                break;
            }
        }

        return runtimeLaneLocalPositions.Count > 0;
    }

    private GameObject CreateObstacleInstance()
    {
        if (obstaclePrefab != null)
        {
            return Instantiate(obstaclePrefab, transform);
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform, false);
        return cube;
    }
}
