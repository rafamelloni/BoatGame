using System;
using System.Collections.Generic;
using UnityEngine;
using static EnemySpawner;

public class EnemyFactory : MonoBehaviour
{
    [Serializable]
    private struct GroupPoolConfig
    {
        public GameObject prefab;
        public int initialStock;
    }

    [Header("Group")]
    [SerializeField] private GroupPoolConfig[] _groupConfigs;

    [Header("Ship")]
    [SerializeField] private GameObject _shipPrefab;
    [SerializeField] private int _initialShipStock = 3;

    [Header("Rafa")]
    [SerializeField] private GameObject _rafaPrefab;
    [SerializeField] private int _initialRafaStock = 3;

    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _playerAimPo;
    [SerializeField] private BulletFactory _enemyBullet;

    // Pools por tipo
    private readonly Dictionary<Type, object> _pools = new();

    // Grupos: múltiples pools, uno por prefab
    private ObjectPool<EnemyGroup>[] _groupPools;
    private GameObject[] _groupPrefabs;

    // Fase actual cacheada para health overrides
    private SpawnPhase _currentPhase;

    private void Awake()
    {
        InitGroupPools();
        InitPool<ShipEnemy>(_shipPrefab, _initialShipStock);
        InitPool<RafaEnemy>(_rafaPrefab, _initialRafaStock);
    }

    // ——— API pública ———

    public void ApplyPhase(SpawnPhase phase)
    {
        _currentPhase = phase;
    }

    /// Obtiene un enemigo del pool y aplica su setup.
    public T Get<T>(Vector3 position, float yOffset) where T : MonoBehaviour
    {
        var pool = GetPool<T>();
        var instance = pool.Get();
        instance.transform.position = position + new Vector3(0f, yOffset, 0f);
        instance.gameObject.SetActive(true);
        ApplySetup(instance);
        ApplyPhaseHealth(instance);
        return instance;
    }

    /// Obtiene un EnemyGroup aleatorio del pool.
    public EnemyGroup GetGroup(Vector3 position, float yOffset, int prefabIndex = -1)
    {
        int idx = prefabIndex >= 0 && prefabIndex < _groupPools.Length
            ? prefabIndex
            : UnityEngine.Random.Range(0, _groupPools.Length);

        var group = _groupPools[idx].Get();
        group.transform.position = position + new Vector3(0f, yOffset, 0f);
        group.gameObject.SetActive(true);

        if (_currentPhase.groupHealth > 0)
        {
            foreach (var health in group.GetComponentsInChildren<EnemyHealth>(true))
                health.SetMaxHealth(_currentPhase.groupHealth);
        }

        group.Init(_player, _enemyBullet);
        return group;
    }

    /// Devuelve un enemigo al pool.
    public void Return<T>(T instance) where T : MonoBehaviour
    {
        RemoveSetup(instance);
        var pool = GetPool<T>();
        pool.Return(instance);
    }

    /// Devuelve un EnemyGroup al pool correcto.
    public void ReturnGroup(EnemyGroup group)
    {
        group.Cleanup();
        for (int i = 0; i < _groupPrefabs.Length; i++)
        {
            if (group.gameObject.name.Contains(_groupPrefabs[i].name))
            {
                _groupPools[i].Return(group);
                return;
            }
        }
        Debug.LogWarning($"[EnemyFactory] No se encontró pool para grupo '{group.name}'");
    }

    /// Despawnea todos los enemigos de un tipo.
    public void DespawnAll<T>() where T : MonoBehaviour
    {
        var active = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var instance in active)
            Return(instance);
    }

    public void DespawnAllGroups()
    {
        var active = FindObjectsByType<EnemyGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var group in active)
            ReturnGroup(group);
    }

    // ——— Setup por tipo ———

    private void ApplySetup<T>(T instance) where T : MonoBehaviour
    {
        switch (instance)
        {
            case ShipEnemy ship:
                ship.SetPlayer(_player, _playerAimPo);
                ship.OnDead += OnShipDead;
                break;
            case RafaEnemy rafa:
                rafa.SetPlayer(_player);
                rafa.OnDead += OnRafaDead;
                break;
        }
    }

    private void RemoveSetup<T>(T instance) where T : MonoBehaviour
    {
        switch (instance)
        {
            case ShipEnemy ship:
                ship.OnDead -= OnShipDead;
                break;
            case RafaEnemy rafa:
                rafa.OnDead -= OnRafaDead;
                break;
        }
        instance.gameObject.SetActive(false);
    }

    private void ApplyPhaseHealth<T>(T instance) where T : MonoBehaviour
    {
        var health = instance.GetComponent<EnemyHealth>();
        if (health == null) return;

        float phaseHealth = instance switch
        {
            ShipEnemy => _currentPhase.shipHealth,
            RafaEnemy => _currentPhase.rafaHealth,
            _ => 0f
        };

        if (phaseHealth > 0)
            health.SetMaxHealth(phaseHealth);
    }

    // ——— Callbacks de muerte ———

    private void OnShipDead(ShipEnemy ship) => Return(ship);
    private void OnRafaDead(RafaEnemy rafa) => Return(rafa);

    // ——— Init ———

    private void InitGroupPools()
    {
        _groupPools = new ObjectPool<EnemyGroup>[_groupConfigs.Length];
        _groupPrefabs = new GameObject[_groupConfigs.Length];
        for (int i = 0; i < _groupConfigs.Length; i++)
        {
            var config = _groupConfigs[i];
            _groupPrefabs[i] = config.prefab;
            _groupPools[i] = new ObjectPool<EnemyGroup>(
                () => { var go = Instantiate(config.prefab); go.SetActive(false); return go.GetComponent<EnemyGroup>(); },
                g => g.gameObject.SetActive(true),
                g => {g.gameObject.SetActive(false); },
                config.initialStock
            );
        }
    }

    private void InitPool<T>(GameObject prefab, int stock) where T : MonoBehaviour
    {
        var pool = new ObjectPool<T>(
            () => { var go = Instantiate(prefab); go.SetActive(false); return go.GetComponent<T>(); },
            _ => { },
            t => t.gameObject.SetActive(false),
            stock
        );
        _pools[typeof(T)] = pool;
    }

    private ObjectPool<T> GetPool<T>() where T : MonoBehaviour
    {
        if (_pools.TryGetValue(typeof(T), out var raw))
            return (ObjectPool<T>)raw;
        throw new InvalidOperationException($"[EnemyFactory] No hay pool registrado para {typeof(T).Name}");
    }
}