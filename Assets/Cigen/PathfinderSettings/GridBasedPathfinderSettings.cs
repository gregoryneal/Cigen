using UnityEngine;

[CreateAssetMenu(fileName = "New Pathfinder Settings", menuName = "Pathfinding/Realtime Based Pathfinder Settings", order = 1)]
public class RealtimePathfinding : PathfinderSettings
{
    [Space(10)]
    [Header("Agent settings")]
    /// <summary>
    /// How long in seconds between polling for the best path?
    /// </summary>
    public float pollTimer = .5f;
}
