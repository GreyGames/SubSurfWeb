using System;
using System.Collections.Generic;
using UnityEngine;

public class SideEnvironmentSpawner : MonoBehaviour
{
    [Serializable]
    private struct WeightedBuildingPrefab
    {
        public GameObject prefab;
        [Min(0f)] public float weight;
    }

    [Header("References")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private bool autoFindLooper = true;
    [Tooltip("Optional explicit lane centers in world space (Left -> Mid -> Right).")]
    [SerializeField] private Transform[] laneCenters;
    [SerializeField] private bool autoDetectLaneBoundsFromTrack = true;

    [Header("Pool")]
    [SerializeField] private int totalBuildings = 28;
    [SerializeField] private Vector2 recycleSpacingRange = new Vector2(4.5f, 10f);
    [SerializeField] private float initialExtraAheadDistance = 30f;

    [Header("Placement")]
    [SerializeField] private Vector2 sideDistanceFromLaneRange = new Vector2(3.5f, 8f);
    [SerializeField] private Vector2 sideWidthRange = new Vector2(2f, 6f);
    [SerializeField] private Vector2 sideDepthRange = new Vector2(2f, 6f);
    [SerializeField] private Vector2 sideHeightRange = new Vector2(6f, 22f);
    [SerializeField] private float sideGroundOffsetY = 0f;
    [SerializeField] private bool sideRandomYaw = true;
    [SerializeField] private bool useSymmetricSides = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] sideBuildingPrefabs;
    [SerializeField] private bool useWeightedSidePrefabs = false;
    [SerializeField] private WeightedBuildingPrefab[] weightedSideBuildingPrefabs;

    [Header("Despawn")]
    [SerializeField] private float despawnPassedDistance = 0f;

    private readonly List<Transform> runtimeBuildings = new List<Transform>(64);
    private System.Random rng;
    private float laneLeftX = -1.5f;
    private float laneRightX = 1.5f;

    private void Awake()
    {
        if (autoFindLooper && looper == null)
        {
            looper = GetComponent<EndlessTrackLooper>();
            if (looper == null)
            {
                looper = FindObjectOfType<EndlessTrackLooper>();
            }
        }

        rng = new System.Random();
    }

    private void Start()
    {
        ResolveLaneBounds();
        Rebuild();
    }

    private void Update()
    {
        if (looper == null || looper.StartPoint == null || looper.EndPoint == null)
        {
            return;
        }

        if (runtimeBuildings.Count == 0)
        {
            return;
        }

        Vector3 frameMove = looper.MoveAxis * looper.CurrentSpeed * Time.deltaTime;
        for (int i = 0; i < runtimeBuildings.Count; i++)
        {
            Transform t = runtimeBuildings[i];
            if (t != null)
            {
                t.position += frameMove;
            }
        }

        RecyclePassedBuildings();
    }

    [ContextMenu("Rebuild Side Buildings")]
    public void Rebuild()
    {
        ClearRuntimeBuildings();
        EnsurePool(Mathf.Max(0, totalBuildings));
        ScatterAhead();
    }

