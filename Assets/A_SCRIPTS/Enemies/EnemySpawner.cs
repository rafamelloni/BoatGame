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
    [SerializeField] private float _spawnRangeAroundPlayer = 40f;


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

            if (IsOverlappingSomething(candidate)) continue;
            if (IsVisibleByCamera(candidate)) continue;

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


    private bool IsOverlappingSomething(Vector3 candidate)
    {
        return Physics.CheckSphere(candidate, _overlapCheckRadius, _overlapCheckMask);
    }

    private bool IsVisibleByCamera(Vector3 candidate)
    {
        Vector3 viewportPoint = _camera.WorldToViewportPoint(candidate);
        return viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
               viewportPoint.y >= 0f && viewportPoint.y <= 1f &&
               viewportPoint.z > 0f;
    }

    private void Spawn(Vector3 position)
    {
        Debug.Log($"Spawning at {position}, player at {_player.position}");

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
        Debug.Log($"ReturnToPool called for {group.gameObject.name}");
        bool found = false;

        // Necesitamos saber a qué pool devolver — buscamos por nombre de prefab
        for (int i = 0; i < _enemyGroupPrefabs.Length; i++)
        {
            Debug.Log($"Comparing '{group.gameObject.name}' with '{_enemyGroupPrefabs[i].name}'");

            if (group.gameObject.name.Contains(_enemyGroupPrefabs[i].name))
            {
                _pools[i].Return(group);
                found = true;
                break;
            }
        }
        if (!found) Debug.LogError($"Pool not found for: {group.gameObject.name}");

        _activeGroupCount--;
    }

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
    }
}