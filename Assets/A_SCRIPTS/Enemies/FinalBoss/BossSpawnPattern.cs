using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnPattern : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject dashEnemyPrefab;
    [SerializeField] private EnemyFactory _factory;
    [SerializeField] private Transform player;

    // ─── Patron 1: Linea de Fuego ─────────────────────────────────────────
    [Header("Linea de Fuego")]
    [SerializeField] private float delayBetweenRows = 0.8f;
    [SerializeField] private float spawnDistance = 3f;
    [SerializeField] private float columnSpacing = 2f;

    // ─── Patron 2: Cerco Progresivo ───────────────────────────────────────
    [Header("Cerco Progresivo")]
    [SerializeField] private int _cercoGroupCount = 6;
    [SerializeField] private float _cercoRadiusInicial = 15f;
    [SerializeField] private float _cercoRadiusFinal = 8f;
    [SerializeField] private float _cercoDelayEntreOlas = 2f;

    // ─── Patron 3: Linea de Fuego Groups ─────────────────────────────────
    [Header("Linea de Fuego Groups")]
    [SerializeField] private int _lineaGroupCount = 4;
    [SerializeField] private float _lineaSpawnDistance = 12f;
    [SerializeField] private float _lineaSpacing = 3f;
    [SerializeField] private float _lineaDelayEntreFilas = 1f;

    private readonly List<EnemyGroup> _activeGroups = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ExecutePattern();
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ExecuteCercoProgresivo();
        if (Input.GetKeyDown(KeyCode.Alpha2))
            ExecuteLineaFuegoGroups();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PATRON ORIGINAL: DashBossEnemy en filas
    // ═════════════════════════════════════════════════════════════════════

    public void ExecutePattern()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        Vector3 center = player.position;
        float[] xOffsets = { -columnSpacing, 0f, columnSpacing };

        foreach (float x in xOffsets)
        {
            Vector3 spawnPos = center + new Vector3(x, 0f, spawnDistance);
            Spawn(spawnPos, Vector3.back);
        }

        yield return new WaitForSeconds(delayBetweenRows);

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

    // ═════════════════════════════════════════════════════════════════════
    //  PATRON 2: Cerco Progresivo
    //  Ola 1: groups en circulo grande → se acercan
    //  Ola 2: groups en circulo mas chico → se acercan
    // ═════════════════════════════════════════════════════════════════════

    public void ExecuteCercoProgresivo()
    {
        StartCoroutine(CercoProgresivoSequence());
    }

    private IEnumerator CercoProgresivoSequence()
    {
        // Ola exterior
        SpawnCirculo(_cercoRadiusInicial);

        yield return new WaitForSeconds(_cercoDelayEntreOlas);

        // Ola interior
        SpawnCirculo(_cercoRadiusFinal);
    }

    private void SpawnCirculo(float radius)
    {
        float angleStep = 360f / _cercoGroupCount;
        for (int i = 0; i < _cercoGroupCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 spawnPos = player.position + offset;
            spawnPos.y = player.position.y;

            var group = _factory.GetGroup(spawnPos, 0f);
            _activeGroups.Add(group);
            group.GetComponent<EnemyEmerge>()?.Emerge(player);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PATRON 3: Linea de Fuego con Groups
    //  Fila izquierda y fila derecha se cierran hacia el centro
    // ═════════════════════════════════════════════════════════════════════

    public void ExecuteLineaFuegoGroups()
    {
        StartCoroutine(LineaFuegoSequence());
    }

    private IEnumerator LineaFuegoSequence()
    {
        // Fila izquierda
        for (int i = 0; i < _lineaGroupCount; i++)
        {
            float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
            Vector3 spawnPos = player.position + new Vector3(-_lineaSpawnDistance, 0f, zOffset);
            spawnPos.y = player.position.y;

            var group = _factory.GetGroup(spawnPos, 0f);
            _activeGroups.Add(group);
            group.GetComponent<EnemyEmerge>()?.Emerge(player);
        }

        yield return new WaitForSeconds(_lineaDelayEntreFilas);

        // Fila derecha
        for (int i = 0; i < _lineaGroupCount; i++)
        {
            float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
            Vector3 spawnPos = player.position + new Vector3(_lineaSpawnDistance, 0f, zOffset);
            spawnPos.y = player.position.y;

            var group = _factory.GetGroup(spawnPos, 0f);
            _activeGroups.Add(group);
            group.GetComponent<EnemyEmerge>()?.Emerge(player);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        if (player == null) return;
        Vector3 center = player.position;

        // Patron 1 - DashBossEnemy
        float[] xOffsets = { -columnSpacing, 0f, columnSpacing };
        Gizmos.color = Color.cyan;
        foreach (float x in xOffsets)
        {
            Vector3 pos = center + new Vector3(x, 0f, spawnDistance);
            Gizmos.DrawSphere(pos, 0.3f);
            Gizmos.DrawLine(pos, pos + Vector3.back * 2f);
        }
        Gizmos.color = Color.red;
        foreach (float x in xOffsets)
        {
            Vector3 pos = center + new Vector3(x, 0f, -spawnDistance);
            Gizmos.DrawSphere(pos, 0.3f);
            Gizmos.DrawLine(pos, pos + Vector3.forward * 2f);
        }

        // Patron 2 - Cerco Progresivo
        float angleStep = 360f / _cercoGroupCount;
        Gizmos.color = new Color(1f, 0.5f, 0f); // naranja - ola exterior
        for (int i = 0; i < _cercoGroupCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _cercoRadiusInicial;
            Gizmos.DrawSphere(pos, 0.4f);
            Gizmos.DrawLine(pos, center);
        }
        Gizmos.color = Color.magenta; // interior
        for (int i = 0; i < _cercoGroupCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _cercoRadiusFinal;
            Gizmos.DrawSphere(pos, 0.4f);
            Gizmos.DrawLine(pos, center);
        }

        // Patron 3 - Linea de Fuego
        Gizmos.color = Color.green;
        for (int i = 0; i < _lineaGroupCount; i++)
        {
            float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
            Vector3 posL = center + new Vector3(-_lineaSpawnDistance, 0f, zOffset);
            Vector3 posR = center + new Vector3(_lineaSpawnDistance, 0f, zOffset);
            Gizmos.DrawSphere(posL, 0.4f);
            Gizmos.DrawLine(posL, posL + Vector3.right * 2f);
            Gizmos.DrawSphere(posR, 0.4f);
            Gizmos.DrawLine(posR, posR + Vector3.left * 2f);
        }

        // Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.25f);
    }
}