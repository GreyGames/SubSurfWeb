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

    [Header("Slow Motion Zone")]
    [SerializeField] private bool enableSlowMoZone = true;
    [Range(0f, 1f)]
    [SerializeField] private float slowMoZoneChance = 0.4f;
    [SerializeField] private Vector2 slowMoZoneForwardRange = new Vector2(3f, 9f);
    [SerializeField] private Vector3 slowMoZoneSize = new Vector3(5f, 3f, 5f);
    [SerializeField] private bool autoSizeSlowMoToLane = true;
    [SerializeField] private float slowMoLaneWidthScale = 0.85f;
    [SerializeField] private Vector3 slowMoZoneLocalOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float slowMoTimeScale = 0.35f;
    [SerializeField] private float slowMoZoneEdgePadding = 3f;
    [SerializeField] private bool slowMoZonePreferFrontHalf = false;
    [SerializeField] private int slowMoMinSegmentsBetween = 3;
    [SerializeField] private bool slowMoIncludeCenterLane = true;
    [SerializeField] private float slowMoCenterOrbitDirection = 1f;
    [SerializeField] private bool showSlowMoVisual = true;
    [SerializeField] private Color slowMoVisualColor = new Color(0.2f, 0.85f, 1f, 0.5f);
    [SerializeField] private float slowMoVisualHeight = 0.06f;
    [SerializeField] private float slowMoVisualYOffset = 0.01f;

    private GameObject pooledObstacle;
    private readonly List<Vector3> runtimeLaneLocalPositions = new List<Vector3>(3);
    private GameObject slowMoZoneObject;
    private BoxCollider slowMoZoneCollider;
    private SlowMoZone slowMoZone;
    private GameObject slowMoZoneVisual;
    private MeshRenderer slowMoZoneRenderer;
    private Material slowMoZoneMaterial;
    private static int segmentsSinceSlowMo = 1000;

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
        UpdateSlowMoZone(rng);
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
            GameObject instance = Instantiate(obstaclePrefab, transform);
            EnsureObstacleMarker(instance);
            return instance;
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform, false);
        EnsureObstacleMarker(cube);
        return cube;
    }

    private static void EnsureObstacleMarker(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (instance.GetComponent<ObstacleMarker>() == null)
        {
            instance.AddComponent<ObstacleMarker>();
        }
    }

    private void UpdateSlowMoZone(System.Random rng)
    {
        if (!enableSlowMoZone)
        {
            if (slowMoZoneObject != null)
            {
                slowMoZoneObject.SetActive(false);
            }
            return;
        }

        segmentsSinceSlowMo++;
        if (segmentsSinceSlowMo < Mathf.Max(0, slowMoMinSegmentsBetween))
        {
            if (slowMoZoneObject != null)
            {
                slowMoZoneObject.SetActive(false);
            }
            return;
        }

        bool shouldSpawn = rng.NextDouble() <= slowMoZoneChance;
        if (!shouldSpawn)
        {
            if (slowMoZoneObject != null)
            {
                slowMoZoneObject.SetActive(false);
            }
            return;
        }

        segmentsSinceSlowMo = 0;

        EnsureSlowMoZone();

        if (!TryBuildLaneCenters())
        {
            if (slowMoZoneObject != null)
            {
                slowMoZoneObject.SetActive(false);
            }
            return;
        }

        List<Vector3> sortedLanes = GetSortedLaneLocalPositions();
        if (sortedLanes.Count < 2)
        {
            if (slowMoZoneObject != null)
            {
                slowMoZoneObject.SetActive(false);
            }
            return;
        }

        bool useOrbit = false;
        float cameraSide = 1f;
        float laneX;
        if (slowMoIncludeCenterLane && sortedLanes.Count >= 3)
        {
            int choice = rng.Next(3); // 0=left, 1=middle, 2=right
            if (choice == 1)
            {
                laneX = sortedLanes[1].x;
                useOrbit = true;
            }
            else if (choice == 0)
            {
                laneX = sortedLanes[0].x;
                cameraSide = -1f;
            }
            else
            {
                laneX = sortedLanes[sortedLanes.Count - 1].x;
                cameraSide = 1f;
            }
        }
        else
        {
            bool leftLane = rng.Next(2) == 0;
            laneX = leftLane ? sortedLanes[0].x : sortedLanes[sortedLanes.Count - 1].x;
            cameraSide = leftLane ? -1f : 1f;
        }

        Vector3 zoneSize = slowMoZoneSize;
        if (autoSizeSlowMoToLane && sortedLanes.Count >= 2)
        {
            float laneWidth = Mathf.Abs(sortedLanes[1].x - sortedLanes[0].x);
            if (laneWidth > 0.01f)
            {
                zoneSize.x = Mathf.Max(0.5f, laneWidth * slowMoLaneWidthScale);
            }
        }

        float forward = RandomRange(rng, slowMoZoneForwardRange);
        if (TryGetSegmentLocalZRange(out float minZ, out float maxZ))
        {
            float half = zoneSize.z * 0.5f;
            float start = minZ + half + slowMoZoneEdgePadding;
            float end = maxZ - half - slowMoZoneEdgePadding;
            if (slowMoZonePreferFrontHalf)
            {
                float mid = (minZ + maxZ) * 0.5f;
                start = Mathf.Max(start, mid);
            }
            else
            {
                float mid = (minZ + maxZ) * 0.5f;
                end = Mathf.Min(end, mid);
            }

            if (end > start)
            {
                forward = RandomRange(rng, start, end);
            }
        }

        Vector3 localPos = new Vector3(laneX, slowMoZoneLocalOffset.y, slowMoZoneLocalOffset.z + forward);
        slowMoZoneObject.transform.localPosition = localPos;
        slowMoZoneCollider.size = zoneSize;
        slowMoZoneCollider.center = new Vector3(0f, zoneSize.y * 0.5f - slowMoZoneLocalOffset.y, 0f);
        slowMoZone.SetSlowScale(slowMoTimeScale);
        slowMoZone.SetCameraOrbit(useOrbit, slowMoCenterOrbitDirection);
        if (!useOrbit)
        {
            slowMoZone.SetCameraSideSign(cameraSide);
        }
        UpdateSlowMoVisual();
        slowMoZoneObject.SetActive(true);

    }

    private void EnsureSlowMoZone()
    {
        if (slowMoZoneObject != null)
        {
            return;
        }

        slowMoZoneObject = new GameObject("SlowMoZone");
        slowMoZoneObject.transform.SetParent(transform, false);
        slowMoZoneCollider = slowMoZoneObject.AddComponent<BoxCollider>();
        slowMoZoneCollider.isTrigger = true;
        slowMoZone = slowMoZoneObject.AddComponent<SlowMoZone>();
        EnsureSlowMoVisual();
    }

    private void EnsureSlowMoVisual()
    {
        if (slowMoZoneVisual != null)
        {
            return;
        }

        slowMoZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slowMoZoneVisual.name = "SlowMoVisual";
        slowMoZoneVisual.transform.SetParent(slowMoZoneObject.transform, false);
        slowMoZoneVisual.transform.localPosition = Vector3.zero;

        Collider visualCollider = slowMoZoneVisual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        slowMoZoneRenderer = slowMoZoneVisual.GetComponent<MeshRenderer>();
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        slowMoZoneMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        slowMoZoneRenderer.sharedMaterial = slowMoZoneMaterial;
        UpdateSlowMoVisual();
    }

    private void UpdateSlowMoVisual()
    {
        if (slowMoZoneVisual == null || slowMoZoneRenderer == null)
        {
            return;
        }

        slowMoZoneVisual.SetActive(showSlowMoVisual);
        Vector3 visualScale = slowMoZoneCollider != null ? slowMoZoneCollider.size : slowMoZoneSize;
        visualScale.y = slowMoVisualHeight;
        slowMoZoneVisual.transform.localScale = visualScale;
        slowMoZoneVisual.transform.localPosition = new Vector3(0f, -slowMoZoneLocalOffset.y + slowMoVisualHeight * 0.5f + slowMoVisualYOffset, 0f);

        if (slowMoZoneMaterial != null)
        {
            if (slowMoZoneMaterial.HasProperty("_Color"))
            {
                slowMoZoneMaterial.SetColor("_Color", slowMoVisualColor);
            }
            if (slowMoZoneMaterial.HasProperty("_BaseColor"))
            {
                slowMoZoneMaterial.SetColor("_BaseColor", slowMoVisualColor);
            }
        }
    }

    private static float RandomRange(System.Random random, Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        float a = Mathf.Min(min, max);
        float b = Mathf.Max(min, max);
        return Mathf.Lerp(a, b, (float)random.NextDouble());
    }

    private List<Vector3> GetSortedLaneLocalPositions()
    {
        List<Vector3> sorted = new List<Vector3>(runtimeLaneLocalPositions);
        sorted.Sort((a, b) => a.x.CompareTo(b.x));
        return sorted;
    }

    private bool TryGetSegmentLocalZRange(out float minZ, out float maxZ)
    {
        minZ = float.MaxValue;
        maxZ = float.MinValue;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
            {
                continue;
            }

            string n = r.gameObject.name;
            if (n == "SlowMoVisual" || n.StartsWith("Obstacle_Runtime") || n.StartsWith("SideBuilding_Runtime"))
            {
                continue;
            }

            Bounds b = r.bounds;
            Vector3 localMin = transform.InverseTransformPoint(b.min);
            Vector3 localMax = transform.InverseTransformPoint(b.max);

            minZ = Mathf.Min(minZ, localMin.z, localMax.z);
            maxZ = Mathf.Max(maxZ, localMin.z, localMax.z);
        }

        if (minZ == float.MaxValue || maxZ == float.MinValue)
        {
            return false;
        }

        return maxZ > minZ;
    }
}
