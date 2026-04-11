using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private BulletFactory enemyBullet;

    [Header("Group Prefabs")]
    [SerializeField] private GameObject[] _enemyGroupPrefabs;
    [SerializeField] private int _initialStockPerType = 3;

    [Header("Spawn Config")]
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private int _maxSpawnAttempts = 30;
    [SerializeField] private int _maxActiveGroups = 10;

    [Header("Spawn Area")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(200f, 200f);
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _spawnYOffset = 5.3f; // <-- esto


    [Header("Placement")]
    [SerializeField] private float _minDistanceFromPlayer = 20f;
    [SerializeField] private float _overlapCheckRadius = 5f;
    [SerializeField] private LayerMask _overlapCheckMask;

    private ObjectPool<EnemyGroup>[] _pools;
    private int _activeGroupCount = 0;

    private void Awake()
    {
        _pools = new ObjectPool<EnemyGroup>[_enemyGroupPrefabs.Length];

        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            var prefab = _enemyGroupPrefabs[i];
            _pools[i] = new ObjectPool<EnemyGroup>(
                () =>
                {
                    var go = Instantiate(prefab);
                    go.SetActive(true);
                    return go.GetComponent<EnemyGroup>();
                },
                g => g.gameObject.SetActive(true),
                g => { g.Cleanup(); g.gameObject.SetActive(false); },
                _initialStockPerType
            );
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (_activeGroupCount >= _maxActiveGroups) continue;

            Vector3? position = TryGetValidPosition();
            if (position == null) continue;

            Spawn(position.Value);
        }
    }

    private Vector3? TryGetValidPosition()
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(_camera);

        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 candidate = GetRandomPosition();

            if (IsTooCloseToPlayer(candidate)) continue;
            if (IsOverlappingSomething(candidate)) continue;
            if (IsVisibleByCamera(candidate, planes)) continue;

            return candidate;
        }

        Debug.LogWarning("EnemySpawner: no se encontró posición válida.");
        return null;
    }

    private Vector3 GetRandomPosition()
    {
        float x = _spawnCenter.position.x + Random.Range(-_spawnAreaSize.x * 0.5f, _spawnAreaSize.x * 0.5f);
        float z = _spawnCenter.position.z + Random.Range(-_spawnAreaSize.y * 0.5f, _spawnAreaSize.y * 0.5f);
        return new Vector3(x, _spawnHeight, z);
    }

    private bool IsTooCloseToPlayer(Vector3 candidate)
    {
        return Vector3.Distance(candidate, _player.position) < _minDistanceFromPlayer;
    }

    private bool IsOverlappingSomething(Vector3 candidate)
    {
        return Physics.CheckSphere(candidate, _overlapCheckRadius, _overlapCheckMask);
    }

    private bool IsVisibleByCamera(Vector3 candidate, Plane[] planes)
    {
        var testBounds = new Bounds(candidate, Vector3.one * 4f);
        return GeometryUtility.TestPlanesAABB(planes, testBounds);
    }

    private void Spawn(Vector3 position)
    {
        int randomIndex = Random.Range(0, _pools.Length);
        var group = _pools[randomIndex].Get();
        group.transform.position = position + new Vector3(0f, _spawnYOffset, 0f);
        group.OnGroupDead += ReturnToPool;
        group.Init(_player, enemyBullet);
        _activeGroupCount++;
    }

    private void ReturnToPool(EnemyGroup group)
    {
        group.OnGroupDead -= ReturnToPool;

        // Necesitamos saber a qué pool devolver — buscamos por nombre de prefab
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

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
    }
}