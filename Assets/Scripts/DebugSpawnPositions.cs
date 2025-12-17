using UnityEngine;

public class DebugSpawnPositions : MonoBehaviour
{
    public EnvironmentManager envManager;
    public bool drawGizmos = true;

    void OnDrawGizmos()
    {
        if (!drawGizmos || envManager == null || envManager.Area == null) return;

        // Area bounds'ý çiz (artýk property kullanýyoruz)
        Bounds bounds = envManager.Area.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Spawn pozisyonlarýný test et
        for (int i = 0; i < 10; i++)
        {
            Vector3 testPos = bounds.center +
                new Vector3(
                    Random.Range(-bounds.extents.x, bounds.extents.x),
                    1f,
                    Random.Range(-bounds.extents.z, bounds.extents.z)
                );

            // Artýk property kullanýyoruz
            Gizmos.color = Physics.CheckSphere(testPos, 0.5f, envManager.ObstacleLayer) ?
                Color.red : Color.blue;
            Gizmos.DrawSphere(testPos, 0.5f);
        }
    }
}