// LifeBoxSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class LifeBoxSpawner : MonoBehaviour
{
    [Header("Prefabs & UI")]
    [SerializeField] LifeBox _lifeBoxPrefab;
    [SerializeField] RectTransform _indicatorPrefab;
    [SerializeField] Canvas _uiCanvas;

    [Header("Spawn Config")]
    [SerializeField] int _spawnCount = 3;
    [SerializeField] int _maxActiveCrates = 2;
    [SerializeField] Transform _areaCenter;
    [SerializeField] Vector2 _areaSize = new Vector2(200f, 200f);
    [SerializeField] float _spawnY = 0f;
    [SerializeField] LayerMask _overlapCheckLayers;
    [SerializeField] float _overlapRadius = 2f;
    [SerializeField] int _maxPlacementAttempts = 10;
    [SerializeField] float _minDistanceFromPlayer = 30f;  // <--
    [SerializeField] Transform _player;

    [Header("Auto Spawn")]
    [SerializeField] RT_PlayerHealth _playerHealth;
    [SerializeField] float _lowHealthThreshold = 30f;
    [SerializeField] float _autoSpawnCooldown = 20f;

    float _lastAutoSpawnTime = -999f;
    bool _wasLowHealth = false;
    List<LifeBox> _activeCrates = new();

    void Update()
    {
        

        CheckAutoSpawn();
    }

    void CheckAutoSpawn()
    {
        if (_playerHealth == null) return;

        bool isLowHealth = _playerHealth.CurrentHealth <= _lowHealthThreshold;

        if (isLowHealth && Time.time - _lastAutoSpawnTime >= _autoSpawnCooldown)
        {
            SpawnBatch();
            _lastAutoSpawnTime = Time.time;
        }
    }

    public void SpawnBatch()
    {
        // Limpiar referencias nulas (cajas ya recogidas)
        _activeCrates.RemoveAll(c => c == null);

        int available = _maxActiveCrates - _activeCrates.Count;
        if (available <= 0) return;

        int toSpawn = Mathf.Min(_spawnCount, available);
        Vector3 center = _areaCenter != null ? _areaCenter.position : Vector3.zero;

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3? pos = GetValidPosition(center);
            if (pos == null)
            {
                Debug.LogWarning("LifeBoxSpawner: no se encontró posición válida");
                continue;
            }

            LifeBox box = Instantiate(_lifeBoxPrefab, pos.Value, Quaternion.identity);
            RectTransform indicator = Instantiate(_indicatorPrefab, _uiCanvas.transform);
            box.InitIndicator(indicator);
            _activeCrates.Add(box);
        }
    }

    Vector3? GetValidPosition(Vector3 center)
    {
        for (int attempt = 0; attempt < _maxPlacementAttempts; attempt++)
        {
            float x = Random.Range(center.x - _areaSize.x / 2f, center.x + _areaSize.x / 2f);
            float z = Random.Range(center.z - _areaSize.y / 2f, center.z + _areaSize.y / 2f);
            Vector3 candidate = new Vector3(x, _spawnY, z);

            if (_player != null)
            {
                float distToPlayer = Vector2.Distance(
                    new Vector2(candidate.x, candidate.z),
                    new Vector2(_player.position.x, _player.position.z)
                );
                if (distToPlayer < _minDistanceFromPlayer) continue;
            }

            if (Physics.OverlapSphere(candidate, _overlapRadius, _overlapCheckLayers).Length == 0)
                return candidate;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = _areaCenter != null ? _areaCenter.position : Vector3.zero;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3(center.x, _spawnY, center.z),
            new Vector3(_areaSize.x, 1f, _areaSize.y)
        );
    }

    public void ClearAllCrates()
    {
        _activeCrates.RemoveAll(c => c == null);

        foreach (LifeBox box in _activeCrates)
            box.ForceDestroy();

        _activeCrates.Clear();
        _wasLowHealth = false;
        _lastAutoSpawnTime = -999f;
    }
}