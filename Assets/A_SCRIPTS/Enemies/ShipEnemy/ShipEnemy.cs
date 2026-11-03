using System;
using UnityEngine;

// Te persigue, se queda a cierta distancia y te dispara de lejos.
// Los cañones giran hacia vos y las balas salen hacia donde vas a estar.
public class ShipEnemy : Enemy
{
    [Header("Movimiento")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _retreatSpeed = 4f;
    [SerializeField] private float _acceleration = 6f;
    [SerializeField] private float _turnSpeed = 2.5f;

    [Header("Distancia al jugador")]
    [Tooltip("Distancia a la que intenta quedarse")]
    [SerializeField] private float _preferredDistance = 18f;
    [Tooltip("Margen alrededor de esa distancia en el que se queda quieto")]
    [SerializeField] private float _distanceTolerance = 3f;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleRadius = 8f;
    [SerializeField] private float avoidanceWeight = 3f;

    [Header("Disparo")]
    [Tooltip("Segundos entre cada disparo de los dos cañones")]
    [SerializeField] private float _fireInterval = 2.5f;
    [SerializeField] private float _firstShotDelay = 1.5f;
    [SerializeField] private float _fireRange = 32f;
    [SerializeField] private float _bulletSpeed = 18f;
    [Tooltip("Cuánto anticipa tu movimiento. 0 = apunta a donde estás, 1 = a donde vas a estar")]
    [SerializeField, Range(0f, 1f)] private float _aimLead = 0.7f;
    [Tooltip("Error de puntería en metros")]
    [SerializeField] private float _aimSpread = 1f;
    [SerializeField] private float _cannonTurnSpeed = 8f;
    [SerializeField] private Transform _SP;
    [SerializeField] private Transform _SP1;
    [SerializeField] private ParticleSystem particle;
    [SerializeField] private ParticleSystem particle0;

    public SO_CannonData so_island;

    private BulletFactory _bullets;
    private RT_CannonData _rtCannonDataEnemy;
    private Transform _player;
    private Rigidbody _playerRb;
    private Transform _cannon;
    private Transform _cannon1;
    private float _nextFireTime;
    private float _currentSpeed;

    public event Action<ShipEnemy> OnDead;

    private void Awake()
    {
        base.Awake();
        _rtCannonDataEnemy = new RT_CannonData(so_island);
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
        if (_SP != null) _cannon = _SP.parent;
        if (_SP1 != null) _cannon1 = _SP1.parent;
    }

    public void SetPlayer(Transform player, Transform aimPoint)
    {
        _player = player;
        _playerRb = player != null ? player.GetComponentInParent<Rigidbody>() : null;
        _currentSpeed = 0f;
        _nextFireTime = Time.time + _firstShotDelay;
    }

    private void OnEnable()
    {
        _bullets = GameObject.FindWithTag("IslandBulletFactory").GetComponent<BulletFactory>();
        _enemyHealth.Revive();
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance < 0.01f) return;

        Move(toPlayer / distance, distance);
        AimCannon(_cannon, _SP);
        AimCannon(_cannon1, _SP1);

        if (distance <= _fireRange && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireInterval;
            Shoot();
        }
    }

    private void Move(Vector3 directionToPlayer, float distance)
    {
        Vector3 desiredDirection = directionToPlayer;
        float targetSpeed = 0f;

        if (distance > _preferredDistance + _distanceTolerance)
        {
            targetSpeed = _chaseSpeed;
        }
        else if (distance < _preferredDistance - _distanceTolerance)
        {
            desiredDirection = -directionToPlayer;
            targetSpeed = _retreatSpeed;
        }

        desiredDirection += GetAvoidance() * avoidanceWeight;
        desiredDirection.y = 0f;
        if (desiredDirection.sqrMagnitude < 0.001f) desiredDirection = directionToPlayer;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;
    }

    private Vector3 GetAvoidance()
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
        return avoidance;
    }

    // El caño del modelo apunta hacia -X
    private void AimCannon(Transform cannon, Transform shootPoint)
    {
        if (cannon == null || shootPoint == null) return;

        Vector3 direction = _player.position - cannon.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 forward = Vector3.Cross(-direction.normalized, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
        cannon.rotation = Quaternion.Slerp(cannon.rotation, targetRotation, _cannonTurnSpeed * Time.deltaTime);
    }

    private Vector3 GetAimPoint(Vector3 origin)
    {
        Vector3 target = _player.position;

        if (_playerRb != null && _aimLead > 0f)
        {
            float flightTime = Vector3.Distance(origin, target) / Mathf.Max(_bulletSpeed, 0.01f);
            Vector3 velocity = _playerRb.linearVelocity;
            velocity.y = 0f;
            target += velocity * flightTime * _aimLead;
        }

        Vector2 spread = UnityEngine.Random.insideUnitCircle * _aimSpread;
        target.x += spread.x;
        target.z += spread.y;
        return target;
    }

    private void Shoot()
    {
        ShootFrom(_SP, particle);
        ShootFrom(_SP1, particle0);
    }

    private void ShootFrom(Transform shootPoint, ParticleSystem muzzle)
    {
        if (shootPoint == null || _bullets == null) return;

        var bullet = _bullets.Create();
        bullet.GetComponent<CannonBulletIsland>().SetupAimed(shootPoint, _rtCannonDataEnemy, GetAimPoint(shootPoint.position), _bulletSpeed);

        if (muzzle != null)
        {
            muzzle.transform.position = shootPoint.position;
            muzzle.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("ShipEnemy"))
        {
            // No matar enemigos que todavia estan saliendo del agua (recien spawneados)
            var emerge = other.GetComponentInParent<EnemyEmerge>();
            if (emerge != null && emerge.IsEmerging) return;

            other.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(999f);
        }
    }
}
