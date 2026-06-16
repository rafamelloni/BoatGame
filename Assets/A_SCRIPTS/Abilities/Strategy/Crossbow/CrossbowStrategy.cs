using System.Collections;
using UnityEngine;

public class CrossbowStrategy : IAbilityStrategy
{
    private readonly SO_CrossbowData _baseData;
    public readonly RT_CrossbowData _rtData;
    private readonly Transform _launchPoint;
    private readonly CoroutineRunner _runner;
    private readonly LayerMask _enemyLayers;
    private readonly RT_PlayerUpgrades _playerUpgrades;
    private readonly GameObject _crossbowGO;
    private readonly CrossbowAimer _aimer;

    public bool IsUnlocked { get; private set; } = false;

    public CrossbowStrategy(SO_CrossbowData data, Transform launchPoint, CoroutineRunner runner,
        LayerMask enemyLayers, RT_PlayerUpgrades playerUpgrades, GameObject crossbowGO)
    {
        _baseData = data;
        _rtData = new RT_CrossbowData(_baseData);
        _launchPoint = launchPoint;
        _runner = runner;
        _enemyLayers = enemyLayers;
        _playerUpgrades = playerUpgrades;
        _crossbowGO = crossbowGO;
        _aimer = crossbowGO != null ? crossbowGO.GetComponent<CrossbowAimer>() : null;
    }

    public void TryExecute() { }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        if (_crossbowGO != null)
            _crossbowGO.SetActive(unlocked);
        if (unlocked)
            _runner.StartCoroutine(ShootLoop());
    }

    private IEnumerator ShootLoop()
    {
        while (IsUnlocked)
        {
            Transform target = GetNearestEnemy();
            if (target != null)
            {
                _aimer?.SetTarget(target);
                TryShoot(target);
            }
            yield return new WaitForSeconds(_rtData.cooldown);
        }
    }

    private void TryShoot(Transform target)
    {
        bool hasBurst = _playerUpgrades != null &&
            _playerUpgrades.HasAbility(SpecialAbilityType.CrossbowBurst);

        if (hasBurst)
            ShootBurst(target);
        else
            SpawnArrow(target.position, 0f);
    }

    private void ShootBurst(Transform target)
    {
        float halfSpread = _rtData.burstSpreadAngle / 2f;
        float step = _rtData.burstArrowCount > 1
            ? _rtData.burstSpreadAngle / (_rtData.burstArrowCount - 1)
            : 0f;

        for (int i = 0; i < _rtData.burstArrowCount; i++)
        {
            float angle = -halfSpread + step * i;
            SpawnArrow(target.position, angle);
        }
    }

    private void SpawnArrow(Vector3 targetPos, float angleOffset)
    {
        Vector3 dir = (targetPos - _launchPoint.position).normalized;
        if (angleOffset != 0f)
            dir = Quaternion.Euler(0f, angleOffset, 0f) * dir;

        Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f);
        GameObject go = Object.Instantiate(_baseData.arrowPrefab, _launchPoint.position, rot);

        CrossbowArrow arrow = go.GetComponent<CrossbowArrow>();
        if (arrow != null)
            arrow.Init(dir, _rtData.arrowSpeed, _rtData.damage, _rtData.explosionVfx, _rtData.waterVfx);
    }

    private Transform GetNearestEnemy()
    {
        Collider[] nearby = Physics.OverlapSphere(_launchPoint.position, _rtData.detectionRadius, _enemyLayers);
        if (nearby.Length == 0) return null;

        Transform best = null;
        float bestDist = Mathf.Infinity;
        foreach (var col in nearby)
        {
            float dist = Vector3.Distance(_launchPoint.position, col.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = col.transform;
            }
        }
        return best;
    }

    public void ResetUpgrades()
    {
        SetUnlocked(false);
        _rtData.damage = _baseData.damage;
        _rtData.cooldown = _baseData.cooldown;
        _rtData.arrowSpeed = _baseData.arrowSpeed;
        _rtData.detectionRadius = _baseData.detectionRadius;
    }
}