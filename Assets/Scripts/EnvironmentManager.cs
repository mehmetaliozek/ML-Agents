using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{

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

    private void Awake()
    {
        areaBounds = area.bounds;
        Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
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
        SelectedRoom = Rooms[Random.Range(0, Rooms.Count)];
    }

    public void SelectRoom(Vector3 center, float radius)
    {
        var nearbyRooms = Rooms.Where(r => Vector3.Distance(r.transform.localPosition, center) <= radius).ToList();
        SelectedRoom = nearbyRooms[Random.Range(0, nearbyRooms.Count)];
    }

    public void SetTargetRandomPosition()
    {
        Vector3 roomCenter = SelectedRoom.transform.localPosition;
        Vector3 randomPosition = roomCenter + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        target.localPosition = randomPosition;
    }

    public Vector3 GetRandomAgentPosition()
    {
        Vector3 randomSpawnPos;
        while (true)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
            randomSpawnPos = area.transform.localPosition + new Vector3(randomPosX, 1f, randomPosZ);

            if (!Physics.CheckSphere(randomSpawnPos, 2.0f, obstacleLayer)) break;
        }
        return randomSpawnPos;
    }

    public Quaternion GetRandomAgentRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0, 360), 0f);
    }
}