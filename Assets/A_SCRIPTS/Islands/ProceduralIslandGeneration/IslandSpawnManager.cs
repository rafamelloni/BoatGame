using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IslandSpawnManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private IslandSpawner _islandSpawner;
    [SerializeField] private float _spawnInterval = 30f;
    [SerializeField] private int _maxIslands = 10;

    [Header("Placement")]
    [SerializeField] private float _minDistanceBetweenIslands = 20f;
    [SerializeField] private float _minDistanceFromPlayer = 50f;
    [SerializeField] private LayerMask _overlapCheckMask;

    [Header("Spawn Area")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(200f, 200f); // ancho x largo
    [SerializeField] private float _spawnHeight = 0f;

    private List<Vector3> _spawnedPositions = new();
    private int _currentIslandCount = 0;
    private Transform _player;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
        _islandSpawner.OnIslandReturned += OnIslandRemoved;
    }


    public void StartSpawning() 
    {
        Debug.Log("StartSpawning llamado");
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }


    public IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (_currentIslandCount >= _maxIslands) continue;

            Vector3? position = TryGetValidPosition();
            if (position == null) continue;

            _islandSpawner.SpawnIslandAt(position.Value);
            _spawnedPositions.Add(position.Value);
            _currentIslandCount++;
        }
    }

    private Vector3? TryGetValidPosition()
    {
        int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = GetRandomPosition();

            if (!IsFarEnoughFromOthers(candidate)) continue;
            if (IsOverlappingSomething(candidate)) continue;
            if (IsTooCloseToPlayer(candidate)) continue;

            return candidate;
        }

        Debug.LogWarning("No se encontró posición válida para spawnear isla.");
        return null;
    }

    private bool IsTooCloseToPlayer(Vector3 candidate)
    {
        if (_player == null) return false;
        return Vector3.Distance(candidate, _player.position) < _minDistanceFromPlayer;
    }

    private Vector3 GetRandomPosition()
    {
        float x = _spawnCenter.position.x + Random.Range(-_spawnAreaSize.x * 0.5f, _spawnAreaSize.x * 0.5f);
        float z = _spawnCenter.position.z + Random.Range(-_spawnAreaSize.y * 0.5f, _spawnAreaSize.y * 0.5f);
        return new Vector3(x, _spawnHeight, z);
    }


    private bool IsFarEnoughFromOthers(Vector3 candidate)
    {
        foreach (var pos in _spawnedPositions)
        {
            if (Vector3.Distance(candidate, pos) < _minDistanceBetweenIslands)
                return false;
        }
        return true;
    }

    private bool IsOverlappingSomething(Vector3 candidate)
    {
        return Physics.CheckSphere(candidate, _minDistanceBetweenIslands * 0.5f, _overlapCheckMask);
    }

    public void OnIslandRemoved(Vector3 position)
    {
        _spawnedPositions.Remove(position);
        _currentIslandCount--;
    }

    public void ResetIslands()
    {
        StopAllCoroutines();
        _islandSpawner.OnIslandReturned -= OnIslandRemoved; // desuscribite primero
        _islandSpawner.DespawnAll();
        _islandSpawner.OnIslandReturned += OnIslandRemoved; // volvé a suscribirte
        _spawnedPositions.Clear();
        _currentIslandCount = 0;
    }   

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
    }
}
