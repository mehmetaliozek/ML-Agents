using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    private SimpleMultiAgentGroup m_AgentGroup;

    [Header("References")]
    [SerializeField]
    private Transform target;
    [SerializeField]
<<<<<<< Updated upstream
    private Collider area;
=======
    private Collider area;  // Bu satýrý ekleyin
    private Bounds areaBounds;

>>>>>>> Stashed changes
    [SerializeField]
    private GameObject roomParent;
    [SerializeField]
    private LayerMask obstacleLayer;  // Bu satýrý ekleyin

<<<<<<< Updated upstream
    public Transform Target => target;
    public List<Room> Rooms { get; private set; }
    public Room SelectedRoom { get; private set; }

    private Bounds areaBounds;
    private const int MAX_SPAWN_ATTEMPTS = 100;

    [SerializeField] 
    private List<PathfinderAgentV3> agents; //ajanlarý multiAgent grubuna eklemek için editörden atama yapcaz
=======
    [SerializeField]
    private List<PathfinderAgentV3> agents;

    // Public properties for external access
    public Collider Area => area;
    public LayerMask ObstacleLayer => obstacleLayer;
>>>>>>> Stashed changes

    private void Awake()
    {
        m_AgentGroup = new SimpleMultiAgentGroup();

<<<<<<< Updated upstream
        areaBounds = area.bounds;
        Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
            
        InitializeRooms();
=======
        if (area != null)
        {
            areaBounds = area.bounds;
        }

        if (roomParent != null)
        {
            Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
        }
        else
        {
            Rooms = new List<Room>();
        }
>>>>>>> Stashed changes
    }

    private void Start()
    {
        if (agents != null && agents.Count > 0)
        {
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    m_AgentGroup.RegisterAgent(agent);
                }
            }
        }

        ResetScene();
    }

    public void ResetScene()
    {
        InitializeRooms();
        //SelectRoom();
        SetTargetRandomPosition();

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

    public void InitializeRooms()
    {
        foreach (var room in Rooms)
        {
            room.Initialize();
        }
    }

    public void ResetEnvironment()
    {
<<<<<<< Updated upstream
        InitializeRooms();
        SelectRandomRoom();
        SetTargetRandomPosition();
=======
        if (Rooms != null && Rooms.Count > 0)
        {
            SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
        }
>>>>>>> Stashed changes
    }

    public void SelectRandomRoom()
    {
<<<<<<< Updated upstream
        if (Rooms.Count == 0) return;
        SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
=======
        if (Rooms != null)
        {
            var nearbyRooms = Rooms.Where(r => Vector3.Distance(r.transform.localPosition, center) <= radius).ToList();
            if (nearbyRooms.Count > 0)
            {
                SelectedRoom = nearbyRooms[Random.Range(0, nearbyRooms.Count)];
            }
        }
>>>>>>> Stashed changes
    }

    public void SetTargetRandomPosition()
    {
<<<<<<< Updated upstream
        if (SelectedRoom == null) return;

        Vector3 roomCenter = SelectedRoom.transform.localPosition;
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        target.localPosition = roomCenter + randomOffset;
=======
        if (SelectedRoom != null && target != null)
        {
            Vector3 roomCenter = SelectedRoom.transform.localPosition;
            Vector3 randomPosition = roomCenter + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
            target.localPosition = randomPosition;
        }
>>>>>>> Stashed changes
    }

    public Vector3 GetRandomAgentPosition()
    {
<<<<<<< Updated upstream
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
=======
        if (area == null)
        {
            Debug.LogError("Area is not assigned in EnvironmentManager!");
            return Vector3.zero;
        }

        // Bounds'ý her seferinde güncelle
        areaBounds = area.bounds;

        Vector3 randomSpawnPos;
        int maxAttempts = 100;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(areaBounds.min.x, areaBounds.max.x);
            float randomZ = Random.Range(areaBounds.min.z, areaBounds.max.z);

            randomSpawnPos = new Vector3(randomX, areaBounds.max.y + 1f, randomZ);

            // Debug için
            Debug.Log($"Attempt {attempts}: Testing position {randomSpawnPos}");

            if (!Physics.CheckSphere(randomSpawnPos, 0.5f, obstacleLayer))
            {
                Debug.Log($"Valid position found at {randomSpawnPos} after {attempts} attempts");
                return randomSpawnPos;
            }
        }

        Debug.LogError($"Could not find valid spawn position after {maxAttempts} attempts");
        return areaBounds.center + Vector3.up * 2f;
>>>>>>> Stashed changes
    }

    public Quaternion GetRandomAgentRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    // Debug için Inspector'dan çaðrýlacak metod
    [ContextMenu("Print Spawn Info")]
    public void PrintSpawnInfo()
    {
        if (area == null)
        {
            Debug.LogError("Area is not assigned!");
            return;
        }

        Debug.Log($"Area: {area.name}");
        Debug.Log($"Bounds Center: {area.bounds.center}");
        Debug.Log($"Bounds Size: {area.bounds.size}");

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = GetRandomAgentPosition();
            Debug.Log($"Spawn Position {i}: {pos}");

            bool isObstructed = Physics.CheckSphere(pos, 0.5f, obstacleLayer);
            Debug.Log($"  Is obstructed: {isObstructed}");
        }
    }

    // Debug için Gizmos çizimi
    private void OnDrawGizmos()
    {
        if (area != null)
        {
            Bounds bounds = area.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Mevcut ajan pozisyonlarýný göster
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
    }
}