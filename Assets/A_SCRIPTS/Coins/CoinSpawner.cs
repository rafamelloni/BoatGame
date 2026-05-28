// CoinSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _coinPrefab;
    [SerializeField] private Transform _player;

    [Header("Drop config")]
    [SerializeField] private int _coinsPerKill = 3;
    [SerializeField] private float _spawnRadius = 1.5f;
    [SerializeField] private float _spawnHeight = 0.5f;   // altura fija de flotación

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

    private void OnEnable() => EnemyHealth.OnAnyEnemyDied += SpawnCoins;
    private void OnDisable() => EnemyHealth.OnAnyEnemyDied -= SpawnCoins;

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
            coin.Init(_player, spawnPos);
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