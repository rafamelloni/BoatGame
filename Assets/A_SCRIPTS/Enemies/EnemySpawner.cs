using System.Collections;
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
    [SerializeField] private int _initialStockPerType = 3;

    [Header("Ship Prefab")]
    [SerializeField] private GameObject _shipEnemyPrefab;
    [SerializeField] private int _initialShipStock = 3;

    [Header("Spawn Config (Groups)")]
    [SerializeField] private float _groupSpawnInterval = 5f;
    [SerializeField] private int _maxActiveGroups = 10;

    [Header("Spawn Config (Ships)")]
    [SerializeField] private float _shipSpawnInterval = 8f;
    [SerializeField] private int _maxActiveShips = 5;

    [Header("Spawn Area")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(200f, 200f);
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _spawnYOffset = 5.3f;
    [SerializeField] private float _spawnRangeAroundPlayer = 40f;

    [Header("Placement")]
    [SerializeField] private int _maxSpawnAttempts = 30;
    [SerializeField] private float _minDistanceFromPlayer = 20f;
    [SerializeField] private float _overlapCheckRadius = 5f;
    [SerializeField] private LayerMask _overlapCheckMask;
    [SerializeField] private float _minDistanceBetweenGroups = 30f;
    [SerializeField] private float _minDistanceBetweenShips = 20f;

    private ObjectPool<EnemyGroup>[] _pools;
    private ObjectPool<ShipEnemy> _shipPool;
    private int _activeGroupCount = 0;
    private int _activeShipCount = 0;

    private void Awake()
    {
        _pools = new ObjectPool<EnemyGroup>[_enemyGroupPrefabs.Length];
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            var prefab = _enemyGroupPrefabs[i];
            _pools[i] = new ObjectPool<EnemyGroup>(
                () => { var go = Instantiate(prefab); go.SetActive(true); return go.GetComponent<EnemyGroup>(); },
                g => g.gameObject.SetActive(true),
                g => { g.Cleanup(); g.gameObject.SetActive(false); },
                _initialStockPerType
            );
        }

        _shipPool = new ObjectPool<ShipEnemy>(
            () => { var go = Instantiate(_shipEnemyPrefab); go.SetActive(false); return go.GetComponent<ShipEnemy>(); },
            s => s.gameObject.SetActive(true),
            s => s.gameObject.SetActive(false),
            _initialShipStock
        );
    }

    private void Start()
    {
        StartCoroutine(GroupSpawnLoop());
        StartCoroutine(ShipSpawnLoop());
    }

    // — GROUPS —

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
        int randomIndex = Random.Range(0, _pools.Length);
        var group = _pools[randomIndex].Get();
        group.transform.position = position + new Vector3(0f, _spawnYOffset, 0f);
        group.OnGroupDead += ReturnGroupToPool;
        group.Init(_player, enemyBullet);
        _activeGroupCount++;
    }

    private void ReturnGroupToPool(EnemyGroup group)
    {
        group.OnGroupDead -= ReturnGroupToPool;
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            if (group.gameObject.name.Contains(_enemyGroupPrefabs[i].name))
            {
                _pools[i].Return(group);
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

    // — SHIPS —

    private IEnumerator ShipSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_shipSpawnInterval);
            if (_activeShipCount >= _maxActiveShips) continue;
            Vector3? pos = TryGetValidPosition(IsTooCloseToActiveShip);
            if (pos == null) continue;
            SpawnShip(pos.Value);
        }
    }

    private void SpawnShip(Vector3 position)
    {
        var ship = _shipPool.Get();
        ship.transform.position = position + new Vector3(0f, _spawnYOffset, 0f);
        ship.SetPlayer(_player, _playerAimPo);
        ship.OnDead += ReturnShipToPool;
        _activeShipCount++;
    }

    private void ReturnShipToPool(ShipEnemy ship)
    {
        ship.OnDead -= ReturnShipToPool;
        _shipPool.Return(ship);
        _activeShipCount--;
    }

    private bool IsTooCloseToActiveShip(Vector3 candidate)
    {
        var ships = FindObjectsByType<ShipEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in ships)
            if (Vector3.Distance(candidate, s.transform.position) < _minDistanceBetweenShips) return true;
        return false;
    }

    // — COMPARTIDO —

    private Vector3? TryGetValidPosition(System.Func<Vector3, bool> isTooClose)
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
        Vector2 randomCircle = Random.insideUnitCircle * _spawnRangeAroundPlayer;
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

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
    }
}