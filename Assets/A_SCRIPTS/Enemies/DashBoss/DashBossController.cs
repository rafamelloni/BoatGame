using System.Collections;
using UnityEngine;

public class DashBossController : Enemy
{
    [Header("References")]
    [SerializeField] private DashBossMovement _movement;
    [SerializeField] private DashTelegraph _telegraph;
    [SerializeField] private DashBossShoot _shoot;
    [Header("Dash Settings")]
    [SerializeField] private float _telegraphDuration = 0.8f;
    [SerializeField] private float _timeBetweenDashes = 2f;
    [Header("Special Attack")]
    [SerializeField] private int _specialDashCount = 3;
    [SerializeField] private float _specialCooldown = 8f;
    [Header("Circle Special")]
    [SerializeField] private DashTelegraph[] _circleTelegraphs;
    [SerializeField] private int _circleDashCount = 5;
    [SerializeField] private float _circleDashRadius = 4f;
    [SerializeField] private float _circleTelegraphSpawnDuration = 1f; // 1 segundo para aparecer todos
    [SerializeField] private float _circleSpecialCooldown = 12f;
    [SerializeField] private float _circleDashDuration = 20f;
    [SerializeField] private float _circleDashRotationSpeed = 20f;
    [SerializeField] private float _betweenDashes;

    [Header("Particle")]
    [SerializeField] private ParticleSystem _dashImpactParticles;
    [SerializeField] private GameObject _dashFirePrefab;
    [SerializeField] private float _fireYOffset = 0.5f;
    

    private float _nextCircleSpecialTime;

    private Transform _player;
    private float _nextDashTime;
    private float _nextSpecialTime;
    private bool _isBusy;

    [SerializeField] Transform player;

    private void Awake()
    {
        base.Awake();
        SetPlayer(player);
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _movement.SetPlayer(player);
        _nextSpecialTime = Time.time + _specialCooldown;
        _shoot.SetPlayer(player);
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
        _telegraph.Show(transform.position, destination, _telegraphDuration);
        yield return new WaitForSeconds(_telegraphDuration);
        _telegraph.Hide();
        yield return _movement.ExecuteDash(destination);
        _movement.LockRotation(false);
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }
    private IEnumerator DoCircleDash()
    {
        _isBusy = true;
        _shoot.SetCanShoot(false);
        _movement.LockRotation(true);
        yield return _movement.RotateToFace(_player.position);

        // 1. Calcular puntos en circulo alrededor del player
        Vector3 playerPos = _player.position;
        int count = _circleTelegraphs.Length;
        Vector3[] destinations = new Vector3[count];
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _circleDashRadius;
            destinations[i] = playerPos + offset;
            destinations[i].y = transform.position.y;
        }

        // 2. Mostrar indicadores uno por uno
        float spawnInterval = _circleTelegraphSpawnDuration / count;
        Vector3 from = transform.position;
        for (int i = 0; i < count; i++)
        {
            float dist = Vector3.Distance(from, destinations[i]);
            Debug.Log($"Telegraph {i}: distancia={dist}");

            _circleTelegraphs[i].Show(from, destinations[i], _telegraphDuration);
            from = destinations[i];
            yield return new WaitForSeconds(spawnInterval);
        }

        // esperar que el primero termine
        yield return new WaitForSeconds(_telegraphDuration - _circleTelegraphSpawnDuration);

        // 3. Dashes en secuencia
        Vector3 prevPos = transform.position;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (destinations[i] - transform.position);
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
            _circleTelegraphs[i].Hide();
            yield return _movement.ExecuteDash(destinations[i], _circleDashDuration);
            SpawnDashFire(prevPos, destinations[i]);
            prevPos = destinations[i];
            yield return new WaitForSeconds(_betweenDashes);
        }

        _movement.LockRotation(false);
        _nextCircleSpecialTime = Time.time + _circleSpecialCooldown;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }
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
            _telegraph.Show(transform.position, destination, _telegraphDuration);
            yield return new WaitForSeconds(_telegraphDuration);
            _telegraph.Hide();
            yield return _movement.ExecuteDash(destination);
            _movement.LockRotation(false);
            yield return new WaitForSeconds(0.15f);
        }
        _nextSpecialTime = Time.time + _specialCooldown;
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
        print("Special finished, isBusy: " + _isBusy);
    }

    void SpawnDashFire(Vector3 from, Vector3 to)
    {
        Vector3 center = (from + to) / 2f;
        center.y += _fireYOffset;
        Vector3 dir = (to - from);
        dir.y = 0f;
        Quaternion rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, 90f, 0f);
        Instantiate(_dashFirePrefab, center, rotation);
    }
}