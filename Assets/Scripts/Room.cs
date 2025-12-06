using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField]
    private bool _isRandomRotation = false;

    public bool IsVisited { get; set; } = false;

    private void Rotate()
    {
        if (_isRandomRotation)
        {
            transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
        }
    }

    public void Initialize()
    {
        Rotate();
        IsVisited = false;
        gameObject.tag = Tags.Room;
    }

    public void MarkAsVisited()
    {
        if(IsVisited) return; // 2 defa marklamasýný engellemek için koydum

        IsVisited = true;
        gameObject.tag = Tags.Visited;
    }
}