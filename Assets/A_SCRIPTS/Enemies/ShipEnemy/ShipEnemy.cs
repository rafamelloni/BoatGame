using System;
using UnityEngine;

public class ShipEnemy : Enemy
{
    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private Transform playerAimPoint;

    [Header("Distances")]
    [SerializeField] private float broadSideDistance = 15f;
    [SerializeField] private float broadSideExitDistance = 20f;

    [Header("Broadside")]
    [SerializeField] private float broadSideRotationSpeed = 5f;
    [SerializeField] private float orbitSpeed = 6f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float obstacleRadius = 8f;
    [SerializeField] private float avoidanceWeight = 3f;

    [Header("Broadside Obstacle")]
    [SerializeField] private float broadsideRayLength = 6f;
    [SerializeField] private float broadsideFlipCooldown = 1.5f;

    [Header("Shoot")]
    private BulletFactory _bullets;
    private RT_CannonData _rtCannonDataEnemy;
    private float _nextFireTime;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private Transform _SP;
    [SerializeField] private Transform _SP1;
    [SerializeField] private ParticleSystem particle;
    [SerializeField] private ParticleSystem particle0;

    public SO_CannonData so_island;
    private Transform _player;
    [SerializeField] private Transform player;
    private int _broadsideSide = 1;
    private float _lastFlipTime = -99f;

    private enum State { Approach, Broadside }
    private State _state;
    private Rigidbody _rb;

    public event Action<ShipEnemy> OnDead;

    private void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        _rtCannonDataEnemy = new RT_CannonData(so_island);
        _bullets = GameObject.FindWithTag("IslandBulletFactory").GetComponent<BulletFactory>();
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
    }

    public void SetPlayer(Transform player, Transform aimPoint)
    {
        _player = player;
        playerAimPoint = aimPoint;
        _state = State.Approach;
        _enemyHealth.Revive();
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (_state == State.Approach && dist <= broadSideDistance)
        {
            float dot = Vector3.Dot(transform.right, toPlayer.normalized);
            _broadsideSide = dot >= 0 ? 1 : -1;
            _nextFireTime = Time.time + fireRate;
            _state = State.Broadside;
        }
        else if (_state == State.Broadside && dist > broadSideExitDistance)
        {
            _state = State.Approach;
        }

        switch (_state)
        {
            case State.Approach: HandleApproach(toPlayer); break;
            case State.Broadside: HandleBroadside(toPlayer); break;
        }
    }

    private void HandleApproach(Vector3 toPlayer)
    {
        Vector3 avoidance = Vector3.zero;
        Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleRadius, obstacleLayer);
        foreach (var col in obstacles)
        {
            Vector3 away = transform.position - col.ClosestPoint(transform.position);
            away.y = 0f;
            float d = away.magnitude;
            if (d > 0.001f)
                avoidance += away.normalized / d;
        }

        Vector3 desiredDir = toPlayer.normalized + avoidance * avoidanceWeight;
        desiredDir.y = 0f;

        if (desiredDir.sqrMagnitude < 0.001f)
            desiredDir = toPlayer.normalized;

        Quaternion targetRot = Quaternion.LookRotation(desiredDir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        transform.position += transform.forward * speed * Time.deltaTime;
    }
    private void HandleBroadside(Vector3 toPlayer)
    {
        // --- Raycast hacia adelante: si hay isla, flip de lado ---
        if (Time.time > _lastFlipTime + broadsideFlipCooldown)
        {
            if (Physics.Raycast(transform.position, transform.forward, broadsideRayLength, obstacleLayer))
            {
                _broadsideSide *= -1;
                _lastFlipTime = Time.time;
            }
        }

        Vector3 targetDir = playerAimPoint != null
            ? playerAimPoint.position - transform.position
            : toPlayer;
        targetDir.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(targetDir.normalized)
            * Quaternion.Euler(0f, -90f * _broadsideSide, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, broadSideRotationSpeed * Time.deltaTime);
        transform.position += transform.forward * orbitSpeed * Time.deltaTime;
        TryShoot();
    }

    private void TryShoot()
    {
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + fireRate;
        Shoot();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("ShipEnemy"))
        {
            other.gameObject.GetComponent<EnemyHealth>().TakeDamage(999f);
            print("cool");
        }
    }

    private void Shoot()
    {
        var b = _bullets.Create();
        b.GetComponent<CannonBulletIsland>().Setup(_SP, _rtCannonDataEnemy, -1);
        particle.Play();
        var a = _bullets.Create();
        particle0.Play();
        a.GetComponent<CannonBulletIsland>().Setup(_SP1, _rtCannonDataEnemy, -1);
    }
}