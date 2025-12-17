using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    private SimpleMultiAgentGroup m_AgentGroup;
    private bool episodeActive = true;
    private float episodeStartTime;

    [Header("References")]
    [SerializeField]
    private Transform target;
    public Transform Target => target;

    [SerializeField]
    private Collider area;
    private Bounds areaBounds;

    [SerializeField]
    private GameObject roomParent;
    public List<Room> Rooms { get; private set; }

    public Room SelectedRoom { get; private set; }

    [SerializeField]
    private LayerMask obstacleLayer;

    [SerializeField]
    private List<PathfinderAgentV3> agents;

    [Header("Episode Settings")]
    [SerializeField]
    private float maxEpisodeDuration = 30f; // Maksimum episode süresi

    // Public properties for access
    public Collider Area => area;
    public LayerMask ObstacleLayer => obstacleLayer;
    public bool EpisodeActive => episodeActive;

    private void Awake()
    {
        m_AgentGroup = new SimpleMultiAgentGroup();

        if (area != null)
        {
            areaBounds = area.bounds;
        }
        else
        {
            Debug.LogError("Area is not assigned in EnvironmentManager!");
        }

        if (roomParent != null)
        {
            Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
        }
        else
        {
            Rooms = new List<Room>();
            Debug.LogError("RoomParent is not assigned in EnvironmentManager!");
        }
    }

    private void Start()
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogError("No agents assigned to EnvironmentManager!");
            return;
        }

        foreach (var agent in agents)
        {
            if (agent != null)
            {
                m_AgentGroup.RegisterAgent(agent);
                Debug.Log($"Registered agent: {agent.name}");
            }
        }

        ResetScene();
    }

    private void Update()
    {
        // Episode süresi kontrolü
        if (episodeActive && Time.time - episodeStartTime > maxEpisodeDuration)
        {
            Debug.Log($"Episode timeout after {maxEpisodeDuration} seconds");
            EndEpisodeWithPenalty(-0.5f, "Timeout");
        }
    }

    public void ResetScene()
    {
        episodeActive = true;
        episodeStartTime = Time.time;

        // Her reset'te bounds'ý güncelle
        if (area != null)
        {
            areaBounds = area.bounds;
        }

        InitializeRooms();
        SelectRoom();
        SetTargetRandomPosition();

        foreach (var agent in agents)
        {
            if (agent != null)
            {
                agent.OnGroupEpisodeBegin();
            }
        }

        Debug.Log("Scene reset complete.");
    }

    public void NotifyTargetFound(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;

        Debug.Log($"Target found by {agent.name}");
        episodeActive = false;
        m_AgentGroup.AddGroupReward(agent.reachTargetReward);
        Debug.Log($"Group reward added: {agent.reachTargetReward}");
        m_AgentGroup.EndGroupEpisode();
        Invoke("ResetScene", 0.1f);
    }

    public void NotifyWallHit(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;

        Debug.Log($"{agent.name} hit a wall! Resetting scene...");
        episodeActive = false;
        m_AgentGroup.AddGroupReward(agent.hitWallPenalty);
        Debug.Log($"Group penalty added: {agent.hitWallPenalty}");
        m_AgentGroup.EndGroupEpisode();
        Invoke("ResetScene", 0.1f);
    }

    public void NotifyStuckInWall(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;

        Debug.Log($"{agent.name} is stuck in wall! Resetting scene...");
        episodeActive = false;
        float stuckPenalty = agent.hitWallPenalty * 0.5f;
        m_AgentGroup.AddGroupReward(stuckPenalty);
        Debug.Log($"Stuck penalty added: {stuckPenalty}");
        m_AgentGroup.EndGroupEpisode();
        Invoke("ResetScene", 0.1f);
    }

    private void EndEpisodeWithPenalty(float penalty, string reason)
    {
        if (!episodeActive) return;

        Debug.Log($"Ending episode: {reason}");
        episodeActive = false;
        m_AgentGroup.AddGroupReward(penalty);
        m_AgentGroup.EndGroupEpisode();
        Invoke("ResetScene", 0.1f);
    }

    public void NotifyNewRoomExplored(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;

        m_AgentGroup.AddGroupReward(agent.discoverNewRoomReward);
    }

    public void InitializeRooms()
    {
        foreach (var room in Rooms)
        {
            room.Initialize();
        }
    }

    public void SelectRoom()
    {
        if (Rooms != null && Rooms.Count > 0)
        {
            SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
        }
    }

    public void SetTargetRandomPosition()
    {
        if (SelectedRoom != null && target != null)
        {
            Vector3 roomCenter = SelectedRoom.transform.position;
            Vector3 randomPosition = roomCenter + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
            target.position = randomPosition;
        }
    }

    public Vector3 GetRandomAgentPosition()
    {
        if (area == null)
        {
            Debug.LogError("Area is null in GetRandomAgentPosition!");
            return Vector3.zero;
        }

        areaBounds = area.bounds;

        Vector3 randomSpawnPos;
        int maxAttempts = 100;

        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            // Duvarlardan uzak, alanýn iç kýsmýnda spawn noktasý
            float padding = 2f;
            float randomX = Random.Range(areaBounds.min.x + padding, areaBounds.max.x - padding);
            float randomZ = Random.Range(areaBounds.min.z + padding, areaBounds.max.z - padding);

            randomSpawnPos = new Vector3(randomX, areaBounds.center.y + 1f, randomZ);

            // Geniþ bir alanda engel kontrolü
            if (!Physics.CheckSphere(randomSpawnPos, 1.5f, obstacleLayer))
            {
                return randomSpawnPos;
            }
        }

        return areaBounds.center + Vector3.up * 2f;
    }

    public Quaternion GetRandomAgentRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0, 360), 0f);
    }

    [ContextMenu("Test Spawn Positions")]
    public void TestSpawnPositions()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = GetRandomAgentPosition();
            Debug.Log($"Test Spawn {i}: {pos}");
        }
    }
}