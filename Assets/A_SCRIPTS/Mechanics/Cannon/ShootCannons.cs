using UnityEngine;
using System.Collections;

public class ShootCannons : MonoBehaviour
{
    [Header("Bullet")]
    [SerializeField] GameObject _bulletPrefab;
    [SerializeField] float _bulletSpeed;
    [SerializeField] float _verticalArc = 0.25f;

    [Header("Burst")]
    [SerializeField] int _shotsPerBurst = 2;
    [SerializeField] float _timeBetweenShots = 0.25f;

    [Header("Shoot Points")]
    [SerializeField] Transform[] _shootPoints;
    [SerializeField] Transform[] _negativeShootPoints;

    [Header("Effects")]
    [SerializeField] GameObject _explosionVfx;
    [SerializeField] ParticleSystem[] _cannnonSmokeVfx;

    [Header("Cooldown")]
    [SerializeField] float _fireCooldown = 1.5f;

    float _nextFireTime = 0f;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireCooldown;
            StartCoroutine(FireBurst());
        }
    }

    IEnumerator FireBurst()
    {
        for (int i = 0; i < _shotsPerBurst; i++)
        {
            foreach (Transform point in _shootPoints)
                FireFromPoint(point, 1f);

            foreach (Transform point in _negativeShootPoints)
                FireFromPoint(point, -1f);
            foreach(ParticleSystem ps in _cannnonSmokeVfx)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
                
            if (i < _shotsPerBurst - 1)
                yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    void FireFromPoint(Transform point, float side)
    {
        GameObject bullet = Instantiate(_bulletPrefab, point.position, Quaternion.identity);
        bullet.GetComponent<CannonBullet>().Setup(_explosionVfx);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        Vector3 dir = point.right * side;
        dir.y = 0f;
        dir.Normalize();

        Vector3 shootDir = (dir + Vector3.up * _verticalArc).normalized;
        rb.linearVelocity = shootDir * _bulletSpeed;
    }
}
