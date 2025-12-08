using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvironmentManagerV2 : MonoBehaviour
{
    [Header("References")]
    [field: SerializeField]
    public MultiAgentGroup MultiAgentGroup { get; private set; }
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

    private void Awake()
    {
        if (area != null) areaBounds = area.bounds;

        Rooms = roomParent.GetComponentsInChildren<Room>().ToList();
            
        InitializeRooms();
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