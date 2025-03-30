using GeneralPathfinder;
using UnityEngine;

/// <summary>
/// A pathfinder for agents in the game, using a copy of the A star algorithm.
/// Goal: Attach this script to an agent and give them a goal position in the world, they will attempt to traverse it.
/// </summary>
public class AgentGridBasedPathfinder : MonoBehaviour
{
    private TerrainPathGenerator pathfinder;
    public AgentGridBasedPathfinder gridBasedPathfinderSettings;

    void Awake()
    {   
        pathfinder = new TerrainPathGenerator();
        //pathfinder.settings = gridBasedPathfinderSettings;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
