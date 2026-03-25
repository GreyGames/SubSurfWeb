using System.Collections.Generic;
using UnityEngine;

public class ObstacleMarker : MonoBehaviour
{
    private static readonly List<ObstacleMarker> ActiveMarkers = new List<ObstacleMarker>(64);

    public static IReadOnlyList<ObstacleMarker> Active => ActiveMarkers;

    private void OnEnable()
    {
        if (!ActiveMarkers.Contains(this))
        {
            ActiveMarkers.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveMarkers.Remove(this);
    }
}
