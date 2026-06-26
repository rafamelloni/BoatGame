using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyFactory _factory;
    [SerializeField] private PhaseManager _phaseManager;
    [SerializeField] private Transform _player;
    [SerializeField] private Rigidbody _playerRb;
    [SerializeField] private Transform _playerBow;
    [SerializeField] private Camera _camera;

    [Header("Spawn Area")]
    [SerializeField] private Transform _spawnCenter;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(200f, 200f);
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _spawnYOffset = 5.3f;
    [SerializeField] private float _spawnRangeAroundPlayer = 30f;

    [Header("Spawn Direction Weights")]
    [Tooltip("Cuánto peso tiene la dirección de movimiento del player.")]
    [SerializeField] private float _movementDirWeight = 1.5f;
    [Tooltip("Cuánto peso tiene el sector con menos enemigos.")]
    [SerializeField] private float _emptyDirWeight = 1f;
    [Tooltip("Cantidad de sectores en los que se divide el espacio alrededor del player.")]
    [SerializeField] private int _sectorCount = 8;

    [Header("Placement")]
    [SerializeField] private int _maxSpawnAttempts = 30;
    [SerializeField] private float _overlapCheckRadius = 5f;
    [SerializeField] private float _minDistanceFromPlayer = 15f;
    [SerializeField] private float _maxDistanceFromPlayer = 40f;
    [Tooltip("Distancia del ray desde la proa para calcular el centro de spawn.")]
    [SerializeField] private float _bowRayLength = 20f;
    [SerializeField] private LayerMask _overlapCheckMask;

    [Header("Min distances")]
    [SerializeField] private float _minDistanceBetweenGroups = 30f;
    [SerializeField] private float _minDistanceBetweenShips = 20f;
    [SerializeField] private float _minDistanceBetweenRafas = 15f;
    [SerializeField] private float _minDistanceBetweenZombies = 15f;

    [Header("Tumulto")]
    [SerializeField] private float _tumultoRadius = 20f;
    [SerializeField] private int _tumultoMinGroups = 3;

    [Header("Rafa")]
    [SerializeField] private float _rafaLifetime = 10f;

    [Header("Distance Culling")]
    [SerializeField] private float _maxGroupDistanceFromPlayer = 80f;
    [SerializeField] private float _distanceCheckInterval = 5f;

    // Posiciones activas
    private readonly Dictionary<EnemyGroup, Vector3> _activeGroupPositions = new();
    private readonly Dictionary<ZombieGroup, Vector3> _activeZombieGroupPositions = new();
    private readonly List<Vector3> _activeShipPositions = new();
    private readonly List<Vector3> _activeRafaPositions = new();

    private int _activeGroupCount = 0;
    private int _activeZombieGroupCount = 0;

    // Parámetros de fase actuales
    private float _groupInterval;
    private int _maxGroups;
    private int _currentGroupPrefabIndex;
    private float _shipInterval;
    private int _maxShips;
    private float _rafaInterval;
    private int _maxRafas;
    private float _zombieInterval;
    private int _maxZombies;
    private int _currentZombieGroupPrefabIndex;
    private int _tumultoMinGroupsPhase;
    private float _tumultoCheckCooldown;
    private float _tumultoActiveDuration;
    private float _tumultoSpawnInterval;
    private TumultoSpawnType _tumultoSpawnType;

    private bool _isRunning = false;
    private Coroutine _groupLoop;
    private Coroutine _shipLoop;
    private Coroutine _rafaLoop;
    private Coroutine _zombieLoop;

    private float _lastTumultoCheck;
    private float _lastTumultoSpawn;
    private float _tumultoActiveUntil;
    private bool _tumultoActive;
    private float _lastDistanceCheck;

    private void Awake()
    {
        _phaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        _phaseManager.OnPhaseChanged -= OnPhaseChanged;
    }

    private void Update()
    {
        if (!_isRunning) return;

        if (Time.time >= _lastDistanceCheck + _distanceCheckInterval)
        {
            _lastDistanceCheck = Time.time;
            DespawnFarGroups();
        }

        if (_tumultoActive)
        {
            if (Time.time >= _tumultoActiveUntil)
            {
                _tumultoActive = false;
                _lastTumultoCheck = Time.time;
            }
            else if (Time.time >= _lastTumultoSpawn + _tumultoSpawnInterval)
            {
                SpawnTumulto();
                _lastTumultoSpawn = Time.time;
            }
        }
        else
        {
            if (Time.time >= _lastTumultoCheck + _tumultoCheckCooldown)
            {
                if (IsTumulto(out _))
                {
                    _tumultoActive = true;
                    _tumultoActiveUntil = Time.time + _tumultoActiveDuration;
                    _lastTumultoSpawn = 0f;
                }
                else
                {
                    _lastTumultoCheck = Time.time;
                }
            }
        }
    }

    // ——— Control externo ———

    public void StartSpawning()
    {
        if (!gameObject.activeSelf) return;
        StopAllLoops();
        _isRunning = true;
        _phaseManager.StartSession();
    }

    public void StopSpawning()
    {
        _isRunning = false;
        _phaseManager.StopSession();
        StopAllLoops();
    }

    public void ResumeSpawning()
    {
        if (!gameObject.activeSelf) return;
        if (_isRunning) return;
        _isRunning = true;
        _phaseManager.ResumeSession();
        RestartLoops();
    }

    public void DespawnAll()
    {
        StopSpawning();
        DespawnGroups();
        DespawnZombieGroups();
        _factory.DespawnAll<ShipEnemy>();
        _factory.DespawnAll<RafaEnemy>();
        _activeShipPositions.Clear();
        _activeRafaPositions.Clear();
    }

    public void DespawnAllAndRestart()
    {
        DespawnAll();
        StartSpawning();
    }

    // ——— Fase ———

    private void OnPhaseChanged(SpawnPhase phase)
    {
        _factory.ApplyPhase(phase);

        _groupInterval = phase.groupSpawnInterval;
        _maxGroups = phase.maxActiveGroups;
        _currentGroupPrefabIndex = phase.groupPrefabIndex;
        _shipInterval = phase.shipSpawnInterval;
        _maxShips = phase.maxActiveShips;
        _rafaInterval = phase.rafaSpawnInterval;
        _maxRafas = phase.maxActiveRafas;
        _zombieInterval = phase.zombieSpawnInterval;
        _maxZombies = phase.maxActiveZombies;
        _currentZombieGroupPrefabIndex = phase.zombieGroupPrefabIndex;
        _tumultoMinGroupsPhase = phase.tumultoMinGroups > 0 ? phase.tumultoMinGroups : _tumultoMinGroups;
        _tumultoCheckCooldown = phase.tumultoCheckCooldown;
        _tumultoActiveDuration = phase.tumultoActiveDuration;
        _tumultoSpawnInterval = phase.tumultoSpawnInterval;
        _tumultoSpawnType = phase.tumultoSpawnType;

        _lastTumultoCheck = Time.time;
        _lastTumultoSpawn = Time.time;
        _tumultoActive = false;

        if (phase.coinsToFill > 0)
            CoinManager.Instance.SetCoinsRequired(phase.coinsToFill);

        if (_isRunning)
            RestartLoops();
    }

    // ——— Distance culling ———

    private void DespawnFarGroups()
    {
        var toRemove = new List<EnemyGroup>();
        foreach (var kvp in _activeGroupPositions)
        {
            if (Vector3.Distance(kvp.Key.transform.position, _player.position) > _maxGroupDistanceFromPlayer)
                toRemove.Add(kvp.Key);
        }
        foreach (var group in toRemove)
        {
            group.OnGroupDead -= ReturnGroup;
            group.OnGroupAlmostDead -= ReleaseGroupSlot;
            _activeGroupPositions.Remove(group);
            _activeGroupCount--;
            _factory.ReturnGroup(group);
        }

        var toRemoveZ = new List<ZombieGroup>();
        foreach (var kvp in _activeZombieGroupPositions)
        {
            if (Vector3.Distance(kvp.Key.transform.position, _player.position) > _maxGroupDistanceFromPlayer)
                toRemoveZ.Add(kvp.Key);
        }
        foreach (var group in toRemoveZ)
        {
            group.OnGroupDead -= ReturnZombieGroup;
            group.OnGroupAlmostDead -= ReleaseZombieGroupSlot;
            _activeZombieGroupPositions.Remove(group);
            _activeZombieGroupCount--;
            _factory.ReturnZombieGroup(group);
        }
    }

    // ——— Tumulto spawn ———

    private void SpawnTumulto()
    {
        Vector3 spawnDir = IsTumulto(out Vector3 oppositeDir) ? oppositeDir : Vector3.zero;

        switch (_tumultoSpawnType)
        {
            case TumultoSpawnType.Group:
                SpawnGroupAroundPlayer(spawnDir);
                break;
            case TumultoSpawnType.Ship:
                SpawnShipAroundPlayer(spawnDir);
                break;
            case TumultoSpawnType.Rafa:
                SpawnRafaAroundPlayer(spawnDir);
                break;
            case TumultoSpawnType.ZombieGroup:
                SpawnZombieGroupAroundPlayer(spawnDir);
                break;
        }
    }

    private void SpawnGroupAroundPlayer(Vector3 directionBias)
    {
        var pos = TryGetValidPositionWithOffset(_ => false, directionBias);
        if (pos == null) return;
        RegisterGroup(_factory.GetGroup(pos.Value, _spawnYOffset, _currentGroupPrefabIndex));
    }

    private void SpawnShipAroundPlayer(Vector3 directionBias)
    {
        var pos = TryGetValidPositionWithOffset(_ => false, directionBias);
        if (pos == null) return;
        var ship = _factory.Get<ShipEnemy>(pos.Value, _spawnYOffset);
        _activeShipPositions.Add(ship.transform.position);
        ship.OnDead += OnShipReturned;
    }

    private void SpawnRafaAroundPlayer(Vector3 directionBias)
    {
        var pos = TryGetValidPositionWithOffset(_ => false, directionBias);
        if (pos == null) return;
        RegisterRafa(_factory.Get<RafaEnemy>(pos.Value, _spawnYOffset));
    }

    private void SpawnZombieGroupAroundPlayer(Vector3 directionBias)
    {
        var pos = TryGetValidPositionWithOffset(_ => false, directionBias);
        if (pos == null) return;
        RegisterZombieGroup(_factory.GetZombieGroup(pos.Value, _spawnYOffset, _currentZombieGroupPrefabIndex));
    }

    // ——— Loops de spawn ———

    private void RestartLoops()
    {
        StopAllLoops();
        _groupLoop = StartCoroutine(SpawnLoop(_groupInterval, () => _activeGroupCount < _maxGroups, SpawnGroup));
        _shipLoop = StartCoroutine(SpawnLoop(_shipInterval, () => _activeShipPositions.Count < _maxShips, SpawnShip));
        _rafaLoop = StartCoroutine(SpawnLoop(_rafaInterval, () => _activeRafaPositions.Count < _maxRafas, SpawnRafa));
        _zombieLoop = StartCoroutine(SpawnLoop(_zombieInterval, () => _activeZombieGroupCount < _maxZombies, SpawnZombieGroup));
    }

    private void StopAllLoops()
    {
        if (_groupLoop != null) { StopCoroutine(_groupLoop); _groupLoop = null; }
        if (_shipLoop != null) { StopCoroutine(_shipLoop); _shipLoop = null; }
        if (_rafaLoop != null) { StopCoroutine(_rafaLoop); _rafaLoop = null; }
        if (_zombieLoop != null) { StopCoroutine(_zombieLoop); _zombieLoop = null; }
    }

    private IEnumerator SpawnLoop(float interval, Func<bool> canSpawn, Action spawnAction)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, interval));
        if (canSpawn()) spawnAction();
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (!canSpawn()) continue;
            spawnAction();
        }
    }

    // ——— Spawn regular ———

    private void SpawnGroup()
    {
        var pos = TryGetValidPosition(IsTooCloseToGroup);
        if (pos == null) return;
        RegisterGroup(_factory.GetGroup(pos.Value, _spawnYOffset, _currentGroupPrefabIndex));
    }

    private void RegisterGroup(EnemyGroup group)
    {
        group.OnGroupDead += ReturnGroup;
        group.OnGroupAlmostDead += ReleaseGroupSlot;
        _activeGroupPositions[group] = group.transform.position;
        _activeGroupCount++;
    }

    private void ReleaseGroupSlot(EnemyGroup group)
    {
        group.OnGroupAlmostDead -= ReleaseGroupSlot;
        _activeGroupPositions.Remove(group);
        _activeGroupCount--;
    }

    private void ReturnGroup(EnemyGroup group)
    {
        group.OnGroupDead -= ReturnGroup;
        _activeGroupPositions.Remove(group);
        _factory.ReturnGroup(group);
    }

    private void SpawnShip()
    {
        var pos = TryGetValidPosition(IsTooCloseToShip);
        if (pos == null) return;
        var ship = _factory.Get<ShipEnemy>(pos.Value, _spawnYOffset);
        _activeShipPositions.Add(ship.transform.position);
        ship.OnDead += OnShipReturned;
    }

    private void OnShipReturned(ShipEnemy ship)
    {
        ship.OnDead -= OnShipReturned;
        _activeShipPositions.Remove(ship.transform.position);
    }

    private void SpawnRafa()
    {
        var pos = TryGetValidPosition(IsTooCloseToRafa);
        if (pos == null) return;
        RegisterRafa(_factory.Get<RafaEnemy>(pos.Value, _spawnYOffset));
    }

    private void RegisterRafa(RafaEnemy rafa)
    {
        _activeRafaPositions.Add(rafa.transform.position);
        rafa.OnDead += OnRafaReturned;
        StartCoroutine(RafaLifetimeCoroutine(rafa));
    }

    private void OnRafaReturned(RafaEnemy rafa)
    {
        rafa.OnDead -= OnRafaReturned;
        _activeRafaPositions.Remove(rafa.transform.position);
    }

    private IEnumerator RafaLifetimeCoroutine(RafaEnemy rafa)
    {
        yield return new WaitForSeconds(_rafaLifetime);
        if (rafa != null && rafa.gameObject.activeInHierarchy)
            rafa.GetComponent<EnemyHealth>().TakeDamage(999f);
    }

    private void SpawnZombieGroup()
    {
        var pos = TryGetValidPosition(IsTooCloseToZombie);
        if (pos == null) return;
        RegisterZombieGroup(_factory.GetZombieGroup(pos.Value, _spawnYOffset, _currentZombieGroupPrefabIndex));
    }

    private void RegisterZombieGroup(ZombieGroup group)
    {
        group.OnGroupDead += ReturnZombieGroup;
        group.OnGroupAlmostDead += ReleaseZombieGroupSlot;
        _activeZombieGroupPositions[group] = group.transform.position;
        _activeZombieGroupCount++;
    }

    private void ReleaseZombieGroupSlot(ZombieGroup group)
    {
        group.OnGroupAlmostDead -= ReleaseZombieGroupSlot;
        _activeZombieGroupPositions.Remove(group);
        _activeZombieGroupCount--;
    }

    private void ReturnZombieGroup(ZombieGroup group)
    {
        group.OnGroupDead -= ReturnZombieGroup;
        _activeZombieGroupPositions.Remove(group);
        _factory.ReturnZombieGroup(group);
    }

    // ——— Despawn ———

    private void DespawnGroups()
    {
        var active = FindObjectsByType<EnemyGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var group in active)
        {
            group.OnGroupDead -= ReturnGroup;
            group.OnGroupAlmostDead -= ReleaseGroupSlot;
            _factory.ReturnGroup(group);
        }
        _activeGroupPositions.Clear();
        _activeGroupCount = 0;
    }

    private void DespawnZombieGroups()
    {
        var active = FindObjectsByType<ZombieGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var group in active)
        {
            group.OnGroupDead -= ReturnZombieGroup;
            group.OnGroupAlmostDead -= ReleaseZombieGroupSlot;
            _factory.ReturnZombieGroup(group);
        }
        _activeZombieGroupPositions.Clear();
        _activeZombieGroupCount = 0;
    }

    // ——— Tumulto ———

    private bool IsTumulto(out Vector3 oppositeDirection)
    {
        oppositeDirection = Vector3.zero;

        var positions = new List<Vector3>(_activeGroupPositions.Values);
        int minGroups = _tumultoMinGroupsPhase > 0 ? _tumultoMinGroupsPhase : _tumultoMinGroups;
        if (positions.Count < minGroups) return false;

        int grouped = 0;
        Vector3 massCenter = Vector3.zero;

        foreach (var pos in positions)
        {
            foreach (var other in positions)
            {
                if (pos == other) continue;
                if (Vector3.Distance(pos, other) < _tumultoRadius)
                {
                    grouped++;
                    massCenter += pos;
                    break;
                }
            }
        }

        if (grouped < minGroups) return false;

        massCenter /= grouped;
        Vector3 dir = _player.position - massCenter;
        dir.y = 0f;
        oppositeDirection = dir.normalized;
        return true;
    }

    // ——— Dirección inteligente de spawn ———

    private Vector3 GetSmartSpawnDirection()
    {
        Vector3 movementDir = Vector3.zero;
        if (_playerRb != null)
        {
            movementDir = _playerRb.linearVelocity;
            movementDir.y = 0f;
            if (movementDir.sqrMagnitude > 0.1f)
                movementDir.Normalize();
            else
                movementDir = Vector3.zero;
        }

        Vector3 emptySectorDir = GetLeastPopulatedSectorDirection();
        Vector3 combined = movementDir * _movementDirWeight + emptySectorDir * _emptyDirWeight;
        combined.y = 0f;

        if (combined.sqrMagnitude < 0.01f)
            combined = new Vector3(UnityEngine.Random.insideUnitCircle.x, 0f, UnityEngine.Random.insideUnitCircle.y).normalized;

        return combined.normalized;
    }

    private Vector3 GetLeastPopulatedSectorDirection()
    {
        int[] sectorCounts = new int[_sectorCount];
        float sectorAngle = 360f / _sectorCount;

        var allPositions = new List<Vector3>();
        foreach (var pos in _activeGroupPositions.Values) allPositions.Add(pos);
        foreach (var pos in _activeZombieGroupPositions.Values) allPositions.Add(pos);
        allPositions.AddRange(_activeShipPositions);
        allPositions.AddRange(_activeRafaPositions);

        foreach (var pos in allPositions)
        {
            Vector3 dir = pos - _player.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) continue;

            float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            int sector = Mathf.FloorToInt(angle / sectorAngle) % _sectorCount;
            sectorCounts[sector]++;
        }

        int leastPopulated = 0;
        for (int i = 1; i < _sectorCount; i++)
            if (sectorCounts[i] < sectorCounts[leastPopulated])
                leastPopulated = i;

        float centerAngle = (leastPopulated + 0.5f) * sectorAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(centerAngle), 0f, Mathf.Sin(centerAngle));
    }

    // ——— Posicionamiento ———

    private Vector3? TryGetValidPosition(Func<Vector3, bool> isTooClose) =>
        TryGetValidPositionWithOffset(isTooClose, Vector3.zero);

    private Vector3? TryGetValidPositionWithOffset(Func<Vector3, bool> isTooClose, Vector3 directionBias)
    {
        Vector3 spawnDir = directionBias != Vector3.zero ? directionBias : GetSmartSpawnDirection();

        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 candidate = GetPositionInDirection(spawnDir, i);
            if (IsOverlappingSomething(candidate)) continue;
            if (isTooClose(candidate)) continue;
            if (Vector3.Distance(candidate, _player.position) < _minDistanceFromPlayer) continue;
            return candidate;
        }
        Debug.LogWarning("[EnemySpawner] No se encontró posición válida.");
        return null;
    }

    private Vector3 GetPositionInDirection(Vector3 direction, int attempt)
    {
        float halfX = _spawnAreaSize.x * 0.5f;
        float halfZ = _spawnAreaSize.y * 0.5f;

        Vector3 bowForward = _playerBow != null ? _playerBow.forward : _player.forward;
        bowForward.y = 0f;
        bowForward.Normalize();
        Vector3 spawnCenter = (_playerBow != null ? _playerBow.position : _player.position) + bowForward * _bowRayLength;
        spawnCenter.y = _spawnHeight;
        spawnCenter.x = Mathf.Clamp(spawnCenter.x, _spawnCenter.position.x - halfX, _spawnCenter.position.x + halfX);
        spawnCenter.z = Mathf.Clamp(spawnCenter.z, _spawnCenter.position.z - halfZ, _spawnCenter.position.z + halfZ);

        float spreadAngle = Mathf.Min(attempt * 10f, 160f);
        float randomAngle = UnityEngine.Random.Range(-spreadAngle, spreadAngle) * Mathf.Deg2Rad;

        float cosA = Mathf.Cos(randomAngle);
        float sinA = Mathf.Sin(randomAngle);
        Vector3 spreadDir = new Vector3(
            direction.x * cosA - direction.z * sinA,
            0f,
            direction.x * sinA + direction.z * cosA
        ).normalized;

        float distance = UnityEngine.Random.Range(_minDistanceFromPlayer + 5f, _maxDistanceFromPlayer);
        Vector3 candidate = spawnCenter + spreadDir * distance;

        candidate.x = Mathf.Clamp(candidate.x, _spawnCenter.position.x - halfX, _spawnCenter.position.x + halfX);
        candidate.z = Mathf.Clamp(candidate.z, _spawnCenter.position.z - halfZ, _spawnCenter.position.z + halfZ);
        candidate.y = _spawnHeight;

        return candidate;
    }

    private bool IsOverlappingSomething(Vector3 pos) =>
        Physics.CheckSphere(pos, _overlapCheckRadius, _overlapCheckMask);

    private bool IsTooCloseToGroup(Vector3 c)
    {
        foreach (var pos in _activeGroupPositions.Values)
            if (Vector3.Distance(c, pos) < _minDistanceBetweenGroups) return true;
        return false;
    }

    private bool IsTooCloseToShip(Vector3 c)
    {
        foreach (var pos in _activeShipPositions)
            if (Vector3.Distance(c, pos) < _minDistanceBetweenShips) return true;
        return false;
    }

    private bool IsTooCloseToRafa(Vector3 c)
    {
        foreach (var pos in _activeRafaPositions)
            if (Vector3.Distance(c, pos) < _minDistanceBetweenRafas) return true;
        return false;
    }

    private bool IsTooCloseToZombie(Vector3 c)
    {
        foreach (var pos in _activeZombieGroupPositions.Values)
            if (Vector3.Distance(c, pos) < _minDistanceBetweenZombies) return true;
        return false;
    }

    // ——— Gizmos ———

    private void OnDrawGizmos()
    {
        if (_spawnCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_spawnCenter.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));

        if (_player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_player.position, _minDistanceFromPlayer);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_player.position, _maxDistanceFromPlayer);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_player.position, _maxGroupDistanceFromPlayer);
        }

        Gizmos.color = Color.cyan;
        foreach (var pos in _activeGroupPositions.Values)
            Gizmos.DrawWireSphere(pos, _tumultoRadius * 0.5f);

        if (Application.isPlaying && IsTumulto(out Vector3 dir))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_player.position, _player.position + dir * 20f);
            Gizmos.DrawWireSphere(_player.position + dir * 20f, 2f);
        }

        if (_playerBow != null)
        {
            Vector3 bowForward = _playerBow.forward;
            bowForward.y = 0f;
            bowForward.Normalize();
            Vector3 bowTip = _playerBow.position + bowForward * _bowRayLength;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(_playerBow.position, bowTip);
            Gizmos.DrawWireSphere(bowTip, 2f);
        }

        if (Application.isPlaying && _player != null)
        {
            Vector3 smartDir = GetSmartSpawnDirection();
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_player.position, _player.position + smartDir * 15f);
            Gizmos.DrawWireSphere(_player.position + smartDir * 15f, 1.5f);
        }
    }
}