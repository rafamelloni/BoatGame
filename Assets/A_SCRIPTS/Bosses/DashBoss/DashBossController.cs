using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashBossController : Enemy
{
    [Header("References")]
    [SerializeField] private DashBossMovement _movement;
    [SerializeField] private DashBossShoot _shoot;

    [Header("Dash Settings")]
    [SerializeField] private float _telegraphDuration = 0.8f;
    [SerializeField] private float _timeBetweenDashes = 2f;

    [Header("Special Attack")]
    [SerializeField] private int _specialDashCount = 3;
    [SerializeField] private float _specialCooldown = 8f;

    [Header("Circle Special")]
    [SerializeField] private int _circleDashCount = 5;
    [SerializeField] private float _circleDashRadius = 4f;
    [SerializeField] private float _circleTelegraphSpawnDuration = 1f;
    [SerializeField] private float _circleSpecialCooldown = 12f;
    [SerializeField] private float _circleDashDuration = 20f;
    [SerializeField] private float _circleMinDistance = 6f;
    [SerializeField] private float _betweenDashes;

    [Header("Dash Circle Indicator")]
    [SerializeField] private DashCircleIndicator _dashCirclePrefab;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _dashImpactParticles;
    [SerializeField] private GameObject _dashFirePrefab;
    [SerializeField] private float _fireYOffset = 0.5f;

    [SerializeField] private Transform player;

    private Transform _player;
    private float _nextDashTime;
    private float _nextSpecialTime;
    private float _nextCircleSpecialTime;
    private bool _isBusy;

    private readonly List<DashCircleIndicator> _activeIndicators = new();

    private void Awake()
    {
        base.Awake();
        _enemyHealth.OnDeath += OnDeath;
        SetPlayer(player);
    }

    public void SetPlayer(Transform p)
    {
        _player = p;
        _movement.SetPlayer(p);
        _shoot.SetPlayer(p);
        _nextSpecialTime = Time.time + _specialCooldown;
        _nextCircleSpecialTime = Time.time + _circleSpecialCooldown;
    }

    private void Update()
    {
        if (_player == null) return;
        if (!_isBusy)
        {
            _movement.RotateBroadside();
            _shoot.TryShoot();
        }
        if (_isBusy) return;

        if (Time.time >= _nextCircleSpecialTime)
            StartCoroutine(DoCircleDash());
        else if (Time.time >= _nextSpecialTime)
            StartCoroutine(DoSpecialAttack());
        else if (Time.time >= _nextDashTime)
            StartCoroutine(DoDash());
    }

    private void OnDeath()
    {
        StopAllCoroutines();
        foreach (var indicator in _activeIndicators)
            if (indicator != null)
                Destroy(indicator.gameObject);
        _activeIndicators.Clear();
    }

    // ─────────────────────────────────────────────
    //  Helpers para el indicador
    // ─────────────────────────────────────────────

    private DashCircleIndicator SpawnIndicator(Vector3 destination, float duration, System.Action onComplete = null)
    {
        DashCircleIndicator indicator = Instantiate(_dashCirclePrefab, destination, Quaternion.identity);
        indicator.Play(duration, onComplete);
        _activeIndicators.Add(indicator);
        return indicator;
    }

    private void DestroyIndicator(DashCircleIndicator indicator)
    {
        if (indicator != null)
        {
            _activeIndicators.Remove(indicator);
            Destroy(indicator.gameObject);
        }
    }

    // ─────────────────────────────────────────────
    //  Dash normal
    // ─────────────────────────────────────────────

    private IEnumerator DoDash()
    {
        _isBusy = true;
        _shoot.SetCanShoot(false);
        _movement.LockRotation(true);

        Vector3 playerPos = _player.position;
        Vector3 dir = (playerPos - transform.position);
        dir.y = 0f;
        Vector3 destination = playerPos + dir.normalized * _movement.StopDistance;
        destination.y = transform.position.y;

        yield return _movement.RotateToFace(destination);

        bool shouldDash = false;
        DashCircleIndicator indicator = SpawnIndicator(destination, _telegraphDuration, () => shouldDash = true);

        yield return new WaitUntil(() => shouldDash);

        DestroyIndicator(indicator);
        yield return _movement.ExecuteDash(destination);

        _movement.LockRotation(false);
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }

    // ─────────────────────────────────────────────
    //  Special attack
    // ─────────────────────────────────────────────

    private IEnumerator DoSpecialAttack()
    {
        _isBusy = true;
        for (int i = 0; i < _specialDashCount; i++)
        {
            Vector3 playerPos = _player.position;
            _movement.LockRotation(true);
            Vector3 dir = (playerPos - transform.position);
            dir.y = 0f;
            Vector3 destination = playerPos + dir.normalized * _movement.StopDistance;
            destination.y = transform.position.y;
            yield return _movement.RotateToFace(destination);
            bool shouldDash = false;
            DashCircleIndicator indicator = SpawnIndicator(destination, _telegraphDuration, () => shouldDash = true);
            yield return new WaitUntil(() => shouldDash);
            DestroyIndicator(indicator);
            yield return _movement.ExecuteDash(destination);
            _movement.LockRotation(false);
            yield return new WaitForSeconds(0.15f);
        }

        _nextSpecialTime = Time.time + _specialCooldown;
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }

    // ─────────────────────────────────────────────
    //  Circle special
    // ─────────────────────────────────────────────

    private IEnumerator DoCircleDash()
    {
        _isBusy = true;
        _shoot.SetCanShoot(false);
        _movement.LockRotation(true);

        yield return _movement.RotateToFace(_player.position);

        Vector3 playerPos = _player.position;
        Vector3 dirToPlayer = (playerPos - transform.position);
        dirToPlayer.y = 0f;
        float distToPlayer = dirToPlayer.magnitude;
        if (distToPlayer < _circleMinDistance)
            playerPos = transform.position + dirToPlayer.normalized * _circleMinDistance;

        int count = _circleDashCount;
        Vector3[] destinations = new Vector3[count];
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _circleDashRadius;
            destinations[i] = playerPos + offset;
            destinations[i].y = transform.position.y;
        }

        float spawnInterval = _circleTelegraphSpawnDuration / count;
        DashCircleIndicator[] indicators = new DashCircleIndicator[count];

        for (int i = 0; i < count; i++)
        {
            indicators[i] = SpawnIndicator(destinations[i], _telegraphDuration);
            yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForSeconds(_telegraphDuration - _circleTelegraphSpawnDuration);

        Vector3 prevPos = transform.position;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (destinations[i] - transform.position);
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir.normalized);

            DestroyIndicator(indicators[i]);

            yield return _movement.ExecuteDash(destinations[i], _circleDashDuration, revertTilt: false);
            SpawnDashFire(prevPos, destinations[i]);
            prevPos = destinations[i];

            yield return new WaitForSeconds(_betweenDashes);
        }

        yield return _movement.TiltX(0f);
        _movement.LockRotation(false);
        _nextCircleSpecialTime = Time.time + _circleSpecialCooldown;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }

    // ─────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────

    private void SpawnDashFire(Vector3 from, Vector3 to)
    {
        Vector3 center = (from + to) / 2f;
        center.y += _fireYOffset;
        Vector3 dir = (to - from);
        dir.y = 0f;
        Quaternion rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, 90f, 0f);
        GameObject p = Instantiate(_dashFirePrefab, center, rotation);
        StartCoroutine(DespawnParticles(p));
    }
    public void ResetBoss()
    {
        Debug.Log($"ResetBoss — movement:{_movement != null} shoot:{_shoot != null} enemyHealth:{_enemyHealth != null} active:{gameObject.activeInHierarchy}");

        StopAllCoroutines();
        foreach (var indicator in _activeIndicators)
            if (indicator != null) Destroy(indicator.gameObject);
        _activeIndicators.Clear();
        _isBusy = false;
        _nextDashTime = 0f;
        _nextSpecialTime = Time.time + _specialCooldown;
        _nextCircleSpecialTime = Time.time + _circleSpecialCooldown;

        Debug.Log("Antes de movement.LockRotation");
        if (_movement != null) _movement.LockRotation(false);

        Debug.Log("Antes de shoot.SetCanShoot");
        if (_shoot != null) _shoot.SetCanShoot(true);

        Debug.Log("Antes de enemyHealth");
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDeath -= OnDeath;
            _enemyHealth.OnDeath += OnDeath;
            _enemyHealth.ResetHealth();
        }

        Debug.Log("ResetBoss completo");
    }

    private IEnumerator DespawnParticles(GameObject fireP)
    {
        yield return new WaitForSeconds(3f);
        if (fireP != null)
            Destroy(fireP.gameObject);
    }
}