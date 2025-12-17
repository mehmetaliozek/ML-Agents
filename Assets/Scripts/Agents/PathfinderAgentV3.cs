using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PathfinderAgentV3 : Agent
{
    [Header("References")]
    [field: SerializeField]
    private EnvironmentManager EnvironmentManager { get; set; }

    [Header("Movement")]
    [SerializeField]
    private float agentMoveSpeed = 10f;
    [SerializeField]
    private float agentRotateSpeed = 90f;

    [Header("Rewards")]
    public float reachTargetReward = 1.0f;
    public float hitWallPenalty = -0.5f; // Azaltýlmýþ ceza
    public float stepPenalty = -0.001f;
    public float approachReward = 0.1f;
    public float foundCorrectRoomReward = 0.5f;
    public float revisitWrongRoomPenalty = -0.2f;
    public float discoverNewRoomReward = 0.1f;
    public float wallStayPenalty = -0.1f; // Artýrýlmýþ ceza

    [Header("Stuck Detection")]
    [SerializeField]
    private float stuckTimeThreshold = 2f;
    [SerializeField]
    private float stuckDistanceThreshold = 0.3f;

    private Rigidbody agentRb;
    private bool isInRoom = false;
    private Room visitedRoom;
    private Vector3 lastValidPosition;
    private float timeAtCurrentPosition = 0f;
    private float timeInWallContact = 0f;
    private bool isInWallContact = false;
    private bool hasHitWall = false;

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        agentRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        lastValidPosition = transform.position;

        if (EnvironmentManager == null)
        {
            Debug.LogError("EnvironmentManager is not assigned!");
        }
    }

    public override void OnEpisodeBegin()
    {
        // ML-Agents episode baþlangýcý - EnvironmentManager kontrol ediyor
    }

    public void OnGroupEpisodeBegin()
    {
        hasHitWall = false;

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

            lastValidPosition = newPosition;
        }

        isInRoom = false;
        visitedRoom = null;
        timeAtCurrentPosition = 0f;
        timeInWallContact = 0f;
        isInWallContact = false;
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
        {
            sensor.AddObservation(visitedRoom.IsVisited ? 1f : 0f);
        }
        else
        {
            sensor.AddObservation(0f);
        }

        if (EnvironmentManager != null && EnvironmentManager.Target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, EnvironmentManager.Target.position);
            sensor.AddObservation(distanceToTarget);
        }
        else
        {
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (agentRb == null || hasHitWall) return;

        float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0, 1f);
        float rotateInput = actions.ContinuousActions[1];

        Vector3 move = agentMoveSpeed * moveInput * transform.forward;
        agentRb.linearVelocity = move;

        Vector3 rotation = agentRotateSpeed * rotateInput * Vector3.up;
        agentRb.angularVelocity = rotation;

        AddReward(stepPenalty);

        CheckIfStuck();

        if (EnvironmentManager != null && EnvironmentManager.Target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, EnvironmentManager.Target.position);
            if (distanceToTarget < 5f)
            {
                AddReward(approachReward * Time.fixedDeltaTime * (5f - distanceToTarget) / 5f);
            }
        }
    }

    private void CheckIfStuck()
    {
        if (hasHitWall) return;

        float distanceMoved = Vector3.Distance(transform.position, lastValidPosition);

        if (distanceMoved > stuckDistanceThreshold)
        {
            lastValidPosition = transform.position;
            timeAtCurrentPosition = 0f;
        }
        else
        {
            timeAtCurrentPosition += Time.fixedDeltaTime;

            if (timeAtCurrentPosition > stuckTimeThreshold)
            {
                Debug.Log($"{gameObject.name} is stuck at position: {transform.position}");
                if (EnvironmentManager != null)
                {
                    EnvironmentManager.NotifyStuckInWall(this);
                }
            }
        }

        if (isInWallContact)
        {
            timeInWallContact += Time.fixedDeltaTime;

            if (timeInWallContact > 1f) // 1 saniyeden fazla duvardaysa
            {
                Debug.Log($"{gameObject.name} stuck in wall for {timeInWallContact:F1} seconds");
                if (EnvironmentManager != null)
                {
                    EnvironmentManager.NotifyStuckInWall(this);
                }
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut.Clear();

        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[1] = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[1] = -1f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHitWall) return;

        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log($"{gameObject.name} collided with target!");
            if (EnvironmentManager != null)
            {
                EnvironmentManager.NotifyTargetFound(this);
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log($"{gameObject.name} hit a wall at position: {transform.position}");
            hasHitWall = true;

            // Bireysel ceza (isteðe baðlý, grup cezasý da var)
            AddReward(hitWallPenalty * 0.5f);

            if (EnvironmentManager != null)
            {
                EnvironmentManager.NotifyWallHit(this);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (hasHitWall) return;

        if (collision.gameObject.CompareTag("Wall"))
        {
            isInWallContact = true;
            AddReward(wallStayPenalty * Time.fixedDeltaTime);

            // Duvardan uzaklaþtýrmak için küçük bir itme kuvveti
            if (collision.contacts.Length > 0)
            {
                Vector3 pushDirection = (transform.position - collision.contacts[0].point).normalized;
                agentRb.AddForce(pushDirection * 3f, ForceMode.VelocityChange);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isInWallContact = false;
            timeInWallContact = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitWall) return;

        if (other.gameObject.CompareTag("Room") || other.gameObject.CompareTag("Visited"))
        {
            if (other.TryGetComponent(out Room room))
            {
                isInRoom = true;
                visitedRoom = room;

                if (EnvironmentManager != null && room == EnvironmentManager.SelectedRoom)
                {
                    AddReward(foundCorrectRoomReward);
                    Debug.Log($"{gameObject.name} found correct room: {room.name}");
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
                        if (EnvironmentManager != null)
                        {
                            EnvironmentManager.NotifyNewRoomExplored(this);
                        }
                        AddReward(discoverNewRoomReward);
                        Debug.Log($"{gameObject.name} discovered new room: {room.name}");
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Room") || other.gameObject.CompareTag("Visited"))
        {
            Room room = other.GetComponent<Room>();
            if (room == visitedRoom)
            {
                isInRoom = false;
                visitedRoom = null;
            }
        }
    }

    void FixedUpdate()
    {
        if (!hasHitWall && isInWallContact)
        {
            // Duvarda sýkýþmýþsa küçük bir zýplama kuvveti
            agentRb.AddForce(Vector3.up * 2f, ForceMode.Acceleration);
        }
    }
}