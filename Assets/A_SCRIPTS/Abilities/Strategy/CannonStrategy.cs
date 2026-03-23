using System.Collections;
using UnityEngine;
public class CannonStrategy : IAbilityStrategy
{
    //No se modifica. Se usa para cosas que no van a cambiar como los VFX
    private readonly SO_CannonData _baseData;

    //Realtime si se modifica.
    private readonly RT_CannonData _rtData;

    //factory de balas
    private BulletFactory _cannonBullet;
    private ParticlePool _particlePool;

    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;

    private float nextFireTime = 0f;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner, BulletFactory cannonBullet, ParticlePool particlePool)
    {
        this._baseData = data;
        _rtData = new RT_CannonData(_baseData);
        this.hardpoints = hardpoints;
        this.runner = runner;
        this._cannonBullet = cannonBullet;
        _particlePool = particlePool;
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
        var b = _cannonBullet.Create();
        
        Transform pointSH = point;
        float sideSH = side;
        var cb = b.GetComponent<CannonBullet>();
        if (cb != null)
            cb.Setup(pointSH, _rtData, sideSH, _baseData.explosionVfx);
    }
}
