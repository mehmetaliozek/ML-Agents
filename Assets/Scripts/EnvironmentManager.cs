using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents; //Multi-Agent Learning için gerekli

public class EnvironmentManager : MonoBehaviour
{

    private SimpleMultiAgentGroup m_AgentGroup;

    [SerializeField]
    private Transform target;
    [SerializeField]
    private Collider area;
    [SerializeField]
    private GameObject roomParent;
    [SerializeField]
    private LayerMask obstacleLayer;

    public Transform Target => target;
    public List<Room> Rooms { get; private set; }
    public Room SelectedRoom { get; private set; }

    private Bounds areaBounds;
    private const int MAX_SPAWN_ATTEMPTS = 100;

    [SerializeField] 
    private List<PathfinderAgentV3> agents; //ajanlarý multiAgent grubuna eklemek için editörden atama yapcaz

    private void Awake()
    {
        m_AgentGroup = new SimpleMultiAgentGroup(); //Multiagent grubunu oluþturuyorum

        areaBounds = area.bounds;
        Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
            
        InitializeRooms();
    }

    private void Start()
    {
        foreach (var agent in agents)
        {
            m_AgentGroup.RegisterAgent(agent); //ajanlarý multiAgent grubuna ekliyorum
        }

        ResetScene(); //Sahneyi baþlatýrken resetleme-kurulum için
    }

    public void ResetScene()
    {
        InitializeRooms();
        //SelectRoom();
        SetTargetRandomPosition();

        foreach(var agent in agents)
        {
            agent.OnEpisodeBegin(); //Her ajan için episode baþlangýcý, senkronizasyon sorunu yaþamamak için
        }
    }

    public void NotifyTargetFound(PathfinderAgentV3 agent)
    {
        m_AgentGroup.AddGroupReward(agent.reachTargetReward); //Tüm ajanlara ödül ver
        m_AgentGroup.EndGroupEpisode(); //Tüm ajanlarýn bölümünü sonlandýr
        ResetScene(); //Sahneyi sýfýrla
    }

    public void NotifyNewRoomExplored(PathfinderAgentV3 agent)
    {
        m_AgentGroup.AddGroupReward(agent.discoverNewRoomReward);
    }

    public void InitializeRooms()
    {
        foreach (var room in Rooms)
        {
            room.Initialize();
        }
    }

    public void ResetEnvironment()
    {
        InitializeRooms();
        SelectRandomRoom();
        SetTargetRandomPosition();
    }

    public void SelectRandomRoom()
    {
        if (Rooms.Count == 0) return;
        SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
    }

    public void SetTargetRandomPosition()
    {
        if (SelectedRoom == null) return;

        Vector3 roomCenter = SelectedRoom.transform.localPosition;
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        target.localPosition = roomCenter + randomOffset;
    }

    public Vector3 GetRandomAgentPosition()
    {
        Vector3 randomSpawnPos;
        int attempts = 0;

        while (attempts < MAX_SPAWN_ATTEMPTS)
        {
            attempts++;
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);

            randomSpawnPos = area.transform.localPosition + new Vector3(randomPosX, 1f, randomPosZ);

            if (!Physics.CheckSphere(randomSpawnPos, 1.0f, obstacleLayer))
            {
                return randomSpawnPos;
            }
        }

        Debug.LogWarning("Uygun spawn noktasý bulunamadý, varsayýlan nokta kullanýlýyor.");
        return area.transform.localPosition + Vector3.up;
    }

    public Quaternion GetRandomAgentRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }
}