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

    [Header("Coin Line")]
    [SerializeField] private bool enableCoinLine = true;
    [Range(0f, 1f)]
    [SerializeField] private float coinLineChance = 0.4f;
    [SerializeField] private int coinMinSegmentsBetween = 1;
    [SerializeField] private Vector2Int coinCountRange = new Vector2Int(6, 10);
    [SerializeField] private float coinSpacing = 1.2f;
    [SerializeField] private Vector2 coinForwardRange = new Vector2(3f, 10f);
    [SerializeField] private float coinEdgePadding = 2f;
    [SerializeField] private bool coinPreferFrontHalf = false;
    [SerializeField] private bool coinPreferNearHalf = true;
    [SerializeField] private float coinAvoidObstacleDistance = 3f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Vector3 coinLocalOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 coinLocalScale = new Vector3(0.5f, 0.15f, 0.5f);
    [SerializeField] private bool coinZeroRotation = true;
    [SerializeField] private int coinValue = 1;

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
    [SerializeField] private bool slowMoIncludeCenterLane = false;
    [SerializeField] private float slowMoCenterOrbitDirection = 1f;
    [SerializeField] private bool showSlowMoVisual = true;
    [SerializeField] private Color slowMoVisualColor = new Color(0.2f, 0.85f, 1f, 0.5f);
    [SerializeField] private float slowMoVisualHeight = 0.06f;
    [SerializeField] private float slowMoVisualYOffset = 0.01f;
    [SerializeField] private Material slowMoVisualMaterial;
    [Header("SlowMo Marker")]
    [SerializeField] private bool showSlowMoMarker = true;
    [SerializeField] private GameObject slowMoMarkerPrefab;
    [SerializeField] private string slowMoMarkerText = "x2";
    [SerializeField] private Vector3 slowMoMarkerLocalOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private float slowMoMarkerFontSize = 90f;
    [SerializeField] private float slowMoMarkerCharacterSize = 0.06f;
    [SerializeField] private Color slowMoMarkerColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float slowMoMarkerSpinDegreesPerSecond = 90f;
    [SerializeField] private float slowMoMarkerFloatAmplitude = 0.15f;
    [SerializeField] private float slowMoMarkerFloatSpeed = 2f;
    [SerializeField] private bool useNamedLaneCentersForSlowMo = true;
    [SerializeField] private string slowMoLeftLaneName = "CenterLeft";
    [SerializeField] private string slowMoRightLaneName = "CenterRight";

    private GameObject pooledObstacle;
    private readonly List<Vector3> runtimeLaneLocalPositions = new List<Vector3>(3);
    private GameObject slowMoZoneObject;
    private BoxCollider slowMoZoneCollider;
    private SlowMoZone slowMoZone;
    private GameObject slowMoZoneVisual;
    private MeshRenderer slowMoZoneRenderer;
    private Material slowMoZoneMaterial;
    private GameObject slowMoMarkerObject;
    private TextMesh slowMoMarkerTextMesh;
    private SlowMoMarkerFloatSpin slowMoMarkerSpin;
    private static int segmentsSinceSlowMo = 1000;
    private static int segmentsSinceCoinLine = 1000;
    private readonly List<CoinPickup> pooledCoins = new List<CoinPickup>(12);
    private static Transform cachedSlowMoLeftLane;
    private static Transform cachedSlowMoRightLane;

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
        UpdateCoinLine(rng, worldSpawnAxis);
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
            if (child == null ||
                child.name == "Obstacle_Runtime" ||
                child.name == "Coin_Runtime" ||
                child.name.StartsWith("SideBuilding_Runtime") ||
                child.name.StartsWith("SlowMo"))
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

        if (useNamedLaneCentersForSlowMo && TryGetNamedSlowMoLaneCenters(out float leftX, out float rightX))
        {
            laneX = rng.Next(2) == 0 ? leftX : rightX;
            cameraSide = Mathf.Approximately(laneX, leftX) ? -1f : 1f;
        }
        else
        {
            float minX = sortedLanes[0].x;
            float maxX = sortedLanes[0].x;
            for (int i = 1; i < sortedLanes.Count; i++)
            {
                float x = sortedLanes[i].x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            laneX = rng.Next(2) == 0 ? minX : maxX;
            cameraSide = laneX < 0f ? -1f : 1f;
        }

        Vector3 zoneSize = slowMoZoneSize;
        if (autoSizeSlowMoToLane)
        {
            float laneWidth = 0f;
            if (useNamedLaneCentersForSlowMo && TryGetNamedSlowMoLaneCenters(out float namedLeftX, out float namedRightX))
            {
                laneWidth = Mathf.Abs(namedRightX - namedLeftX) * 0.5f;
            }
            else if (sortedLanes.Count >= 2)
            {
                laneWidth = Mathf.Abs(sortedLanes[1].x - sortedLanes[0].x);
            }

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

    private void UpdateCoinLine(System.Random rng, Vector3 worldSpawnAxis)
    {
        if (!enableCoinLine)
        {
            DisableAllCoins();
            return;
        }

        segmentsSinceCoinLine++;
        if (segmentsSinceCoinLine < Mathf.Max(0, coinMinSegmentsBetween))
        {
            DisableAllCoins();
            return;
        }

        bool shouldSpawn = rng.NextDouble() <= coinLineChance;
        if (!shouldSpawn)
        {
            DisableAllCoins();
            return;
        }

        segmentsSinceCoinLine = 0;

        if (!TryBuildLaneCenters())
        {
            DisableAllCoins();
            return;
        }

        List<Vector3> sortedLanes = GetSortedLaneLocalPositions();
        if (sortedLanes.Count == 0)
        {
            DisableAllCoins();
            return;
        }

        int minCount = Mathf.Max(1, Mathf.Min(coinCountRange.x, coinCountRange.y));
        int maxCount = Mathf.Max(minCount, Mathf.Max(coinCountRange.x, coinCountRange.y));
        int coinCount = rng.Next(minCount, maxCount + 1);
        EnsureCoinPool(coinCount);

        Vector3 localSpawnDirection = Vector3.forward;

        float startMin = Mathf.Min(coinForwardRange.x, coinForwardRange.y);
        float startMax = Mathf.Max(coinForwardRange.x, coinForwardRange.y);
        float start = RandomRange(rng, coinForwardRange);
        bool hasRange = TryGetSegmentLocalZRange(out float minZ, out float maxZ);
        float totalLength = (coinCount - 1) * coinSpacing;

        if (hasRange)
        {
            float mid = (minZ + maxZ) * 0.5f;
            if (coinPreferNearHalf)
            {
                float nearStartMin = minZ + coinEdgePadding + startMin;
                float nearStartMax = minZ + coinEdgePadding + startMax - totalLength;
                if (nearStartMax > nearStartMin)
                {
                    startMin = nearStartMin;
                    startMax = nearStartMax;
                }
            }
            else if (coinPreferFrontHalf)
            {
                float padMin = minZ + coinEdgePadding;
                float padMax = maxZ - coinEdgePadding - totalLength;
                if (padMax > padMin)
                {
                    startMin = padMin;
                    startMax = padMax;
                }

                float frontMin = startMin;
                float frontMax = startMax;
                frontMin = Mathf.Max(startMin, mid);

                if (frontMax > frontMin)
                {
                    startMin = frontMin;
                    startMax = frontMax;
                }
            }
            else
            {
                float padMin = minZ + coinEdgePadding;
                float padMax = maxZ - coinEdgePadding - totalLength;
                if (padMax > padMin)
                {
                    startMin = padMin;
                    startMax = padMax;
                }
            }
        }

        if (startMax <= startMin)
        {
            DisableAllCoins();
            return;
        }

        start = RandomRange(rng, startMin, startMax);

        int laneIndex = rng.Next(sortedLanes.Count);
        Vector3 laneLocalPos = sortedLanes[laneIndex];

        if (pooledObstacle != null && pooledObstacle.activeSelf && coinAvoidObstacleDistance > 0f)
        {
            Vector3 obstacleLocal = pooledObstacle.transform.localPosition;
            int obstacleLane = GetClosestLaneIndex(obstacleLocal.x, sortedLanes);
            if (laneIndex == obstacleLane)
            {
                float lineMid = start + totalLength * 0.5f;
                if (Mathf.Abs(lineMid - obstacleLocal.z) < coinAvoidObstacleDistance)
                {
                    if (sortedLanes.Count > 1)
                    {
                        laneIndex = obstacleLane == 0 ? sortedLanes.Count - 1 : 0;
                        laneLocalPos = sortedLanes[laneIndex];
                    }
                    else
                    {
                        float shift = Mathf.Sign(localSpawnDirection.z) * coinAvoidObstacleDistance;
                        float shifted = start + shift;
                        if (shifted >= startMin && shifted <= startMax)
                        {
                            start = shifted;
                        }
                        else
                        {
                            DisableAllCoins();
                            return;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < pooledCoins.Count; i++)
        {
            CoinPickup coin = pooledCoins[i];
            if (coin == null)
            {
                continue;
            }

            bool active = i < coinCount;
            GameObject coinObj = coin.gameObject;
            if (!active)
            {
                coinObj.SetActive(false);
                continue;
            }

            Vector3 localPos = laneLocalPos + coinLocalOffset + localSpawnDirection * (start + i * coinSpacing);
            coinObj.transform.localPosition = localPos;
            if (coinZeroRotation)
            {
                coinObj.transform.localRotation = Quaternion.identity;
            }
            coinObj.transform.localScale = coinLocalScale;
            coin.SetValue(coinValue);
            coinObj.SetActive(true);
        }
    }

    private void EnsureCoinPool(int count)
    {
        if (pooledCoins.Count >= count)
        {
            return;
        }

        while (pooledCoins.Count < count)
        {
            CoinPickup coin = CreateCoinInstance();
            if (coin == null)
            {
                break;
            }

            pooledCoins.Add(coin);
        }
    }

    private CoinPickup CreateCoinInstance()
    {
        GameObject instance;
        if (coinPrefab != null)
        {
            instance = Instantiate(coinPrefab, transform);
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            instance.transform.SetParent(transform, false);
        }

        instance.name = "Coin_Runtime";
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        CoinPickup pickup = instance.GetComponent<CoinPickup>();
        if (pickup == null)
        {
            pickup = instance.AddComponent<CoinPickup>();
        }

        return pickup;
    }

    private void DisableAllCoins()
    {
        for (int i = 0; i < pooledCoins.Count; i++)
        {
            CoinPickup coin = pooledCoins[i];
            if (coin == null)
            {
                continue;
            }

            coin.gameObject.SetActive(false);
        }
    }

    private static int GetClosestLaneIndex(float x, List<Vector3> lanes)
    {
        int bestIndex = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < lanes.Count; i++)
        {
            float d = Mathf.Abs(lanes[i].x - x);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestIndex;
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
        EnsureSlowMoMarker();
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

        if (slowMoVisualMaterial != null)
        {
            slowMoZoneMaterial = slowMoVisualMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                slowMoZoneMaterial = new Material(shader);
            }
            else
            {
                slowMoZoneMaterial = new Material(Shader.Find("Standard"));
            }
        }

        slowMoZoneRenderer.sharedMaterial = slowMoZoneMaterial;
        UpdateSlowMoVisual();
    }

    private void EnsureSlowMoMarker()
    {
        if (slowMoMarkerObject != null)
        {
            return;
        }

        if (slowMoMarkerPrefab != null)
        {
            slowMoMarkerObject = Instantiate(slowMoMarkerPrefab, slowMoZoneObject.transform);
            slowMoMarkerObject.name = "SlowMoMarker";
        }
        else
        {
            slowMoMarkerObject = new GameObject("SlowMoMarker");
            slowMoMarkerObject.transform.SetParent(slowMoZoneObject.transform, false);

            slowMoMarkerTextMesh = slowMoMarkerObject.AddComponent<TextMesh>();
            slowMoMarkerTextMesh.text = slowMoMarkerText;
            slowMoMarkerTextMesh.anchor = TextAnchor.MiddleCenter;
            slowMoMarkerTextMesh.alignment = TextAlignment.Center;
            slowMoMarkerTextMesh.color = slowMoMarkerColor;
            slowMoMarkerTextMesh.fontSize = Mathf.RoundToInt(Mathf.Max(10f, slowMoMarkerFontSize));
            slowMoMarkerTextMesh.characterSize = Mathf.Max(0.001f, slowMoMarkerCharacterSize);
        }

        slowMoMarkerSpin = slowMoMarkerObject.GetComponent<SlowMoMarkerFloatSpin>();
        if (slowMoMarkerSpin == null)
        {
            slowMoMarkerSpin = slowMoMarkerObject.AddComponent<SlowMoMarkerFloatSpin>();
        }
        slowMoMarkerSpin.Configure(slowMoMarkerSpinDegreesPerSecond, slowMoMarkerFloatAmplitude, slowMoMarkerFloatSpeed);
        slowMoMarkerSpin.SetBaseLocalPosition(slowMoMarkerLocalOffset);
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

        if (slowMoMarkerObject != null)
        {
            slowMoMarkerObject.SetActive(showSlowMoMarker);
            if (slowMoMarkerSpin != null)
            {
                slowMoMarkerSpin.Configure(slowMoMarkerSpinDegreesPerSecond, slowMoMarkerFloatAmplitude, slowMoMarkerFloatSpeed);
                slowMoMarkerSpin.SetBaseLocalPosition(slowMoMarkerLocalOffset);
            }
            if (slowMoMarkerTextMesh != null)
            {
                slowMoMarkerTextMesh.text = slowMoMarkerText;
                slowMoMarkerTextMesh.color = slowMoMarkerColor;
                slowMoMarkerTextMesh.fontSize = Mathf.RoundToInt(Mathf.Max(10f, slowMoMarkerFontSize));
                slowMoMarkerTextMesh.characterSize = Mathf.Max(0.001f, slowMoMarkerCharacterSize);
            }
        }

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
            if (slowMoZoneMaterial.HasProperty("_EmissionColor"))
            {
                slowMoZoneMaterial.EnableKeyword("_EMISSION");
                slowMoZoneMaterial.SetColor("_EmissionColor", slowMoVisualColor);
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

    private bool TryGetNamedSlowMoLaneCenters(out float leftX, out float rightX)
    {
        leftX = 0f;
        rightX = 0f;

        if (cachedSlowMoLeftLane == null && !string.IsNullOrEmpty(slowMoLeftLaneName))
        {
            GameObject left = GameObject.Find(slowMoLeftLaneName);
            if (left != null)
            {
                cachedSlowMoLeftLane = left.transform;
            }
        }

        if (cachedSlowMoRightLane == null && !string.IsNullOrEmpty(slowMoRightLaneName))
        {
            GameObject right = GameObject.Find(slowMoRightLaneName);
            if (right != null)
            {
                cachedSlowMoRightLane = right.transform;
            }
        }

        if (cachedSlowMoLeftLane == null || cachedSlowMoRightLane == null)
        {
            return false;
        }

        Vector3 leftLocal = transform.InverseTransformPoint(cachedSlowMoLeftLane.position);
        Vector3 rightLocal = transform.InverseTransformPoint(cachedSlowMoRightLane.position);
        leftX = leftLocal.x;
        rightX = rightLocal.x;

        if (leftX > rightX)
        {
            float tmp = leftX;
            leftX = rightX;
            rightX = tmp;
        }

        return true;
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
            if (n == "SlowMoVisual" || n.StartsWith("Obstacle_Runtime") || n.StartsWith("Coin_Runtime") || n.StartsWith("SideBuilding_Runtime"))
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
