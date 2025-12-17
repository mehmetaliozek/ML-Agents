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
    public float hitWallPenalty = -1.0f;

    [Tooltip("Adým baþýna zaman cezasý. MaxStep'e bölünür.")]
    public float stepPenalty = -1.0f;
    [Tooltip("Hedef odasýndayken hedefe yaklaþma ödülü. MaxStep'e bölünür.")]
    public float approachReward = 1.0f;

    [Tooltip("Doðru odayý ilk kez bulma ödülü.")]
    public float foundCorrectRoomReward = 0.5f;
    [Tooltip("Daha önce ziyaret edilen yanlýþ odaya girme cezasý.")]
    public float revisitWrongRoomPenalty = -0.2f;
    [Tooltip("Yeni (ama yanlýþ) bir odayý ilk kez keþfetme ödülü.")]
    public float discoverNewRoomReward = 0.1f;
    [Tooltip("duvarda sürünmeye devam ettikçe vereceðimiz ceza")]
    public float wallStayPenalty = -0.01f;

    private Rigidbody agentRb;
    //private bool isTargetRoomFound; //artýk bulunduðunda EnvironmentManager'a bildiriyoruz, böylece multi-agent senaryosunda diðer ajanlar da ödül alabilir
    private bool isInRoom = false;
    //private bool isRevisited = false; // Room.cs içindeki IsVisited'dan okuduðumuz için gerekli olmayabilir
    private Room visitedRoom;

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        //Episode iþlemleri bizim kontrolümüzde olsun, ml agentsdan çýkaralým diye sildim artýk Manager çaðýracak, OnGroupEpisodeBegin bak

        //EnvironmentManager.InitializeRooms();
        //EnvironmentManager.SelectRoom();
        //EnvironmentManager.SetTargetRandomPosition();

        //agentRb.linearVelocity = Vector3.zero;
        //agentRb.angularVelocity = Vector3.zero;

        //transform.SetLocalPositionAndRotation(EnvironmentManager.GetRandomAgentPosition(), EnvironmentManager.GetRandomAgentRotation());

        //isTargetRoomFound = false;
        //isInRoom = false;
        //isRevisited = false;
        //visitedRoom = null;
    }

    public void OnGroupEpisodeBegin()
    {
        // Debug log ekleyin
        Debug.Log($"{gameObject.name} - OnGroupEpisodeBegin called");

        // Fiziksel hareketi sýfýrla
        agentRb.linearVelocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;

        // Pozisyon ve rotasyonu ayarla
        Vector3 newPosition = EnvironmentManager.GetRandomAgentPosition();
        Quaternion newRotation = EnvironmentManager.GetRandomAgentRotation();

        Debug.Log($"{gameObject.name} moving to position: {newPosition}");

        // Doðrudan transform.position kullanýn
        transform.position = newPosition;
        transform.rotation = newRotation;

        // Eðer Rigidbody'de interpolation kullanýyorsanýz, pozisyonu da ona göre ayarlayýn
        agentRb.position = newPosition;
        agentRb.rotation = newRotation;

        // Durumlarý sýfýrla
        isInRoom = false;
        visitedRoom = null;

        Debug.Log($"{gameObject.name} new position: {transform.position}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(agentRb.linearVelocity));
        sensor.AddObservation(agentRb.angularVelocity.y);

        //sensor.AddObservation(isTargetRoomFound);
        //sensor.AddObservation(isRevisited);

        sensor.AddObservation(isInRoom);

        if (visitedRoom != null)
        {
            sensor.AddObservation(visitedRoom.IsVisited);
        }
        else
        {
            sensor.AddObservation(false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0, 1f);
        float rotateInput = actions.ContinuousActions[1];

        Vector3 move = agentMoveSpeed * moveInput * transform.forward;
        agentRb.linearVelocity = move;

        Vector3 rotation = agentRotateSpeed * rotateInput * Vector3.up;
        agentRb.angularVelocity = rotation;

        AddReward(stepPenalty / MaxStep);
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
        if (collision.gameObject.CompareTag(Tags.Target))
        {
            //AddReward(reachTargetReward);
            //EndEpisode();
            //Ödüller artýk environment manager tarafýndan veriliyor

            EnvironmentManager.NotifyTargetFound(this);

        }
        else if (collision.gameObject.CompareTag(Tags.Wall))
        {
            AddReward(hitWallPenalty);
            //EndEpisode();
            //direkt bitirmek yerine öyle kaldýklarýnda ceza verelim sürünmesinler duvarlarda diye
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag(Tags.Wall))
        {
            AddReward(wallStayPenalty);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(Tags.Room) || other.gameObject.CompareTag(Tags.Visited))
        {
            if (other.TryGetComponent(out Room room))
            {
                isInRoom = true;
                visitedRoom = room;

                if (room == EnvironmentManager.SelectedRoom)
                {
                    AddReward(discoverNewRoomReward * 2);
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
                        EnvironmentManager.NotifyNewRoomExplored(this);
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

                if (room != EnvironmentManager.SelectedRoom)
                {
                    room.MarkAsVisited();
                }
            }
        }
    }
}