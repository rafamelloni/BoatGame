using System;
using System.Collections;
using UnityEngine;

public class CannonStrategy : IAbilityStrategy, IcooldownAbilities, IUpgradeable
{
    private readonly SO_CannonData _baseData;
    public readonly RT_CannonData _rtData;
    private BulletFactory _cannonBullet;
    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;
    private float nextFireTime = 0f;

    followPlayer _camera;
    CannonRecoil _recoilLeft;
    CannonRecoil _recoilRight;

    // IcooldownAbilities
    public event Action<float> OnCooldownStarted;
    public float CooldownDuration => _rtData.cooldown;
    public float RemainingCooldown => Mathf.Max(0f, nextFireTime - Time.time);
    public bool IsOnCooldown => Time.time < nextFireTime;

    // IUpgradeable
    public string AbilityId => "Cannon";
    public bool IsUnlocked { get; private set; } = false;
    public void SetUnlocked(bool unlocked) => IsUnlocked = unlocked;
    public StatType[] ValidStats => new[] { StatType.Damage, StatType.FireRate, StatType.Cooldown };


    public RT_CannonData RuntimeData => _rtData;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner, BulletFactory cannonBullet, followPlayer camera, CannonRecoil recoilR, CannonRecoil recoilL)
    {
        this._baseData = data;
        _rtData = new RT_CannonData(_baseData);
        this.hardpoints = hardpoints;
        this.runner = runner;
        this._cannonBullet = cannonBullet;
        this._camera = camera;
        this._recoilLeft = recoilL;
        this._recoilRight = recoilR;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + _rtData.cooldown;
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
                _camera.ApplyCannonImpact();
                _recoilRight.Fire(_rtData.timeBetweenShots);
                _recoilLeft.Fire(_rtData.timeBetweenShots);
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
        var cb = b.GetComponent<CannonBullet>();
        if (cb != null)
            cb.Setup(point, _rtData, side);
    }

    public void ApplyUpgrade(StatType stat, float value)
    {
        switch (stat)
        {
            case StatType.Damage: _rtData.damage += value; break;
            case StatType.Cooldown: _rtData.cooldown = Mathf.Max(0.1f, _rtData.cooldown - value); break;
            case StatType.FireRate: _rtData.timeBetweenShots = Mathf.Max(0.1f, _rtData.timeBetweenShots - value); break;
        }
    }

    public void ResetUpgrades()
    {
        _rtData.damage = _baseData.damage;
        _rtData.cooldown = _baseData.cooldown;
        _rtData.timeBetweenShots = _baseData.timeBetweenShots;
        _rtData.shotsPerBurst = _baseData.shotsPerBurst;
        nextFireTime = 0f;
    }
}