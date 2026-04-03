using System;
using System.Collections;
using UnityEngine;
public class CannonStrategy : IAbilityStrategy, IcooldownAbilities, IUpgradeable
{
    //No se modifica. Se usa para cosas que no van a cambiar como los VFX
    private readonly SO_CannonData _baseData;

    //Realtime si se modifica.
    private readonly RT_CannonData _rtData;

    //factory de balas
    private BulletFactory _cannonBullet;

    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;

    private float nextFireTime = 0f;


    //Data de la interfaz de cooldown
    public event Action<float> OnCooldownStarted; //Delegate que se llama cuando arrranca el cooldown de la abilidad
    public float CooldownDuration => _rtData.cooldown;
    public float RemainingCooldown
    {
        get
        {
            float remaining = nextFireTime - Time.time;
            return Mathf.Max(0f, remaining);
        }
    }
    public bool IsOnCooldown => Time.time < nextFireTime;
    public RT_CannonData RuntimeData => _rtData;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner, BulletFactory cannonBullet)
    {
        this._baseData = data;
        _rtData = new RT_CannonData(_baseData);
        this.hardpoints = hardpoints;
        this.runner = runner;
        this._cannonBullet = cannonBullet;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + _rtData.cooldown;

        //LLamamos delegate, ejecutamos todo lo que este suscrito a cuando arranca el cooldown de la abilidad
        OnCooldownStarted?.Invoke(_rtData.cooldown);

        runner.StartCoroutine(FireBurst());
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < _rtData.shotsPerBurst; i++)
        {
            foreach (Transform p in hardpoints.rightShootPoints)
            {
                FireFromPoint(p, 1f);
                hardpoints._cannonSmokeShootR.Play();
            }
                

            foreach (Transform p in hardpoints.leftShootPoints)
            {
                FireFromPoint(p, -1f);
                hardpoints._cannonSmokeShootL.Play();

            }


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
            cb.Setup(pointSH, _rtData, sideSH);
    }

    public void ApplyUpgrade(StatType stat, float value)
    {
        switch (stat)
        {
            case StatType.Damage:
                _rtData.damage += value;
                break;
            case StatType.Cooldown:
                _rtData.cooldown = Mathf.Max(0.1f, _rtData.cooldown - value);
                break;
            case StatType.BulletSpeed:
                _rtData.bulletSpeed += value;
                break;
            case StatType.ShotsPerBurst:
                _rtData.shotsPerBurst += (int)value;
                break;
        }
    }
}

