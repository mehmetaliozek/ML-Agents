using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
public class PathfinderAgentV3 : Agent
{
    [Header("References")]
    private EnvironmentManager _environmentManager;
    public EnvironmentManager EnvironmentManager
    {
        get => _environmentManager;
        private set => _environmentManager = value;
    }

    [Header("Movement")]
    [SerializeField] private float agentMoveSpeed = 10f;
    [SerializeField] private float agentRotateSpeed = 90f;

    [Header("Rewards")]
    public float reachTargetReward = 1.0f;
    public float hitWallPenalty = -0.5f;
    public float stepPenalty = -0.001f;
    public float approachReward = 0.1f;
    public float foundCorrectRoomReward = 0.5f;
    public float revisitWrongRoomPenalty = -0.2f;
    public float discoverNewRoomReward = 0.1f;
    public float wallStayPenalty = -0.1f;

    // Debug için public yaptýk (Inspector'da görmek için)
    [Header("Debug Info")]
    public bool isInRoom = false;
    public Room visitedRoom;

    private Rigidbody agentRb;

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        agentRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void SetEnvironmentManager(EnvironmentManager manager)
    {
        EnvironmentManager = manager;
    }

    public override void OnEpisodeBegin() { } // Kontrol Manager'da

    public void OnGroupEpisodeBegin()
    {
        if (agentRb != null)
        {
            agentRb.linearVelocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
            agentRb.rotation = Quaternion.identity;
        }

        if (EnvironmentManager != null)
        {
            Vector3 newPosition = EnvironmentManager.GetRandomAgentPosition();
            Quaternion newRotation = EnvironmentManager.GetRandomAgentRotation();

            transform.position = newPosition;
            transform.rotation = newRotation;

            if (agentRb != null)
            {
                agentRb.position = newPosition;
                agentRb.rotation = newRotation;
            }
        }

        isInRoom = false;
        visitedRoom = null;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (agentRb != null)
        {
            sensor.AddObservation(transform.InverseTransformDirection(agentRb.linearVelocity));
            sensor.AddObservation(agentRb.angularVelocity.y);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        sensor.AddObservation(isInRoom);

        if (visitedRoom != null)
            sensor.AddObservation(visitedRoom.IsVisited ? 1f : 0f);
        else
            sensor.AddObservation(0f);

        if (EnvironmentManager != null && EnvironmentManager.Target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, EnvironmentManager.Target.position);
            sensor.AddObservation(distanceToTarget);
        }
        else
            sensor.AddObservation(0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (agentRb == null) return;

        float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0, 1f);
        float rotateInput = actions.ContinuousActions[1];

        Vector3 move = agentMoveSpeed * moveInput * transform.forward;
        agentRb.linearVelocity = move;

        Vector3 rotation = agentRotateSpeed * rotateInput * Vector3.up;
        agentRb.angularVelocity = rotation;

        AddReward(stepPenalty);

        if (EnvironmentManager != null && EnvironmentManager.Target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, EnvironmentManager.Target.position);
            if (distanceToTarget < 5f)
            {
                AddReward(approachReward * Time.fixedDeltaTime * (5f - distanceToTarget) / 5f);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut.Clear();

        if (Input.GetKey(KeyCode.W)) continuousActionsOut[0] = 1f;
        if (Input.GetKey(KeyCode.D)) continuousActionsOut[1] = 1f;
        else if (Input.GetKey(KeyCode.A)) continuousActionsOut[1] = -1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (EnvironmentManager == null) return;

        if (collision.gameObject.CompareTag(Tags.Target))
        {
            EnvironmentManager.NotifyTargetFound(this);
        }
        else if (collision.gameObject.CompareTag(Tags.Wall) || collision.gameObject.CompareTag(Tags.Agent))
        {
            AddReward(hitWallPenalty);
            EnvironmentManager.NotifyWallHit(this);
        }
    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag(Tags.Wall))
    //    {
    //        AddReward(wallStayPenalty * Time.fixedDeltaTime);

    //        if (collision.contacts.Length > 0)
    //        {
    //            Vector3 pushDirection = (transform.position - collision.contacts[0].point).normalized;
    //            agentRb.AddForce(pushDirection * 3f, ForceMode.VelocityChange);
    //        }
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(Tags.Room) || other.gameObject.CompareTag(Tags.Visited))
        {
            if (other.TryGetComponent(out Room room))
            {
                isInRoom = true;
                visitedRoom = room;

                if (EnvironmentManager != null && room == EnvironmentManager.SelectedRoom)
                {
                    AddReward(foundCorrectRoomReward * 2);
                }
                else
                {
                    if (room.IsVisited)
                    {
                        AddReward(revisitWrongRoomPenalty);
                    }
                    else
                    {
                        room.MarkAsVisited();
                        if (EnvironmentManager != null) EnvironmentManager.NotifyNewRoomExplored(this);
                        AddReward(discoverNewRoomReward);
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(Tags.Room) || other.gameObject.CompareTag(Tags.Visited))
        {
            Room room = other.GetComponent<Room>();
            if (room == visitedRoom)
            {
                isInRoom = false;
                visitedRoom = null;
            }
        }
    }
}