    private void ResolveLaneBounds()
    {
        bool foundAny = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        if (laneCenters != null && laneCenters.Length > 0)
        {
            for (int i = 0; i < laneCenters.Length; i++)
            {
                if (laneCenters[i] == null)
                {
                    continue;
                }

                float x = laneCenters[i].position.x;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                foundAny = true;
            }
        }

        if (!foundAny && autoDetectLaneBoundsFromTrack && looper != null && looper.Segments.Count > 0)
        {
            Transform sampleSegment = looper.Segments[0];
            if (sampleSegment != null)
            {
                List<Renderer> renderers = new List<Renderer>(sampleSegment.GetComponentsInChildren<Renderer>(true));
                renderers.Sort((a, b) => a.bounds.center.x.CompareTo(b.bounds.center.x));
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null)
                    {
                        continue;
                    }

                    string n = r.gameObject.name;
                    if (n.StartsWith("SideBuilding_Runtime") || n == "Obstacle_Runtime")
                    {
                        continue;
                    }

                    float x = r.bounds.center.x;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    foundAny = true;
                }
            }
        }

        if (foundAny)
        {
            laneLeftX = minX;
            laneRightX = maxX;
        }
    }

    private void EnsurePool(int targetCount)
    {
        while (runtimeBuildings.Count < targetCount)
        {
            GameObject instance = CreateBuildingInstance();
            instance.name = $"SideBuilding_Runtime_{runtimeBuildings.Count}";
            instance.transform.SetParent(transform, true);
            runtimeBuildings.Add(instance.transform);
        }
    }

    private void ScatterAhead()
    {
        if (looper == null || looper.StartPoint == null)
        {
            return;
        }

        float startProjection = Vector3.Dot(looper.StartPoint.position, looper.SpawnAxis) + initialExtraAheadDistance;
        float projection = startProjection;

        for (int i = 0; i < runtimeBuildings.Count; i++)
        {
            projection += RandomRange(rng, recycleSpacingRange);
            PlaceBuildingAtProjection(runtimeBuildings[i], projection, i);
        }
    }

    private void RecyclePassedBuildings()
    {
        float farthestProjection = GetFarthestProjection();
        for (int i = 0; i < runtimeBuildings.Count; i++)
        {
            Transform t = runtimeBuildings[i];
            if (t == null)
            {
                continue;
            }

            float passedDistance = Vector3.Dot(t.position - looper.EndPoint.position, looper.MoveAxis);
            if (passedDistance <= despawnPassedDistance)
            {
                continue;
            }

            farthestProjection += RandomRange(rng, recycleSpacingRange);
            PlaceBuildingAtProjection(t, farthestProjection, i);
        }
    }

    private float GetFarthestProjection()
    {
        float best = float.MinValue;
        for (int i = 0; i < runtimeBuildings.Count; i++)
        {
            Transform t = runtimeBuildings[i];
            if (t == null)
            {
                continue;
            }

            float p = Vector3.Dot(t.position, looper.SpawnAxis);
            if (p > best)
            {
                best = p;
            }
        }

        if (best == float.MinValue)
        {
            best = Vector3.Dot(looper.StartPoint.position, looper.SpawnAxis);
        }

        return best;
    }

    private void PlaceBuildingAtProjection(Transform t, float projection, int index)
    {
        if (t == null || looper == null || looper.StartPoint == null)
        {
            return;
        }

        float startProjection = Vector3.Dot(looper.StartPoint.position, looper.SpawnAxis);
        Vector3 axisPoint = looper.StartPoint.position + looper.SpawnAxis * (projection - startProjection);

        float sideSign;
        if (useSymmetricSides)
        {
            sideSign = (index & 1) == 0 ? -1f : 1f;
        }
        else
        {
            sideSign = rng.Next(2) == 0 ? -1f : 1f;
        }

        float anchorX = sideSign < 0f ? laneLeftX : laneRightX;
        float lateralOffset = RandomRange(rng, sideDistanceFromLaneRange);

        float width = RandomRange(rng, sideWidthRange);
        float depth = RandomRange(rng, sideDepthRange);
        float height = RandomRange(rng, sideHeightRange);

        Vector3 pos = axisPoint;
        pos.x = anchorX + sideSign * lateralOffset;
        pos.y = sideGroundOffsetY + (height * 0.5f);

        t.position = pos;
        t.localScale = new Vector3(width, height, depth);

        if (sideRandomYaw)
        {
            t.rotation = Quaternion.Euler(0f, RandomRange(rng, 0f, 360f), 0f);
        }
        else
        {
            t.rotation = Quaternion.identity;
        }

        if (!t.gameObject.activeSelf)
        {
            t.gameObject.SetActive(true);
        }
    }

    private GameObject CreateBuildingInstance()
    {
        GameObject selected = GetRandomSideBuildingPrefab();
        if (selected != null)
        {
            return Instantiate(selected);
        }

        return GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    private GameObject GetRandomSideBuildingPrefab()
    {
        if (useWeightedSidePrefabs && weightedSideBuildingPrefabs != null && weightedSideBuildingPrefabs.Length > 0)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weightedSideBuildingPrefabs.Length; i++)
            {
                if (weightedSideBuildingPrefabs[i].prefab != null)
                {
                    totalWeight += Mathf.Max(0f, weightedSideBuildingPrefabs[i].weight);
                }
            }

            if (totalWeight > 0f)
            {
                float pick = (float)rng.NextDouble() * totalWeight;
                float accum = 0f;
                for (int i = 0; i < weightedSideBuildingPrefabs.Length; i++)
                {
                    WeightedBuildingPrefab entry = weightedSideBuildingPrefabs[i];
                    if (entry.prefab == null)
                    {
                        continue;
                    }

                    accum += Mathf.Max(0f, entry.weight);
                    if (pick <= accum)
                    {
                        return entry.prefab;
                    }
                }
            }
        }

        if (sideBuildingPrefabs == null || sideBuildingPrefabs.Length == 0)
        {
            return null;
        }

        int start = rng.Next(sideBuildingPrefabs.Length);
        for (int i = 0; i < sideBuildingPrefabs.Length; i++)
        {
            int idx = (start + i) % sideBuildingPrefabs.Length;
            if (sideBuildingPrefabs[idx] != null)
            {
                return sideBuildingPrefabs[idx];
            }
        }

        return null;
    }

    private void ClearRuntimeBuildings()
    {
        for (int i = 0; i < runtimeBuildings.Count; i++)
        {
            Transform t = runtimeBuildings[i];
            if (t != null)
            {
                Destroy(t.gameObject);
            }
        }

        runtimeBuildings.Clear();
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
}
