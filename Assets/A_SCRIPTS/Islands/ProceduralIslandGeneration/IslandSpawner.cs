using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class IslandEntry
{
    public GameObject islandPrefab;
    public IslandDecorationProfile profile;
    [Range(0, 100)] public int weight = 10;
}

public class IslandSpawner : MonoBehaviour
{
    public List<IslandEntry> islands;
    [SerializeField] private GameObject _canvasExample;
    [SerializeField] private int _poolSizePerType = 3;

    [Header("Indicator")]
    [SerializeField] private RectTransform _indicatorPrefab;
    [SerializeField] private Transform _canvasParent;

    private Dictionary<GameObject, ObjectPool<GameObject>> _islandPools = new();
    private Dictionary<GameObject, IslandEntry> _prefabToEntry = new();
    private Dictionary<GameObject, (GameObject prefab, GameObject instance)> _activeIslands = new();

    private void Awake()
    {
        foreach (var entry in islands)
        {
            var prefab = entry.islandPrefab;
            _prefabToEntry[prefab] = entry;

            var pool = new ObjectPool<GameObject>(
                () => Instantiate(prefab),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                _poolSizePerType
            );

            _islandPools[prefab] = pool;
        }
    }

    public void SpawnIslandAt(Vector3 position)
    {
        IslandEntry entry = PickWeighted();
        if (entry == null) return;

        GameObject instance = _islandPools[entry.islandPrefab].Get();
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, Random.Range(0, 360f), 0));

        IslandManager manager = instance.GetComponent<IslandManager>();
        if (manager != null)
        {
            manager.SetCanvas(_canvasExample);
            manager.Init();
            manager.OnReadyToReturn += HandleIslandReturn;
        }

        // Instanciar y asignar indicador
        if (_indicatorPrefab != null && _canvasParent != null)
        {
            RectTransform arrow = Instantiate(_indicatorPrefab, _canvasParent);
            instance.GetComponent<IslandIndicator>()?.Init(arrow);
        }

        SpawnDecorations(instance, entry);
        _activeIslands[instance] = (entry.islandPrefab, instance);
    }

    private void SpawnDecorations(GameObject instance, IslandEntry entry)
    {
        var allPoints = instance.GetComponentsInChildren<IslandSpawnPoint>();

        var normalPoints = allPoints.Where(p => p.pointType != SpawnPointType.Defense);
        var defensePoints = allPoints
            .Where(p => p.pointType == SpawnPointType.Defense)
            .OrderBy(_ => Random.value)
            .Take(2);

        foreach (var point in normalPoints.Concat(defensePoints))
        {
            GameObject prefab = entry.profile.GetRandom(point.pointType);
            if (prefab == null) continue;

            GameObject spawned = Instantiate(prefab, point.transform.position,
                                             point.transform.rotation, instance.transform);
            float parentScale = instance.transform.lossyScale.x;
            float prefabScale = prefab.transform.localScale.x;
            spawned.transform.localScale = Vector3.one * (prefabScale / parentScale);
        }
    }

    public void ReturnIsland(GameObject prefab, GameObject instance)
    {
        if (!instance.activeSelf) return;

        foreach (var point in instance.GetComponentsInChildren<IslandSpawnPoint>())
        {
            foreach (Transform child in point.transform)
                Destroy(child.gameObject);
        }

        instance.GetComponent<IslandManager>()?.ResetIsland();
        _islandPools[prefab].Return(instance);
        _activeIslands.Remove(instance);
    }

    public void DespawnAll()
    {
        var toRemove = new List<(GameObject prefab, GameObject instance)>(_activeIslands.Values);
        _activeIslands.Clear();
        foreach (var kvp in toRemove)
            ReturnIsland(kvp.prefab, kvp.instance);
    }

    private void HandleIslandReturn(GameObject instance)
    {
        if (!_activeIslands.TryGetValue(instance, out var entry)) return;
        _activeIslands.Remove(instance);
        ReturnIsland(entry.prefab, instance);
    }

    private IslandEntry PickWeighted()
    {
        int total = 0;
        foreach (var i in islands) total += i.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;

        foreach (var i in islands)
        {
            cumulative += i.weight;
            if (roll < cumulative) return i;
        }

        return null;
    }
}