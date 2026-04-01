using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class CannonShortRangeIsland : MonoBehaviour
{
    [SerializeField] FOV _fov;
    [SerializeField] Transform  _player;


    public BulletFactory enemyBullet;

    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float _fireRange = 15.5f;
    [SerializeField] private float _rotationSpeed = 5f;

    private float _nextFireTime;

    void Update()
    {
        if (!_fov.CanSeeTarget())
        {
           _nextFireTime = 0;
            return;
        }
        float distance = _fov.DistanceToTarget();

        if (distance < _fireRange)
        {
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

        bullet.transform.position = shootPoint.position;
        bullet.transform.rotation = shootPoint.rotation;
    }
}