using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _playerAimPo;
    [SerializeField] private Camera _camera;
    [SerializeField] private BulletFactory enemyBullet;

    [Header("Group Prefabs")]
    [SerializeField] private GameObject[] _enemyGroupPrefabs;
    [SerializeField] private int _initialGroupStockPerType = 3;
    [SerializeField] private float _groupSpawnInterval = 5f;
    [SerializeField] private int _maxActiveGroups = 10;
    [SerializeField] private float _minDistanceBetweenGroups = 30f;

    [Header("Ship Enemy")]
    [SerializeField] private GameObject _shipEnemyPrefab;
    [SerializeField] private int _initialShipStock = 3;
    [SerializeField] private float _shipSpawnInterval = 8f;
    [SerializeField] private int _maxActiveShips = 5;
    [SerializeField] private float _minDistanceBetweenShips = 20f;

    [Header("Rafa Enemy")]
    [SerializeField] private GameObject _rafaEnemyPrefab;
    [SerializeField] private int _initialRafaStock = 3;
    [SerializeField] private float _rafaSpawnInterval = 6f;
    [SerializeField] private int _maxActiveRafas = 4;
    [SerializeField] private float _minDistanceBetweenRafas = 15f;

    [Header("Spawn Area")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(200f, 200f);
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _spawnYOffset = 5.3f;
    [SerializeField] private float _spawnRangeAroundPlayer = 40f;

    [Header("Placement")]
    [SerializeField] private int _maxSpawnAttempts = 30;
    [SerializeField] private float _overlapCheckRadius = 5f;
    [SerializeField] private LayerMask _overlapCheckMask;

    // Handlers
    private EnemySpawnHandler<ShipEnemy> _shipHandler;
    private EnemySpawnHandler<RafaEnemy> _rafaHandler;

    // Groups siguen aparte porque tienen pool multiple y lógica de nombre
    private ObjectPool<EnemyGroup>[] _groupPools;
    private int _activeGroupCount = 0;

    private void Awake()
    {
        InitGroupPools();

        _shipHandler = new EnemySpawnHandler<ShipEnemy>(
            prefab: _shipEnemyPrefab,
            initialStock: _initialShipStock,
            spawnInterval: _shipSpawnInterval,
            maxActive: _maxActiveShips,
            minDistance: _minDistanceBetweenShips,
            onSpawn: ship =>
            {
                ship.SetPlayer(_player, _playerAimPo);
                ship.OnDead += _shipHandler.Return;
            },
            onReturn: ship =>
            {
                ship.OnDead -= _shipHandler.Return;
            },
            owner: this
        );

        _rafaHandler = new EnemySpawnHandler<RafaEnemy>(
            prefab: _rafaEnemyPrefab,
            initialStock: _initialRafaStock,
            spawnInterval: _rafaSpawnInterval,
            maxActive: _maxActiveRafas,
            minDistance: _minDistanceBetweenRafas,
            onSpawn: rafa =>
            {
                rafa.SetPlayer(_player);
                rafa.OnDead += _rafaHandler.Return;
            },
            onReturn: rafa =>
            {
                rafa.OnDead -= _rafaHandler.Return;
            },
            owner: this
        );
    }

    private void Start()
    {
        //ResumeSpawning();
    }

    // — GROUPS —

    private void InitGroupPools()
    {
        _groupPools = new ObjectPool<EnemyGroup>[_enemyGroupPrefabs.Length];
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            var prefab = _enemyGroupPrefabs[i];
            _groupPools[i] = new ObjectPool<EnemyGroup>(
                () => { var go = Instantiate(prefab); go.SetActive(true); return go.GetComponent<EnemyGroup>(); },
                g => g.gameObject.SetActive(true),
                g => { g.Cleanup(); g.gameObject.SetActive(false); },
                _initialGroupStockPerType
            );
        }
    }

    private IEnumerator GroupSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_groupSpawnInterval);
            if (_activeGroupCount >= _maxActiveGroups) continue;
            Vector3? pos = TryGetValidPosition(IsTooCloseToActiveGroup);
            if (pos == null) continue;
            SpawnGroup(pos.Value);
        }
    }

    private void SpawnGroup(Vector3 position)
    {
        int randomIndex = UnityEngine.Random.Range(0, _groupPools.Length);
        var group = _groupPools[randomIndex].Get();
        group.transform.position = position + new Vector3(0f, _spawnYOffset, 0f);
        group.OnGroupDead += ReturnGroupToPool;
        group.Init(_player, enemyBullet);
        _activeGroupCount++;
    }

    private void ReturnGroupToPool(EnemyGroup group)
    {
        if (!group.gameObject.activeSelf) return;
        group.OnGroupDead -= ReturnGroupToPool;
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            if (group.gameObject.name.Contains(_enemyGroupPrefabs[i].name))
            {
                _groupPools[i].Return(group);
                break;
            }
        }
        _activeGroupCount--;
    }

    private bool IsTooCloseToActiveGroup(Vector3 candidate)
    {
        var groups = FindObjectsByType<EnemyGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in groups)
            if (Vector3.Distance(candidate, g.transform.position) < _minDistanceBetweenGroups) return true;
        return false;
    }

    // — COMPARTIDO —

    private Vector3? TryGetValidPosition(Func<Vector3, bool> isTooClose)
    {
        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 candidate = GetRandomPosition();
            if (IsOverlappingSomething(candidate)) continue;
            if (IsVisibleByCamera(candidate)) continue;
            if (isTooClose(candidate)) continue;
            return candidate;
        }
        Debug.LogWarning("EnemySpawner: no se encontró posición válida.");
        return null;
    }

    private Vector3 GetRandomPosition()
    {
        float halfX = _spawnAreaSize.x * 0.5f;
        float halfZ = _spawnAreaSize.y * 0.5f;
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _spawnRangeAroundPlayer;
        float x = Mathf.Clamp(_player.position.x + randomCircle.x,
            _spawnCenter.position.x - halfX, _spawnCenter.position.x + halfX);
        float z = Mathf.Clamp(_player.position.z + randomCircle.y,
            _spawnCenter.position.z - halfZ, _spawnCenter.position.z + halfZ);
        return new Vector3(x, _spawnHeight, z);
    }

    private bool IsOverlappingSomething(Vector3 candidate) =>
        Physics.CheckSphere(candidate, _overlapCheckRadius, _overlapCheckMask);

    private bool IsVisibleByCamera(Vector3 candidate)
    {
        Vector3 vp = _camera.WorldToViewportPoint(candidate);
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f;
    }

    // — DESPAWN / RESUME —

    public void DespawnAllAndReset()
    {
        DespawnAll();
        ResumeSpawning();
    }

    public void DespawnAll()
    {
        StopAllCoroutines();
        DespawnGroups();
        _shipHandler.DespawnAll();
        _rafaHandler.DespawnAll();
    }

    private void DespawnGroups()
    {
        var groups = FindObjectsByType<EnemyGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var group in groups)
        {
            group.OnGroupDead -= ReturnGroupToPool;
            for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
            {
                if (group.gameObject.name.Contains(_enemyGroupPrefabs[i].name))
                {
                    _groupPools[i].Return(group);
                    break;
                }
            }
        }
        _activeGroupCount = 0;
    }

    public void ResumeSpawning()
    {
        StartCoroutine(GroupSpawnLoop());
        _shipHandler.StartLoop();
        _rafaHandler.StartLoop();
    }

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
    }

    // ————————————————————————————————————————————
    // HANDLER GENERICO
    // ————————————————————————————————————————————

    private class EnemySpawnHandler<T> where T : MonoBehaviour
    {
        private readonly ObjectPool<T> _pool;
        private readonly float _spawnInterval;
        private readonly int _maxActive;
        private readonly float _minDistance;
        private readonly Action<T> _onSpawn;
        private readonly Action<T> _onReturn;
        private readonly EnemySpawner _owner;
        private int _activeCount;
        private Coroutine _loop;

        public EnemySpawnHandler(
            GameObject prefab,
            int initialStock,
            float spawnInterval,
            int maxActive,
            float minDistance,
            Action<T> onSpawn,
            Action<T> onReturn,
            EnemySpawner owner)
        {
            _spawnInterval = spawnInterval;
            _maxActive = maxActive;
            _minDistance = minDistance;
            _onSpawn = onSpawn;
            _onReturn = onReturn;
            _owner = owner;

            _pool = new ObjectPool<T>(
                () => { var go = UnityEngine.Object.Instantiate(prefab); go.SetActive(false); return go.GetComponent<T>(); },
                t => t.gameObject.SetActive(true),
                t => t.gameObject.SetActive(false),
                initialStock
            );
        }

        public void StartLoop()
        {
            _loop = _owner.StartCoroutine(SpawnLoop());
        }

        public void StopLoop()
        {
            if (_loop != null)
                _owner.StopCoroutine(_loop);
        }

        public void Return(T instance)
        {
            _onReturn(instance);
            _pool.Return(instance);
            _activeCount--;
        }

        public void DespawnAll()
        {
            StopLoop();
            var active = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var instance in active)
            {
                _onReturn(instance);
                _pool.Return(instance);
            }
            _activeCount = 0;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);
                if (_activeCount >= _maxActive) continue;
                Vector3? pos = _owner.TryGetValidPosition(IsTooClose);
                if (pos == null) continue;
                Spawn(pos.Value);
            }
        }

        private void Spawn(Vector3 position)
        {
            var instance = _pool.Get();
            instance.transform.position = position + new Vector3(0f, _owner._spawnYOffset, 0f);
            _onSpawn(instance);
            _activeCount++;
        }

        private bool IsTooClose(Vector3 candidate)
        {
            var active = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in active)
                if (Vector3.Distance(candidate, t.transform.position) < _minDistance) return true;
            return false;
        }
    }
}