using System;
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
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSpawnChance = 0.7f;
    [SerializeField] private Vector3 obstacleLocalOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private bool obstacleZeroRotation = true;

    private GameObject pooledObstacle;

    public void OnRecycled(System.Random rng)
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

        RandomizeObstacle(rng);
    }

    private void RandomizeObstacle(System.Random rng)
    {
        if (obstaclePrefab == null || obstacleLaneCenters == null || obstacleLaneCenters.Length == 0)
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
            pooledObstacle = Instantiate(obstaclePrefab, transform);
            pooledObstacle.name = "Obstacle_Runtime";
        }

        int laneIndex = rng.Next(obstacleLaneCenters.Length);
        Transform lane = obstacleLaneCenters[laneIndex];
        if (lane == null)
        {
            SetObstacleActive(false);
            return;
        }

        pooledObstacle.SetActive(true);
        pooledObstacle.transform.localPosition = lane.localPosition + obstacleLocalOffset;
        pooledObstacle.transform.localRotation = obstacleZeroRotation ? Quaternion.identity : lane.localRotation;
    }

    private void SetObstacleActive(bool state)
    {
        if (pooledObstacle != null)
        {
            pooledObstacle.SetActive(state);
        }
    }
}
