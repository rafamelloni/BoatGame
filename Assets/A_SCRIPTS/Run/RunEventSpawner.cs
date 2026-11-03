using System.Collections.Generic;
using UnityEngine;

public class RunEventSpawner : MonoBehaviour
{
    [Header("Plan")]
    [SerializeField] private SO_RunPlan _plan;

    [Header("Referencias")]
    [SerializeField] private TimerBoss _timer;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private Transform _player;

    [Header("Prefabs")]
    [SerializeField] private ChargeShrine _shrinePrefab;
    [SerializeField] private SpecialTower _towerPrefab;

    [Header("Mapa")]
    [Tooltip("El mismo centro que usa CircularDamageZone")]
    [SerializeField] private Transform _mapCenter;
    [SerializeField] private float _mapRadius = 78.5f;
    [SerializeField] private float _shrineEdgeMargin = 15f;
    [SerializeField] private float _towerEdgeMargin = 8f;

    [Header("Distancias")]
    [SerializeField] private float _shrineMinDistanceFromPlayer = 40f;
    [SerializeField] private float _towerMinDistanceFromPlayer = 30f;
    [SerializeField] private float _minDistanceBetweenObjects = 25f;
    [SerializeField] private int _maxAttempts = 30;

    [Header("Torres")]
    [SerializeField] private int _maxActiveTowers = 4;
    [Tooltip("Altura de la torre en el mundo")]
    [SerializeField] private float _towerY = 5.19f;

    private readonly List<RunEvent> _events = new List<RunEvent>();
    private readonly List<SpecialTower> _towers = new List<SpecialTower>();
    private ChargeShrine _shrine;
    private int _nextEvent;
    private float _lastElapsed;

    private void Awake()
    {
        if (_plan == null) return;
        _events.AddRange(_plan.events);
        _events.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void Update()
    {
        if (_plan == null || _timer == null) return;

        float elapsed = _timer.ElapsedTime;

        // El timer volvió atrás: se reinició la run
        if (elapsed < _lastElapsed - 0.01f) ResetRun();
        _lastElapsed = elapsed;

        RemoveDeadTowers();

        if (!_timer.IsRunning) return;

        while (_nextEvent < _events.Count && elapsed >= _events[_nextEvent].time)
        {
            Fire(_events[_nextEvent]);
            _nextEvent++;
        }
    }

    private void Fire(RunEvent runEvent)
    {
        switch (runEvent.type)
        {
            case RunEventType.Wave:
                if (_enemySpawner != null) _enemySpawner.TriggerWave(runEvent.amount);
                break;
            case RunEventType.Shrine:
                SpawnShrine();
                break;
            case RunEventType.Tower:
                for (int i = 0; i < runEvent.amount; i++) SpawnTower();
                break;
        }
    }

    private void SpawnShrine()
    {
        if (_shrinePrefab == null) return;

        // Si el anterior sigue activo, este se saltea: nunca hay dos a la vez
        if (_shrine != null && _shrine.gameObject.activeSelf) return;

        if (!TryGetPosition(_shrineEdgeMargin, _shrineMinDistanceFromPlayer, out Vector3 position))
        {
            Debug.LogWarning("[RunEventSpawner] No encontré lugar para el templo.");
            return;
        }

        position.y = _shrinePrefab.transform.position.y;

        if (_shrine == null)
        {
            _shrine = Instantiate(_shrinePrefab, position, _shrinePrefab.transform.rotation);
            _shrine.gameObject.SetActive(true);
        }
        else
        {
            _shrine.transform.position = position;
            _shrine.gameObject.SetActive(true);
        }
    }

    private void SpawnTower()
    {
        if (_towerPrefab == null || _towers.Count >= _maxActiveTowers) return;

        if (!TryGetPosition(_towerEdgeMargin, _towerMinDistanceFromPlayer, out Vector3 position))
        {
            Debug.LogWarning("[RunEventSpawner] No encontré lugar para la torre.");
            return;
        }

        position.y = _towerY;

        // El prefab de la torre puede estar guardado apagado
        SpecialTower tower = Instantiate(_towerPrefab, position, _towerPrefab.transform.rotation);
        tower.gameObject.SetActive(true);
        _towers.Add(tower);
    }

    private void RemoveDeadTowers()
    {
        for (int i = _towers.Count - 1; i >= 0; i--)
        {
            SpecialTower tower = _towers[i];
            if (tower == null)
            {
                _towers.RemoveAt(i);
            }
            else if (!tower.IsAlive)
            {
                // Le da tiempo a terminar de hundirse
                Destroy(tower.gameObject, 5f);
                _towers.RemoveAt(i);
            }
        }
    }

    private bool TryGetPosition(float edgeMargin, float minDistanceFromPlayer, out Vector3 position)
    {
        Vector3 center = _mapCenter != null ? _mapCenter.position : transform.position;
        float radius = Mathf.Max(_mapRadius - edgeMargin, 1f);

        for (int i = 0; i < _maxAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = new Vector3(center.x + offset.x, 0f, center.z + offset.y);

            if (_player != null && DistanceXZ(candidate, _player.position) < minDistanceFromPlayer) continue;
            if (IsTooCloseToOthers(candidate)) continue;

            position = candidate;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsTooCloseToOthers(Vector3 candidate)
    {
        if (_shrine != null && _shrine.gameObject.activeSelf &&
            DistanceXZ(candidate, _shrine.transform.position) < _minDistanceBetweenObjects)
            return true;

        foreach (SpecialTower tower in _towers)
        {
            if (tower != null && DistanceXZ(candidate, tower.transform.position) < _minDistanceBetweenObjects)
                return true;
        }
        return false;
    }

    private void ResetRun()
    {
        foreach (SpecialTower tower in _towers)
        {
            if (tower != null) Destroy(tower.gameObject);
        }
        _towers.Clear();

        if (_shrine != null) _shrine.gameObject.SetActive(false);

        _nextEvent = 0;
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
