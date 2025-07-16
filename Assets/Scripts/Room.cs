using UnityEngine;

public class Room:MonoBehaviour
{
    [field: SerializeField]
    public Transform Door { get; private set; }

    public bool IsPassedDoor { get; set; } = false;

    public void SelfRandomRotate()
    {
        transform.rotation = Quaternion.Euler(
            0f,
            90f * Random.Range(0, 3),
            0f
        );
    }
}