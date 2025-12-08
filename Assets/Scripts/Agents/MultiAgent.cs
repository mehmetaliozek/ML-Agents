using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MultiAgent : Agent
{
    [Header("References")]
    [SerializeField] private EnvironmentManager envManager;

    [Header("Movement Settings")]
    [SerializeField] private float agentMoveSpeed = 10f;
    [SerializeField] private float agentRotateSpeed = 2.5f;

    [Header("Reward Settings")]
    [SerializeField] private float reachTargetReward = 1.0f;
    [SerializeField] private float hitWallPenalty = -1.0f;
    [SerializeField] private float stepPenalty = -1.0f;
    [SerializeField] private float foundCorrectRoomReward = 0.5f;
    [SerializeField] private float revisitWrongRoomPenalty = -0.2f;
    [SerializeField] private float discoverNewRoomReward = 0.1f;

    [Header("Exploration Settings")]
    [SerializeField] private int maxExplorerRoomCount = 5;

    private Rigidbody _rb;
    private bool _isTargetRoomFound;
    private bool _isInRoom;
    private bool _isRevisited;
    private Room _currentRoom;
    private int _exploredRoomCounter;
    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.localPosition;
    }

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        StopMovement();

        transform.localRotation = envManager.GetRandomAgentRotation();

        _isTargetRoomFound = false;
        _isInRoom = false;
        _isRevisited = false;
        _currentRoom = null;
        _exploredRoomCounter = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(_rb.linearVelocity));
        sensor.AddObservation(_rb.angularVelocity.y);

        sensor.AddObservation(_isTargetRoomFound);
        sensor.AddObservation(_isInRoom);
        sensor.AddObservation(_isRevisited);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions);

        AddReward(stepPenalty / MaxStep);
    }

    private void MoveAgent(ActionBuffers actions)
    {
        float moveSignal = Mathf.Clamp(actions.ContinuousActions[0], 0, 1f);
        float rotateSignal = actions.ContinuousActions[1];

        Vector3 moveForce = transform.forward * (moveSignal * agentMoveSpeed);
        _rb.linearVelocity = new Vector3(moveForce.x, _rb.linearVelocity.y, moveForce.z);

        Vector3 rotation = transform.up * (rotateSignal * agentRotateSpeed);
        _rb.angularVelocity = rotation;
    }

    public void StopMovement()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetAgent()
    {
        transform.localPosition = _startPosition;
        _exploredRoomCounter = 0;
        enabled = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions.Clear();

        if (Input.GetKey(KeyCode.W)) continuousActions[0] = 1f;
        if (Input.GetKey(KeyCode.D)) continuousActions[1] = 1f;
        else if (Input.GetKey(KeyCode.A)) continuousActions[1] = -1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Tags.Target))
        {
            envManager.MultiAgentGroup.AddGroupReward(reachTargetReward);
            AddReward(reachTargetReward);
            envManager.MultiAgentGroup.EndGroupEpisode();
        }
        else if (collision.gameObject.CompareTag(Tags.Wall) || collision.gameObject.CompareTag(Tags.Agent))
        {
            envManager.MultiAgentGroup.AddGroupReward(hitWallPenalty);
            AddReward(hitWallPenalty);    
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Tags.Room)) return;

        if (other.TryGetComponent(out Room room))
        {
            _isInRoom = true;
            _currentRoom = room;

            HandleRoomLogic(room);
        }
    }

    private void HandleRoomLogic(Room room)
    {
        if (room == envManager.SelectedRoom)
        {
            if (!_isTargetRoomFound)
            {
                envManager.MultiAgentGroup.AddGroupReward(foundCorrectRoomReward);
                AddReward(foundCorrectRoomReward);
                _isTargetRoomFound = true;
            }
        }
        else
        {
            if (room.IsVisited)
            {
                AddReward(revisitWrongRoomPenalty);
                _isRevisited = true;
            }
            else
            {
                envManager.MultiAgentGroup.AddGroupReward(foundCorrectRoomReward);
                AddReward(discoverNewRoomReward);

                room.IsVisited = true;
                _exploredRoomCounter++;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(Tags.Room)) return;

        if (other.TryGetComponent(out Room room) && room == _currentRoom)
        {
            _isInRoom = false;
            _isRevisited = false;
            _currentRoom = null;

            if (room != envManager.SelectedRoom)
            {
                room.MarkAsVisited();
            }

            if (_exploredRoomCounter >= maxExplorerRoomCount)
            {
                _exploredRoomCounter = 0;
                StopMovement();
                envManager.MultiAgentGroup.NextAgent();
            }
        }
    }
}