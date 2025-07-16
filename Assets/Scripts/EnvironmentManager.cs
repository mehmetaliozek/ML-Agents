using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoints;
    [SerializeField] private GameObject roomsParent;

    private List<Transform> points;
    private List<Room> rooms;

    public Room SelectedRoom { get; private set; }

    private void Awake()
    {
        rooms = roomsParent.GetComponentsInChildren<Room>().ToList();
        points = spawnPoints.GetComponentsInChildren<Transform>().ToList();
    }

    public void RandomRotateRooms()
    {
        foreach (Room room in rooms)
        {
            room.SelfRandomRotate();
        }
    }

    public Vector3 GetAgentRandomPosition()
    {
        return points[Random.Range(0, points.Count)].localPosition;
    }

    public Vector3 GetTargetRandomPosition()
    {
        SelectedRoom = rooms[Random.Range(0, rooms.Count)];

        float rndX = SelectedRoom.transform.localPosition.x + Random.Range(-3f, 3f);
        float rndZ = SelectedRoom.transform.localPosition.z + Random.Range(-3f, 3f);

        return new Vector3(rndX, 0.5f, rndZ);
    }

    public Vector3 GetDoorPosition()
    {
        return transform.InverseTransformPoint(SelectedRoom.Door.position);
    }
}