using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnPhase
    {
        public string name;
        public float startAtSecond;

        [Header("Groups")]
        public float groupSpawnInterval;
        public int maxActiveGroups;
        public float groupHealth; // 0 = usar valor del prefab

        [Header("Ships")]
        public float shipSpawnInterval;
        public int maxActiveShips;
        public float shipHealth; // 0 = usar valor del prefab

        [Header("Rafas")]
        public float rafaSpawnInterval;
        public int maxActiveRafas;
        public float rafaHealth; // 0 = usar valor del prefab

        [Header("Coins")]
        public int coinsToFill; // 0 = no modificar
    }

    [Header("Phases")]
    [SerializeField] private SpawnPhase[] _phases;

    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _playerAimPo;
    [SerializeField] private Camera _camera;
    [SerializeField] private BulletFactory enemyBullet;

    [Header("Group Prefabs")]
    [SerializeField] private GameObject[] _enemyGroupPrefabs;
    [SerializeField] private int _initialGroupStockPerType = 3;
    [SerializeField] private float _minDistanceBetweenGroups = 30f;

    [Header("Ship Enemy")]
    [SerializeField] private GameObject _shipEnemyPrefab;
    [SerializeField] private int _initialShipStock = 3;
    [SerializeField] private float _minDistanceBetweenShips = 20f;

    [Header("Rafa Enemy")]
    [SerializeField] private GameObject _rafaEnemyPrefab;
    [SerializeField] private int _initialRafaStock = 3;
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
    [SerializeField] private float _minDistanceFromPlayer = 25f;
    [SerializeField] private LayerMask _overlapCheckMask;

    // Handlers
    private EnemySpawnHandler<ShipEnemy> _shipHandler;
    private EnemySpawnHandler<RafaEnemy> _rafaHandler;

    // Groups
    private ObjectPool<EnemyGroup>[] _groupPools;
    private int _activeGroupCount = 0;

    // FIX: diccionario en vez de lista para que Remove siempre funcione aunque el grupo se haya movido
    private readonly Dictionary<EnemyGroup, Vector3> _activeGroupPositions = new();
    private readonly List<Vector3> _activeShipPositions = new();
    private readonly List<Vector3> _activeRafaPositions = new();

    // Fase actual
    private int _currentPhaseIndex = 0;
    private float _sessionTime = 0f;
    private bool _isRunning = false;

    private SpawnPhase CurrentPhase => _phases[_currentPhaseIndex];

    private void Awake()
    {
        InitGroupPools();

        _shipHandler = new EnemySpawnHandler<ShipEnemy>(
            prefab: _shipEnemyPrefab,
            initialStock: _initialShipStock,
            minDistance: _minDistanceBetweenShips,
            activePositions: _activeShipPositions,
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
            minDistance: _minDistanceBetweenRafas,
            activePositions: _activeRafaPositions,
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
        // intencional: ResumeSpawning() se llama desde afuera
    }

    private void Update()
    {
        if (!_isRunning) return;
        if (_phases == null || _phases.Length == 0) return;

        _sessionTime += Time.deltaTime;

        int nextPhase = _currentPhaseIndex + 1;
        if (nextPhase < _phases.Length && _sessionTime >= _phases[nextPhase].startAtSecond)
        {
            _currentPhaseIndex = nextPhase;
            ApplyCurrentPhase();
            Debug.Log($"[EnemySpawner] Fase {_currentPhaseIndex}: {CurrentPhase.name}");
        }
    }

    private void ApplyCurrentPhase()
    {
        var phase = CurrentPhase;
        _shipHandler.UpdateParams(phase.shipSpawnInterval, phase.maxActiveShips);
        _rafaHandler.UpdateParams(phase.rafaSpawnInterval, phase.maxActiveRafas);
        if (phase.coinsToFill > 0)
            CoinManager.Instance.SetCoinsRequired(phase.coinsToFill);
    }

    // — GROUPS —

    private void InitGroupPools()
    {
        _groupPools = new ObjectPool<EnemyGroup>[_enemyGroupPrefabs.Length];
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            var prefab = _enemyGroupPrefabs[i];
            _groupPools[i] = new ObjectPool<EnemyGroup>(
                () => { var go = Instantiate(prefab); go.SetActive(false); return go.GetComponent<EnemyGroup>(); },
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
            yield return new WaitForSeconds(CurrentPhase.groupSpawnInterval);
            if (_activeGroupCount >= CurrentPhase.maxActiveGroups) continue;
            Vector3? pos = TryGetValidPosition(IsTooCloseToActiveGroup);
            if (pos == null) continue;
            SpawnGroup(pos.Value);
        }
    }

    private void SpawnGroup(Vector3 position)
    {
        int randomIndex = UnityEngine.Random.Range(0, _groupPools.Length);
        var group = _groupPools[randomIndex].Get();
        Vector3 finalPos = position + new Vector3(0f, _spawnYOffset, 0f);
        group.transform.position = finalPos;
        Debug.Log($"[Spawn] distancia al player: {Vector3.Distance(finalPos, _player.position):F1}");


        if (CurrentPhase.groupHealth > 0)
        {
            var members = group.GetComponentsInChildren<EnemyHealth>(true);
            foreach (var member in members)
                member.SetMaxHealth(CurrentPhase.groupHealth);
        }

        group.OnGroupDead += ReturnGroupToPool;
        group.Init(_player, enemyBullet);
        _activeGroupPositions[group] = finalPos; // FIX: diccionario keyed por referencia
        _activeGroupCount++;
    }

    private void ReturnGroupToPool(EnemyGroup group)
    {
        group.OnGroupDead -= ReturnGroupToPool;
        if (!_activeGroupPositions.ContainsKey(group)) return;
        _activeGroupPositions.Remove(group); // FIX: Remove por referencia, siempre funciona
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
        foreach (var pos in _activeGroupPositions.Values) // FIX: iterar Values del diccionario
            if (Vector3.Distance(candidate, pos) < _minDistanceBetweenGroups) return true;
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
            if (Vector3.Distance(candidate, _player.position) < _minDistanceFromPlayer) continue;
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
        float margin = 0.15f;
        return vp.x >= -margin && vp.x <= 1f + margin
            && vp.y >= -margin && vp.y <= 1f + margin
            && vp.z > 0f;
    }

    // — DESPAWN / RESUME —

    public void DespawnAllAndReset()
    {
        DespawnAll();
        ResumeSpawning();
    }

    public void DespawnAll()
    {
        _isRunning = false;
        StopAllCoroutines();
        _shipHandler.StopLoop();
        _rafaHandler.StopLoop();
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
        _activeGroupPositions.Clear();
        _activeGroupCount = 0;
    }

    public void ResumeSpawning()
    {
        if (!gameObject.activeSelf) return;
        if (_phases == null || _phases.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: no hay fases configuradas.");
            return;
        }

        StopAllCoroutines();
        _shipHandler.StopLoop();
        _rafaHandler.StopLoop();

        _currentPhaseIndex = 0;
        _sessionTime = 0f;
        _isRunning = true;

        ApplyCurrentPhase();


        StartCoroutine(GroupSpawnLoop());
        _shipHandler.StartLoop();
        _rafaHandler.StartLoop();
    }

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
        if (_player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_player.position, _minDistanceFromPlayer);
        }
    }

    // ————————————————————————————————————————————
    // HANDLER GENERICO
    // ————————————————————————————————————————————

    private class EnemySpawnHandler<T> where T : MonoBehaviour
    {
        private readonly ObjectPool<T> _pool;
        private float _spawnInterval;
        private int _maxActive;
        private readonly float _minDistance;
        private readonly Action<T> _onSpawn;
        private readonly Action<T> _onReturn;
        private readonly EnemySpawner _owner;
        private readonly List<Vector3> _activePositions;
        private int _activeCount;
        private Coroutine _loop;

        public EnemySpawnHandler(
            GameObject prefab,
            int initialStock,
            float minDistance,
            List<Vector3> activePositions,
            Action<T> onSpawn,
            Action<T> onReturn,
            EnemySpawner owner)
        {
            _minDistance = minDistance;
            _activePositions = activePositions;
            _onSpawn = onSpawn;
            _onReturn = onReturn;
            _owner = owner;

            _pool = new ObjectPool<T>(
                () => { var go = UnityEngine.Object.Instantiate(prefab); go.SetActive(false); return go.GetComponent<T>(); },
                t => { },
                t => t.gameObject.SetActive(false),
                initialStock
            );
        }

        public void UpdateParams(float spawnInterval, int maxActive)
        {
            _spawnInterval = spawnInterval;
            _maxActive = maxActive;
            if (_loop != null)
            {
                _owner.StopCoroutine(_loop);
                _loop = _owner.StartCoroutine(SpawnLoop());
            }
        }

        public void StartLoop()
        {
            _loop = _owner.StartCoroutine(SpawnLoop());
        }

        public void StopLoop()
        {
            if (_loop != null)
            {
                _owner.StopCoroutine(_loop);
                _loop = null;
            }
        }

        public void Return(T instance)
        {
            _activePositions.Remove(instance.transform.position);
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
            _activePositions.Clear();
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
            Vector3 finalPos = position + new Vector3(0f, _owner._spawnYOffset, 0f);
            instance.transform.position = finalPos;
            instance.gameObject.SetActive(true);

            var health = instance.GetComponent<EnemyHealth>();
            if (health != null)
            {
                float phaseHealth = typeof(T) == typeof(ShipEnemy)
                    ? _owner.CurrentPhase.shipHealth
                    : _owner.CurrentPhase.rafaHealth;
                if (phaseHealth > 0)
                    health.SetMaxHealth(phaseHealth);
            }

            _activePositions.Add(finalPos);
            _onSpawn(instance);
            _activeCount++;
        }

        private bool IsTooClose(Vector3 candidate)
        {
            foreach (var pos in _activePositions)
                if (Vector3.Distance(candidate, pos) < _minDistance) return true;
            return false;
        }
    }
}