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
    [SerializeField] float _areaRadius = 100f; // CAMBIO: antes Vector2 _areaSize
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
            // CAMBIO: punto random dentro de un círculo en vez de un rectángulo
            Vector2 offset = Random.insideUnitCircle * _areaRadius;
            Vector3 candidate = new Vector3(center.x + offset.x, _spawnY, center.z + offset.y);
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
        DrawGizmoCircle(new Vector3(center.x, _spawnY, center.z), _areaRadius); // CAMBIO: antes DrawWireCube
    }
    // CAMBIO: helper nuevo para dibujar el área circular
    private static void DrawGizmoCircle(Vector3 center, float radius, int segments = 64)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
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