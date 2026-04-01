using UnityEngine;
using UnityEngine.UI;

public class CannonIslandShoot : Enemy
{
    [Header("Target")]
    [SerializeField] private Transform _player;

    [Header("Shoot")]
    [SerializeField] private FOV _fov;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private float _fireRate = 2f;
    [SerializeField] private float _fireRange = 20f;
    [SerializeField] private BulletFactory _bullets;

    private RT_CannonData _rtIsland;
    public SO_CannonData so_island;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 5f;

    private float _nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        _rtIsland = new RT_CannonData(so_island);
    }

    private void Update()
    {
        if (_player == null) return;
        if (!_fov.CanSeeTarget())
        {
            return;
        }

        float distance = _fov.DistanceToTarget();
        if (distance < _fireRange)
        {
            RotateToPlayer();
            TryShoot();
        }
        
    }

    private void RotateToPlayer()
    {
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f; // clave: solo rota en Y

        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            _rotationSpeed * Time.deltaTime
        );
    }

    private void TryShoot()
    {
        if (Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + _fireRate;

        Shoot();
    }

    private void Shoot()
    {
        var b = _bullets.Create();
        var cb = b.GetComponent<CannonBulletIsland>();
        cb.Setup(_shootPoint, _rtIsland, -1);
    }
}