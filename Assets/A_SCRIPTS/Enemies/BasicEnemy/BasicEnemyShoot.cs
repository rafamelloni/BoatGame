using UnityEngine;

public class BasicEnemyShoot : MonoBehaviour
{

    public BulletFactory enemyBullet;

    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireRate = 2f;

    private float _nextFireTime;

    void Update()
    {
        if (Time.time >= _nextFireTime)
        {
            Shoot();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        var bullet = enemyBullet.Create();

        bullet.transform.position = shootPoint.position;
        bullet.transform.rotation = shootPoint.rotation;
    }
}
