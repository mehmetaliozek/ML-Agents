using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    // ML-Agents Group Management
    private SimpleMultiAgentGroup m_AgentGroup;

    [Header("Environment Settings")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Collider area; // Spawn alaný sýnýrlarý için

    [SerializeField]
    private GameObject roomParent; // Odalarýn parent objesi

    [SerializeField]
    private LayerMask obstacleLayer; // Engel tespiti için layer

    [Header("Agents")]
    [SerializeField]
    private List<PathfinderAgentV3> agents;

    // Internal State
    private Bounds areaBounds;
    private const int MAX_SPAWN_ATTEMPTS = 100;

    // Public Properties
    public Collider Area => area;
    public LayerMask ObstacleLayer => obstacleLayer;
    public Transform Target => target;
    public List<Room> Rooms { get; private set; }
    public Room SelectedRoom { get; private set; }

    private void Awake()
    {
        m_AgentGroup = new SimpleMultiAgentGroup();

        // Initialize Bounds
        if (area != null)
        {
            areaBounds = area.bounds;
        }
        else
        {
            Debug.LogError("EnvironmentManager: 'Area' collider is not assigned!");
        }

        // Initialize Rooms
        if (roomParent != null)
        {
            Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
        }
        else
        {
            Rooms = new List<Room>();
            Debug.LogWarning("EnvironmentManager: 'RoomParent' is not assigned!");
        }

        InitializeRooms();
    }

    private void Start()
    {
        RegisterAgents();
        ResetScene();
    }

    private void RegisterAgents()
    {
        if (agents == null || agents.Count == 0) return;

        foreach (var agent in agents)
        {
            if (agent != null)
            {
                m_AgentGroup.RegisterAgent(agent);
            }
        }
    }

    public void ResetScene()
    {
        InitializeRooms();
        SelectRandomRoom();
        SetTargetRandomPosition();

        // Reset all agents
        if (agents != null)
        {
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    agent.OnGroupEpisodeBegin();
                }
            }
        }
    }

    public void NotifyTargetFound(PathfinderAgentV3 agent)
    {
        if (m_AgentGroup != null)
        {
            // Reward the group and end the episode
            m_AgentGroup.AddGroupReward(agent.reachTargetReward);
            m_AgentGroup.EndGroupEpisode();
        }
        ResetScene();
    }

    public void NotifyNewRoomExplored(PathfinderAgentV3 agent)
    {
        if (m_AgentGroup != null)
        {
            m_AgentGroup.AddGroupReward(agent.discoverNewRoomReward);
        }
    }

    private void InitializeRooms()
    {
        if (Rooms == null) return;

        foreach (var room in Rooms)
        {
            room.Initialize();
        }
    }

    private void SelectRandomRoom()
    {
        if (Rooms != null && Rooms.Count > 0)
        {
            SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
        }
    }

    private void SetTargetRandomPosition()
    {
        if (SelectedRoom == null || target == null) return;

        Vector3 roomCenter = SelectedRoom.transform.localPosition;
        // Ufak bir random offset ile hedefin yerini deðiþtiriyoruz
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        target.localPosition = roomCenter + randomOffset;
    }

    /// <summary>
    /// Finds a safe random position for an agent within the area bounds.
    /// </summary>
    public Vector3 GetRandomAgentPosition()
    {
        if (area == null)
        {
            Debug.LogError("Area is not assigned in EnvironmentManager!");
            return Vector3.zero;
        }

        // Refresh bounds in case the object moved
        areaBounds = area.bounds;

        Vector3 randomSpawnPos;
        int attempts = 0;

        while (attempts < MAX_SPAWN_ATTEMPTS)
        {
            attempts++;

            float randomX = Random.Range(areaBounds.min.x, areaBounds.max.x);
            float randomZ = Random.Range(areaBounds.min.z, areaBounds.max.z);

            // Y ekseni için area'nýn üst noktasýný baz alýyoruz + 1f yükseklik
            randomSpawnPos = new Vector3(randomX, areaBounds.center.y, randomZ);

            // Check collision with obstacles (radius 0.5f)
            if (!Physics.CheckSphere(randomSpawnPos, 0.5f, obstacleLayer))
            {
                return randomSpawnPos;
            }
        }

        Debug.LogWarning($"Could not find valid spawn position after {MAX_SPAWN_ATTEMPTS} attempts. Using Center.");
        return areaBounds.center + Vector3.up;
    }

    public Quaternion GetRandomAgentRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    #region Debugging Tools

    [ContextMenu("Print Spawn Info")]
    public void PrintSpawnInfo()
    {
        if (area == null)
        {
            Debug.LogError("Area is not assigned!");
            return;
        }

        Debug.Log($"Area: {area.name} | Center: {area.bounds.center} | Size: {area.bounds.size}");

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = GetRandomAgentPosition();
            bool isObstructed = Physics.CheckSphere(pos, 0.5f, obstacleLayer);
            Debug.Log($"Sample Spawn {i}: {pos} | Obstructed: {isObstructed}");
        }
    }

    private void OnDrawGizmos()
    {
        if (area != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
        }

        if (Application.isPlaying && agents != null)
        {
            Gizmos.color = Color.red;
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    Gizmos.DrawSphere(agent.transform.position, 0.5f);
                }
            }
        }
    }
    #endregion
}