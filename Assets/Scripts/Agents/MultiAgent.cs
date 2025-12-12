using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class MultiAgent : Agent
{
    [Header("References")]
    [SerializeField] private EnvironmentManagerV2 envManager;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    [Header("Movement Settings")]
    [SerializeField] private float agentMoveSpeed = 10f;
    [SerializeField] private float agentRotateSpeed = 2.5f;
    [SerializeField] private float travelSpeed = 12f;
    [SerializeField] private float stopDistance = 3.5f;

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

    private bool _isTravelingToStartPoint = false;
    private Vector3 _travelTargetPosition;
    private Quaternion _travelTargetRotation;

    protected override void Awake() 
    { 
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        _startPosition = transform.localPosition;

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
    }

    private void FixedUpdate()
    {
        if (navMeshAgent.enabled)
        {
            navMeshAgent.nextPosition = transform.position;
        }

        if (_isTravelingToStartPoint)
        {
            HandleTravelMovement();
        }
    }

    private void HandleTravelMovement()
    {
        if (!navMeshAgent.isOnNavMesh) return;

        float distance = Vector3.Distance(transform.position, _travelTargetPosition);
        if (distance <= stopDistance)
        {
            StartTrainingPhase();
            return;
        }

        Vector3 dir = (navMeshAgent.steeringTarget - transform.position).normalized;

        dir.y = 0;
        dir.Normalize();

        Vector3 targetVelocity = dir * travelSpeed;
        _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);

        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    private void StartTrainingPhase()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.rotation = _travelTargetRotation;

        _isTravelingToStartPoint = false;
        navMeshAgent.enabled = false;
        navMeshObstacle.enabled = true;
    }

    public override void OnEpisodeBegin()
    {
        // Normalde burasý her resetlendiðinde çalýþýr. 
        // Ancak bu senaryoda "Reset" mantýðýný MultiAgentGroup yönetiyor.
        // Ajan kendi kendine reset atmamalý, grup onu resetlemeli.
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_isTravelingToStartPoint)
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(false);
            sensor.AddObservation(false);
            sensor.AddObservation(false);
            return;
        }

        sensor.AddObservation(transform.InverseTransformDirection(_rb.linearVelocity));
        sensor.AddObservation(_rb.angularVelocity.y);
        sensor.AddObservation(_isTargetRoomFound);
        sensor.AddObservation(_isInRoom);
        sensor.AddObservation(_isRevisited);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_isTravelingToStartPoint) return;

        MoveAgent(actions);
        AddReward(stepPenalty/MaxStep);
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


    public void ResetAgentFull()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.localPosition = _startPosition;
        transform.rotation = Quaternion.identity;

        _exploredRoomCounter = 0;
        _isTargetRoomFound = false;
        _isInRoom = false;
        _isRevisited = false;
        _currentRoom = null;
        _isTravelingToStartPoint = false;
        _travelTargetPosition = Vector3.zero;
        _travelTargetRotation = Quaternion.identity;

        navMeshAgent.enabled = false;
        navMeshObstacle.enabled = true;
        this.enabled = false;
    }

    public void ActivateAndTravelTo(Vector3 targetPos, Quaternion rotation)
    {
        this.enabled = true;
        navMeshObstacle.enabled = false;
        navMeshAgent.enabled = true;

        _travelTargetPosition = targetPos;
        navMeshAgent.SetDestination(_travelTargetPosition);
        _travelTargetRotation = rotation;
        _isTravelingToStartPoint = true;
    }

    public void ActivateDirectly()
    {
        this.enabled = true;
        navMeshObstacle.enabled = true;
        navMeshAgent.enabled = false;
        _isTravelingToStartPoint = false;

        transform.localRotation = envManager.GetRandomAgentRotation();
    }

    public void StopMovement()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (_isTravelingToStartPoint) return;

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
        if (_isTravelingToStartPoint) return;

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
                _exploredRoomCounter++;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isTravelingToStartPoint) return;

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