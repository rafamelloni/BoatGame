using UnityEngine;

public class IslandSpawnPoint : MonoBehaviour
{
    public SpawnPointType pointType;

    [Tooltip("Peso relativo: 0 = nunca spawnea, 1 = siempre")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = pointType switch
        {
            SpawnPointType.Vegetation => Color.green,
            SpawnPointType.Decoration => Color.yellow,
            SpawnPointType.Defense => Color.red,
            _ => Color.white
        };
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
