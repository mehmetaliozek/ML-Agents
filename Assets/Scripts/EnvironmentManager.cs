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
    private float maxEpisodeDuration = 75f;

    // Debug görselleþtirmeleri (Gizmos) için ayar
    [Header("Debug Settings")]
    [SerializeField] private bool showGizmos = true;
    // showGUI deðiþkenini ve OnGUI metodunu kaldýrdýk

    // Public properties
    public Collider Area => area;
    public LayerMask ObstacleLayer => obstacleLayer;
    public bool EpisodeActive => episodeActive;

    private void Awake()
    {
        m_AgentGroup = new SimpleMultiAgentGroup();

        // Otomatik Ajan Bulma
        agents = new List<PathfinderAgentV3>(GetComponentsInChildren<PathfinderAgentV3>());

        if (agents.Count == 0)
            Debug.LogError($"{gameObject.name}: Ajan bulunamadý! Ajanlarýn bu objenin altýnda (child) olduðundan emin ol.");

        if (roomParent != null)
            Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
        else
        {
            Rooms = new List<Room>();
            Debug.LogError("RoomParent atanmamýþ!");
        }
    }

    private void Start()
    {
        foreach (var agent in agents)
        {
            if (agent != null)
            {
                agent.SetEnvironmentManager(this);
                m_AgentGroup.RegisterAgent(agent);
            }
        }
        ResetScene("Initial Start");
    }

    private void FixedUpdate()
    {
        if (episodeActive && Time.time - episodeStartTime > maxEpisodeDuration)
        {
            EndEpisodeWithPenalty(-0.5f, "Zaman Aþýmý (Timeout)");
        }
    }

    public void ResetScene(string reason = "")
    {
        episodeActive = true;
        episodeStartTime = Time.time;

        if (area != null) areaBounds = area.bounds;

        InitializeRooms();
        SelectRoom();
        SetTargetRandomPosition();

        foreach (var agent in agents)
        {
            if (agent != null) agent.OnGroupEpisodeBegin();
        }

        Debug.Log($"[DEBUG] [{gameObject.name}] Sahne resetlendi. Sebep: {reason}");
    }

    public void NotifyTargetFound(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;

        episodeActive = false;
        m_AgentGroup.AddGroupReward(agent.reachTargetReward);
        m_AgentGroup.EndGroupEpisode();

        Debug.Log($"[DEBUG] [{gameObject.name}] Hedef bulundu. Ödül: {agent.reachTargetReward} - Ajan: {agent.name}");

        ResetScene($"Hedef Bulundu: {agent.name}");
    }

    public void NotifyWallHit(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;
        // Duvara çarpma mantýðý ajan içinde hallediliyor
    }

    private void EndEpisodeWithPenalty(float penalty, string reason)
    {
        if (!episodeActive) return;

        episodeActive = false;
        m_AgentGroup.AddGroupReward(penalty);
        m_AgentGroup.EndGroupEpisode();

        Debug.Log($"[DEBUG] [{gameObject.name}] Episode penalty ile sonlandý. Ceza: {penalty} - Sebep: {reason}");

        ResetScene(reason);
    }

    public void NotifyNewRoomExplored(PathfinderAgentV3 agent)
    {
        if (!episodeActive) return;
        m_AgentGroup.AddGroupReward(agent.discoverNewRoomReward);
    }

    public void InitializeRooms()
    {
        foreach (var room in Rooms) room.Initialize();
    }

    public void SelectRoom()
    {
        if (Rooms != null && Rooms.Count > 0)
            SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
    }

    public void SetTargetRandomPosition()
    {
        if (SelectedRoom != null && target != null)
        {
            Vector3 roomCenter = SelectedRoom.transform.position;
            Vector3 randomPosition = roomCenter + new Vector3(Random.Range(-1.5f, 1.5f), 0.5f, Random.Range(-1.5f, 1.5f));
            target.position = randomPosition;
        }
    }

    public Vector3 GetRandomAgentPosition()
    {
        if (area == null) return Vector3.zero;

        areaBounds = area.bounds;

        Vector3 randomSpawnPos;
        int maxAttempts = 50;

        for (int i = 0; i < maxAttempts; i++)
        {
            float padding = 2f;
            float randomX = Random.Range(areaBounds.min.x + padding, areaBounds.max.x - padding);
            float randomZ = Random.Range(areaBounds.min.z + padding, areaBounds.max.z - padding);

            randomSpawnPos = new Vector3(randomX, areaBounds.center.y + 1f, randomZ);

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
}