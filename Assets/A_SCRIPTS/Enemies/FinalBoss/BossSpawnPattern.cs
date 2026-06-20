using System.Collections;
using UnityEngine;

public class BossSpawnPattern : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject dashEnemyPrefab;
    [SerializeField] private Transform player;

    [Header("Configuracion")]
    [SerializeField] private float delayBetweenRows = 0.8f;
    [SerializeField] private float spawnDistance = 3f;   // distancia adelante/atras del player
    [SerializeField] private float columnSpacing = 2f;   // separacion entre columnas

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ExecutePattern();
    }
    public void ExecutePattern()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        Vector3 center = player.position;

        // Posiciones X de las 3 columnas
        float[] xOffsets = { -columnSpacing, 0f, columnSpacing };

        // Fila de arriba (Z positivo) → dashean hacia abajo (Vector3.back)
        foreach (float x in xOffsets)
        {
            Vector3 spawnPos = center + new Vector3(x, 0f, spawnDistance);
            Spawn(spawnPos, Vector3.back);
        }

        yield return new WaitForSeconds(delayBetweenRows);

        // Fila de abajo (Z negativo) → dashean hacia arriba (Vector3.forward)
        foreach (float x in xOffsets)
        {
            Vector3 spawnPos = center + new Vector3(x, 0f, -spawnDistance);
            Spawn(spawnPos, Vector3.forward);
        }
    }

    private void Spawn(Vector3 position, Vector3 direction)
    {
        position.y = player.position.y;
        var go = Instantiate(dashEnemyPrefab, position, Quaternion.identity);
        var enemy = go.GetComponent<DashBossEnemy>();
        enemy.OnDead += OnEnemyDead;
        go.GetComponent<EnemyEmerge>()?.Emerge(player);
        enemy.Launch(direction);
    }

    private void OnEnemyDead(DashBossEnemy enemy)
    {
        enemy.OnDead -= OnEnemyDead;
        Destroy(enemy.gameObject);
    }

    private void OnDrawGizmos()
    {
        if (player == null) return;

        Vector3 center = player.position;
        float[] xOffsets = { -columnSpacing, 0f, columnSpacing };

        // Fila arriba - cyan
        Gizmos.color = Color.cyan;
        foreach (float x in xOffsets)
        {
            Vector3 pos = center + new Vector3(x, 0f, spawnDistance);
            Gizmos.DrawSphere(pos, 0.3f);
            Gizmos.DrawLine(pos, pos + Vector3.back * 2f);
        }

        // Fila abajo - rojo
        Gizmos.color = Color.red;
        foreach (float x in xOffsets)
        {
            Vector3 pos = center + new Vector3(x, 0f, -spawnDistance);
            Gizmos.DrawSphere(pos, 0.3f);
            Gizmos.DrawLine(pos, pos + Vector3.forward * 2f);
        }

        // Player - amarillo
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.25f);
    }
}