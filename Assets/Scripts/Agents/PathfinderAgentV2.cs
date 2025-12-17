//using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
//using Unity.MLAgents.Sensors;
//using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//public class PathfinderAgentV2 : Agent
//{
//    [Header("References")]
//    [field: SerializeField]
//    private EnvironmentManager EnvironmentManager { get; set; }

//    [Header("Movement")]
//    [SerializeField]
//    private float agentMoveSpeed = 10f;
//    [SerializeField]
//    private float agentRotateSpeed = 90f;

//    [Header("Rewards")]
//    public float reachTargetReward = 1.0f;
//    public float hitWallPenalty = -1.0f;

//    [Tooltip("Adým baþýna zaman cezasý. MaxStep'e bölünür.")]
//    public float stepPenalty = -1.0f;
//    [Tooltip("Hedef odasýndayken hedefe yaklaþma ödülü. MaxStep'e bölünür.")]
//    public float approachReward = 1.0f;

//    [Tooltip("Doðru odayý ilk kez bulma ödülü.")]
//    public float foundCorrectRoomReward = 0.5f;
//    [Tooltip("Daha önce ziyaret edilen yanlýþ odaya girme cezasý.")]
//    public float revisitWrongRoomPenalty = -0.2f;
//    [Tooltip("Yeni (ama yanlýþ) bir odayý ilk kez keþfetme ödülü.")]
//    public float discoverNewRoomReward = 0.1f;

//    private Rigidbody agentRb;
//    private bool isTargetRoomFound;
//    private bool isInRoom = false;
//    private bool isRevisited = false;
//    private Room visitedRoom;

//    public override void Initialize()
//    {
//        agentRb = GetComponent<Rigidbody>();
//    }

//    public override void OnEpisodeBegin()
//    {
//        EnvironmentManager.InitializeRooms();

//        agentRb.linearVelocity = Vector3.zero;
//        agentRb.angularVelocity = Vector3.zero;

//        transform.SetLocalPositionAndRotation(EnvironmentManager.GetRandomAgentPosition(), EnvironmentManager.GetRandomAgentRotation());
//        //EnvironmentManager.SelectRoom(transform.localPosition, Random.Range(15, 20));
//        EnvironmentManager.SetTargetRandomPosition();

//        isTargetRoomFound = false;
//        isInRoom = false;
//        isRevisited = false;
//        visitedRoom = null;
//    }

//    public override void CollectObservations(VectorSensor sensor)
//    {
//        sensor.AddObservation(transform.InverseTransformDirection(agentRb.linearVelocity));
//        sensor.AddObservation(agentRb.angularVelocity.y);

//        sensor.AddObservation(isTargetRoomFound);
//        sensor.AddObservation(isInRoom);
//        sensor.AddObservation(isRevisited);
//    }

//    public override void OnActionReceived(ActionBuffers actions)
//    {
//        float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0, 1f);
//        float rotateInput = actions.ContinuousActions[1];

//        Vector3 move = agentMoveSpeed * moveInput * transform.forward;
//        agentRb.linearVelocity = move;

//        Vector3 rotation = agentRotateSpeed * rotateInput * Vector3.up;
//        agentRb.angularVelocity = rotation;

//        AddReward(stepPenalty / MaxStep);
//    }

//    public override void Heuristic(in ActionBuffers actionsOut)
//    {
//        var continuousActionsOut = actionsOut.ContinuousActions;
//        continuousActionsOut.Clear();

//        if (Input.GetKey(KeyCode.W))
//        {
//            continuousActionsOut[0] = 1f;
//        }

//        if (Input.GetKey(KeyCode.D))
//        {
//            continuousActionsOut[1] = 1f;
//        }
//        else if (Input.GetKey(KeyCode.A))
//        {
//            continuousActionsOut[1] = -1f;
//        }
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        if (collision.gameObject.CompareTag(Tags.Target))
//        {
//            AddReward(reachTargetReward);
//            EndEpisode();
//        }
//        else if (collision.gameObject.CompareTag(Tags.Wall))
//        {
//            AddReward(hitWallPenalty);
//            EndEpisode();
//        }
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.gameObject.CompareTag(Tags.Room))
//        {
//            if (other.TryGetComponent(out Room room))
//            {
//                isInRoom = true;
//                visitedRoom = room;

//                if (room == EnvironmentManager.SelectedRoom)
//                {
//                    if (!isTargetRoomFound)
//                    {
//                        AddReward(foundCorrectRoomReward);
//                        isTargetRoomFound = true;
//                    }
//                }
//                else
//                {
//                    if (room.IsVisited)
//                    {
//                        AddReward(revisitWrongRoomPenalty);
//                        isRevisited = true;
//                    }
//                    else
//                    {
//                        AddReward(discoverNewRoomReward);
//                        room.IsVisited = true;
//                    }
//                }
//            }
//        }
//    }

//    private void OnTriggerExit(Collider other)
//    {
//        if (other.gameObject.CompareTag(Tags.Room))
//        {
//            if (other.GetComponent<Room>() == visitedRoom)
//            {
//                isInRoom = false;
//                isRevisited = false;
//                visitedRoom = null;
//            }
//        }
//    }
//}