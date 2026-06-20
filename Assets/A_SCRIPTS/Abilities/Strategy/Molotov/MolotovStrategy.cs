using System.Collections;
using UnityEngine;

public class MolotovStrategy : IAbilityStrategy
{
    private readonly SO_MolotovData _baseData;
    public readonly RT_MolotovData _rtData;
    private readonly Transform _launchPoint;
    private readonly CoroutineRunner _runner;
    private readonly LayerMask _enemyLayers;
    private readonly RT_PlayerUpgrades _playerUpgrades;
    private readonly Collider _shipCollider;

    public bool IsUnlocked { get; private set; } = false;

    public MolotovStrategy(SO_MolotovData data, Transform launchPoint, CoroutineRunner runner, LayerMask enemyLayers, RT_PlayerUpgrades playerUpgrades, Collider shipCollider)
    {
        _baseData = data;
        _rtData = new RT_MolotovData(_baseData);
        _launchPoint = launchPoint;
        _runner = runner;
        _enemyLayers = enemyLayers;
        _playerUpgrades = playerUpgrades;
        _shipCollider = shipCollider;
    }

    public void TryExecute() { }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        if (unlocked)
        {
            _runner.StartCoroutine(LaunchLoop());
            _runner.StartCoroutine(BurstLoop());
        }
    }

    private IEnumerator LaunchLoop()
    {
        _launchTimer = 0f;
        while (IsUnlocked)
        {
            _launchTimer += Time.deltaTime;
            if (_launchTimer >= _rtData.launchInterval)
            {
                _launchTimer = 0f;
                TryLaunch();
            }
            yield return null;
        }
        _launchTimer = 0f;
    }

    private IEnumerator BurstLoop()
    {
        while (IsUnlocked)
        {
            yield return new WaitForSeconds(_rtData.burstInterval);
            bool hasBurst = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.TripleMolotov);
            if (hasBurst)
                TryLaunchBurst();
        }
    }
    private float _launchTimer = 0f;
    public float LaunchProgress => _rtData.launchInterval > 0f ? Mathf.Clamp01(_launchTimer / _rtData.launchInterval) : 0f;
    private void TryLaunch()
    {
        Collider[] nearby = Physics.OverlapSphere(_launchPoint.position, _rtData.detectionRadius, _enemyLayers);
        if (nearby.Length == 0) return;

        Collider bestTarget = null;
        float bestDist = Mathf.Infinity;

        for (int i = 0; i < nearby.Length; i++)
        {
            float dist = Vector3.Distance(_launchPoint.position, nearby[i].transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = nearby[i];
            }
        }

        if (bestTarget == null) return;
        SpawnMolotov(bestTarget.transform.position);
    }

    private void TryLaunchBurst()
    {
        Collider[] nearby = Physics.OverlapSphere(_launchPoint.position, _rtData.detectionRadius, _enemyLayers);
        if (nearby.Length == 0) return;

        System.Array.Sort(nearby, (a, b) =>
            Vector3.Distance(_launchPoint.position, a.transform.position)
            .CompareTo(Vector3.Distance(_launchPoint.position, b.transform.position)));

        for (int i = 0; i < _rtData.burstCount; i++)
        {
            Collider target = nearby[i % nearby.Length];
            SpawnMolotov(target.transform.position);
        }
    }

    private void SpawnMolotov(Vector3 targetPos)
    {
        GameObject go = Object.Instantiate(_rtData.molotovPrefab, _launchPoint.position, Quaternion.identity);
        MolotovProjectile projectile = go.GetComponent<MolotovProjectile>();
        if (projectile != null)
            projectile.Launch(_launchPoint.position, targetPos, _rtData, _shipCollider);
    }

    public void ResetUpgrades()
    {
        SetUnlocked(false);
        _rtData.damage = _baseData.damage;
        _rtData.launchInterval = _baseData.launchInterval;
        _rtData.explosionRadius = _baseData.explosionRadius;
        _rtData.fireDamagePerSecond = _baseData.fireDamagePerSecond;
        _rtData.fireRadius = _baseData.fireRadius;
        _rtData.fireDuration = _baseData.fireDuration;
        _rtData.burstInterval = _baseData.burstInterval;
        _rtData.burstCount = _baseData.burstCount;
        _launchTimer = 0f;
    }
}