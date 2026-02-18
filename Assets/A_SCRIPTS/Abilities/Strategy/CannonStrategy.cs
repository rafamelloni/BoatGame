using System.Collections;
using UnityEngine;
public class CannonStrategy : IAbilityStrategy
{
    //No se modifica. Se usa para cosas que no van a cambiar como los VFX
    private readonly SO_CannonData _baseData;

    //Realtime si se modifica.
    private readonly RT_CannonData _rtData;


    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;

    private float nextFireTime = 0f;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner)
    {
        this._baseData = data;
        _rtData = new RT_CannonData(_baseData);
        this.hardpoints = hardpoints;
        this.runner = runner;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + _rtData.cooldown;

        runner.StartCoroutine(FireBurst());
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < _rtData.shotsPerBurst; i++)
        {
            foreach (Transform p in hardpoints.rightShootPoints)
                FireFromPoint(p, 1f);

            foreach (Transform p in hardpoints.leftShootPoints)
                FireFromPoint(p, -1f);

            if (i < _rtData.shotsPerBurst - 1)
                yield return new WaitForSeconds(_rtData.timeBetweenShots);
        }
    }

    private void FireFromPoint(Transform point, float side)
    {
        GameObject bullet = Object.Instantiate(_baseData.bulletPrefab, point.position, Quaternion.identity);

        var cb = bullet.GetComponent<CannonBullet>();
        if (cb != null)
            cb.Setup(_baseData.explosionVfx);

        var rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 dir = point.right * side;
        dir.y = 0f;
        dir.Normalize();

        Vector3 shootDir = (dir + Vector3.up * _rtData.verticalArc).normalized;
        rb.linearVelocity = shootDir * _rtData.bulletSpeed;
    }
}
