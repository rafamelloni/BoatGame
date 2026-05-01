using UnityEngine;

public class CannonShortRangeIsland : MonoBehaviour
{
    [SerializeField] FOV _fov;
     Transform  _player;


    public BulletFactory enemyBullet;

    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float _fireRange = 15.5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _speed = 5f;

    private RT_CannonData _rtIsland;
    public SO_CannonData so_island;

    private float _nextFireTime;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
        enemyBullet = GameObject.FindWithTag("IslandHeavyBulletFactory").GetComponent<BulletFactory>();
        _rtIsland = new RT_CannonData(so_island);
        _nextFireTime = float.MaxValue;
    }
    void Update()
    {
        if (!_fov.CanSeeTarget())
        {
            _nextFireTime = float.MaxValue;
            return;
        }
        float distance = _fov.DistanceToTarget();

        if (distance < _fireRange)
        {
            if (_nextFireTime == float.MaxValue)
                _nextFireTime = Time.time + fireRate;
            RotateToPlayer();

            if (Time.time >= _nextFireTime)
            {
                Shoot();
                _nextFireTime = Time.time + fireRate;
            }
        }
       // _nextFireTime = 0;
    }

    private void RotateToPlayer()
    {
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f; // clave: solo rota en Y

        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            _rotationSpeed * Time.deltaTime
        );
    }

    private void Shoot()
    {
        if (!_fov.CanSeeTarget()) return;
        var bullet = enemyBullet.Create();
        bullet.GetComponent<HeavyIslandBullet>().Setup(shootPoint, _speed, _rtIsland);
    }
}