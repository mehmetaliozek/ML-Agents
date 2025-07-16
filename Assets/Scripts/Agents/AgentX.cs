using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentX : Agent
{
    [Header("Rewards and Penalties")]
    private readonly float targetReward = 5f;
    private readonly float doorReward = 2.5f;
    private readonly float wallPenalty = -7.5f;
    private readonly float distanceRewardMultiplier = 0.01f;
    private readonly float stepPenalty = -0.5f;

    [Space(5)]
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Space(5)]
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Space(5)]
    [Header("Environment Settings")]
    [SerializeField] private EnvironmentManager environmentManager;

    private Rigidbody rb;
    private float previousDistanceToTarget;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        environmentManager.RandomRotateRooms();

        transform.localPosition = environmentManager.GetAgentRandomPosition();
        target.localPosition = environmentManager.GetTargetRandomPosition();

        previousDistanceToTarget = Vector3.Distance(transform.localPosition, TargetSelector());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.forward);

        sensor.AddObservation(environmentManager.GetDoorPosition());
        sensor.AddObservation((environmentManager.GetDoorPosition() - transform.localPosition).normalized);

        sensor.AddObservation(target.localPosition);
        sensor.AddObservation((target.localPosition - transform.localPosition).normalized);

        sensor.AddObservation(environmentManager.SelectedRoom.IsPassedDoor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];

        Vector3 moveDirection = MoveActionToVector(moveAction);
        transform.rotation = RotateWithMoveAction(moveAction);
        rb.linearVelocity = moveDirection * moveSpeed;

        AddReward(stepPenalty / MaxStep);

        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, TargetSelector());
        float distanceDelta = (previousDistanceToTarget - currentDistanceToTarget);
        AddReward(distanceDelta * distanceRewardMultiplier);

        previousDistanceToTarget = currentDistanceToTarget;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 0;
        else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 3;
    }

    private Vector3 MoveActionToVector(int action)
    {
        return action switch
        {
            (int)MoveAction.Forward => Vector3.forward,
            (int)MoveAction.Backward => Vector3.back,
            (int)MoveAction.Right => Vector3.right,
            (int)MoveAction.Left => Vector3.left,
            _ => Vector3.zero,
        };
    }

    private Quaternion RotateWithMoveAction(int action)
    {
        return action switch
        {
            (int)MoveAction.Forward => Quaternion.Euler(0, 0, 0),
            (int)MoveAction.Backward => Quaternion.Euler(0, 180, 0),
            (int)MoveAction.Right => Quaternion.Euler(0, 90, 0),
            (int)MoveAction.Left => Quaternion.Euler(0, -90, 0),
            _ => Quaternion.Euler(0, 0, 0),
        };
    }


    private Vector3 TargetSelector()
    {
        return environmentManager.SelectedRoom.IsPassedDoor switch
        {
            true => target.localPosition,
            false => environmentManager.GetDoorPosition()
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            AddReward(targetReward);
            EndEpisode();
        }
        else if (other.CompareTag("Wall"))
        {
            AddReward(wallPenalty);
            EndEpisode();
        }
        else if (other.CompareTag("Door"))
        {
            if (environmentManager.SelectedRoom.Door.gameObject == other.gameObject)
            {
                if (!environmentManager.SelectedRoom.IsPassedDoor)
                {
                    AddReward(doorReward);
                    environmentManager.SelectedRoom.IsPassedDoor = true;
                    previousDistanceToTarget = Vector3.Distance(transform.localPosition, TargetSelector());
                }
            }
        }
    }
}