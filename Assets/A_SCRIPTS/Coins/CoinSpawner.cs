// CoinSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _coinPrefab;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _chest;
    [SerializeField] private RT_PlayerStats _stats;

    [Header("Drop config")]
    [SerializeField] private int _coinsPerKill = 1;
    [SerializeField] private float _spawnRadius = 1.5f;
    [SerializeField] private float _spawnHeight = 0.5f;   // altura fija de flotaci�n
    [SerializeField] private PhaseManager _phaseManager;

    private ObjectPool<Coin> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<Coin>(
            () =>
            {
                var go = Instantiate(_coinPrefab);
                go.SetActive(false);
                var coin = go.GetComponent<Coin>();
                coin.OnCollected += ReturnToPool;
                return coin;
            },
            c => c.gameObject.SetActive(true),
            c => c.gameObject.SetActive(false),
            _coinsPerKill * 10
        );
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += SpawnCoins;
        _phaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= SpawnCoins;
        _phaseManager.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(SpawnPhase phase)
    {
        if (phase.coinsPerKill > 0)
            _coinsPerKill = phase.coinsPerKill;
    }

    private void SpawnCoins(Vector3 deathPos)
    {
        for (int i = 0; i < _coinsPerKill; i++)
        {
            Vector2 rand = Random.insideUnitCircle * _spawnRadius;
            Vector3 spawnPos = new Vector3(
                deathPos.x + rand.x,
                _spawnHeight,
                deathPos.z + rand.y
            );

            Coin coin = _pool.Get();
            coin.Init(_chest, _player, spawnPos, _stats.pickUpRange);
        }
    }

    public void SpawnFromChest(Vector3 chestPosition, int count, float minHorizontalSpeed, float maxHorizontalSpeed, float minVerticalSpeed, float maxVerticalSpeed, float launchPower = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 dir2D = Random.insideUnitCircle.normalized;
            float horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed);
            float verticalSpeed = Random.Range(minVerticalSpeed, maxVerticalSpeed);

            Vector3 launchVelocity = new Vector3(dir2D.x, 0f, dir2D.y) * horizontalSpeed + Vector3.up * verticalSpeed;
            Vector3 spawnPos = new Vector3(chestPosition.x, _spawnHeight, chestPosition.z);

            Coin coin = _pool.Get();
            coin.InitLaunched(_chest, _player, spawnPos, _stats.pickUpRange, launchVelocity, launchPower);
        }
    }

    private void ReturnToPool(Coin coin) => _pool.Return(coin);
    public void DespawnAll()
    {
        var active = FindObjectsByType<Coin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var coin in active)
            _pool.Return(coin);
    }
}