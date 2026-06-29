using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnPattern : MonoBehaviour
{
    [Serializable]
    private struct BossGroupConfig
    {
        public GameObject prefab;
        public int initialStock;
    }

    [Header("Referencias")]
    [SerializeField] private GameObject dashEnemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private BulletFactory _bulletFactory;

    [Header("Boss Groups")]
    [SerializeField] private BossGroupConfig[] _bossGroupConfigs;

    private ObjectPool<ZombieGroup>[] _bossGroupPools;
    private GameObject[] _bossGroupPrefabs;

    // ─── Patron 1: DashBossEnemy ──────────────────────────────────────────
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
    [SerializeField] private int _cercoGroupPrefabIndex = 0;

    // ─── Patron 3: Linea de Fuego Groups ─────────────────────────────────
    [Header("Linea de Fuego Groups")]
    [SerializeField] private int _lineaGroupCount = 4;
    [SerializeField] private float _lineaSpawnDistance = 12f;
    [SerializeField] private float _lineaSpacing = 3f;
    [SerializeField] private float _lineaDelayEntreFilas = 1f;
    [SerializeField] private int _lineaGroupPrefabIndex = 0;

    private readonly List<ZombieGroup> _activeGroups = new();

    private void Awake()
    {
        InitBossGroupPools();
    }

    private void InitBossGroupPools()
    {
        if (_bossGroupConfigs == null || _bossGroupConfigs.Length == 0) return;

        _bossGroupPools = new ObjectPool<ZombieGroup>[_bossGroupConfigs.Length];
        _bossGroupPrefabs = new GameObject[_bossGroupConfigs.Length];

        for (int i = 0; i < _bossGroupConfigs.Length; i++)
        {
            var config = _bossGroupConfigs[i];
            _bossGroupPrefabs[i] = config.prefab;
            _bossGroupPools[i] = new ObjectPool<ZombieGroup>(
                () => { var go = Instantiate(config.prefab); go.SetActive(false); return go.GetComponent<ZombieGroup>(); },
                g => g.gameObject.SetActive(true),
                g => g.gameObject.SetActive(false),
                config.initialStock
            );
        }
    }

    private ZombieGroup GetBossGroup(Vector3 position, int prefabIndex)
    {
        int idx = Mathf.Clamp(prefabIndex, 0, _bossGroupPools.Length - 1);
        var group = _bossGroupPools[idx].Get();
        group.transform.position = position;
        group.gameObject.SetActive(true);
        group.Init(player, _bulletFactory);
        group.GetComponent<EnemyEmerge>()?.Emerge(player);
        _activeGroups.Add(group);
        return group;
    }

    private void ReturnBossGroup(ZombieGroup group)
    {
        group.GetComponent<EnemyEmerge>()?.Reset();
        group.Cleanup();
        _activeGroups.Remove(group);
        for (int i = 0; i < _bossGroupPrefabs.Length; i++)
        {
            if (group.gameObject.name.Contains(_bossGroupPrefabs[i].name))
            {
                _bossGroupPools[i].Return(group);
                return;
            }
        }
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
    // ═════════════════════════════════════════════════════════════════════

    public void ExecuteCercoProgresivo()
    {
        StartCoroutine(CercoProgresivoSequence());
    }

    private IEnumerator CercoProgresivoSequence()
    {
        SpawnCirculo(_cercoRadiusInicial, _cercoGroupPrefabIndex);
        yield return new WaitForSeconds(_cercoDelayEntreOlas);
        SpawnCirculo(_cercoRadiusFinal, _cercoGroupPrefabIndex);
    }

    private void SpawnCirculo(float radius, int prefabIndex)
    {
        float angleStep = 360f / _cercoGroupCount;
        for (int i = 0; i < _cercoGroupCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 spawnPos = player.position + offset;
            spawnPos.y = player.position.y;
            GetBossGroup(spawnPos, prefabIndex);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PATRON 3: Linea de Fuego con Groups
    // ═════════════════════════════════════════════════════════════════════

    public void ExecuteLineaFuegoGroups()
    {
        StartCoroutine(LineaFuegoSequence());
    }

    private IEnumerator LineaFuegoSequence()
    {
        for (int i = 0; i < _lineaGroupCount; i++)
        {
            float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
            Vector3 spawnPos = player.position + new Vector3(-_lineaSpawnDistance, 0f, zOffset);
            spawnPos.y = player.position.y;
            GetBossGroup(spawnPos, _lineaGroupPrefabIndex);
        }

        yield return new WaitForSeconds(_lineaDelayEntreFilas);

        for (int i = 0; i < _lineaGroupCount; i++)
        {
            float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
            Vector3 spawnPos = player.position + new Vector3(_lineaSpawnDistance, 0f, zOffset);
            spawnPos.y = player.position.y;
            GetBossGroup(spawnPos, _lineaGroupPrefabIndex);
        }
    }

    public void ResetAll()
    {
        StopAllCoroutines();

        foreach (var group in _activeGroups.ToArray())
        {
            if (group == null) continue;
            group.GetComponent<EnemyEmerge>()?.Reset();
            group.Cleanup();
            group.gameObject.SetActive(false);
        }
        _activeGroups.Clear();

        var dashEnemies = FindObjectsByType<DashBossEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in dashEnemies)
            Destroy(e.gameObject);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═════════════════════════════════════════════════════════════════════

    //private void OnDrawGizmos()
    //{
    //    if (player == null) return;
    //    Vector3 center = player.position;

    //    float[] xOffsets = { -columnSpacing, 0f, columnSpacing };
    //    Gizmos.color = Color.cyan;
    //    foreach (float x in xOffsets)
    //    {
    //        Vector3 pos = center + new Vector3(x, 0f, spawnDistance);
    //        Gizmos.DrawSphere(pos, 0.3f);
    //        Gizmos.DrawLine(pos, pos + Vector3.back * 2f);
    //    }
    //    Gizmos.color = Color.red;
    //    foreach (float x in xOffsets)
    //    {
    //        Vector3 pos = center + new Vector3(x, 0f, -spawnDistance);
    //        Gizmos.DrawSphere(pos, 0.3f);
    //        Gizmos.DrawLine(pos, pos + Vector3.forward * 2f);
    //    }

    //    float angleStep = 360f / _cercoGroupCount;
    //    Gizmos.color = new Color(1f, 0.5f, 0f);
    //    for (int i = 0; i < _cercoGroupCount; i++)
    //    {
    //        float angle = i * angleStep * Mathf.Deg2Rad;
    //        Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _cercoRadiusInicial;
    //        Gizmos.DrawSphere(pos, 0.4f);
    //        Gizmos.DrawLine(pos, center);
    //    }
    //    Gizmos.color = Color.magenta;
    //    for (int i = 0; i < _cercoGroupCount; i++)
    //    {
    //        float angle = i * angleStep * Mathf.Deg2Rad;
    //        Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _cercoRadiusFinal;
    //        Gizmos.DrawSphere(pos, 0.4f);
    //        Gizmos.DrawLine(pos, center);
    //    }

    //    Gizmos.color = Color.green;
    //    for (int i = 0; i < _lineaGroupCount; i++)
    //    {
    //        float zOffset = (i - (_lineaGroupCount - 1) / 2f) * _lineaSpacing;
    //        Vector3 posL = center + new Vector3(-_lineaSpawnDistance, 0f, zOffset);
    //        Vector3 posR = center + new Vector3(_lineaSpawnDistance, 0f, zOffset);
    //        Gizmos.DrawSphere(posL, 0.4f);
    //        Gizmos.DrawLine(posL, posL + Vector3.right * 2f);
    //        Gizmos.DrawSphere(posR, 0.4f);
    //        Gizmos.DrawLine(posR, posR + Vector3.left * 2f);
    //    }

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere(center, 0.25f);
    //}
}