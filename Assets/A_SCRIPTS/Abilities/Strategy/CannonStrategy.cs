using System.Collections;
using UnityEngine;
public class CannonStrategy : IAbilityStrategy
{
    private readonly SO_CannonData data;
    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;

    private float nextFireTime = 0f;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner)
    {
        this.data = data;
        this.hardpoints = hardpoints;
        this.runner = runner;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + data.cooldown;

        runner.StartCoroutine(FireBurst());
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < data.shotsPerBurst; i++)
        {
            foreach (Transform p in hardpoints.rightShootPoints)
                FireFromPoint(p, 1f);

            foreach (Transform p in hardpoints.leftShootPoints)
                FireFromPoint(p, -1f);

            if (i < data.shotsPerBurst - 1)
                yield return new WaitForSeconds(data.timeBetweenShots);
        }
    }

    private void FireFromPoint(Transform point, float side)
    {
        GameObject bullet = Object.Instantiate(data.bulletPrefab, point.position, Quaternion.identity);

        var cb = bullet.GetComponent<CannonBullet>();
        if (cb != null)
            cb.Setup(data.explosionVfx);

        var rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 dir = point.right * side;
        dir.y = 0f;
        dir.Normalize();

        Vector3 shootDir = (dir + Vector3.up * data.verticalArc).normalized;
        rb.linearVelocity = shootDir * data.bulletSpeed;
    }
}
