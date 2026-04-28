using UnityEngine;
using System.Collections;

public class MorterStrategy : IAbilityStrategy
{
    private readonly SO_MorterData _baseData;
    private readonly RT_MortarData _rt;
    private readonly Transform _ownerTransform;
    private readonly Transform _shootPoint;
    private readonly CoroutineRunner _runner;
    private BulletFactory _barrelFactory;

    private const int MaxCharges = 3;
    private const float DispersionRadius = 3f;

    private int _currentCharges;
    

    //[Header("DATA RELOAD")]
    public event System.Action<int> OnChargeConsumed;  // currentCharges, cooldown
    
    
    public MorterStrategy(SO_MorterData baseData, Transform ownerTransform, Transform shootPoint, CoroutineRunner runner, BulletFactory barrelFactory)
    {
        _baseData = baseData;
        _rt = new RT_MortarData(_baseData);
        _ownerTransform = ownerTransform;
        _shootPoint = shootPoint;
        _runner = runner;
        _barrelFactory = barrelFactory;

        _currentCharges = MaxCharges;
    }

    public void TryExecute()
    {
        if (_currentCharges <= 0) return;
        _currentCharges--;
        OnChargeConsumed?.Invoke(_currentCharges);
        Shoot();
    }

    private void Shoot()
    {
        Vector2 rand = Random.insideUnitCircle * DispersionRadius;
        Vector3 offset = new Vector3(rand.x, 0f, rand.y);
        VisualShoot();
        SpawnRealProjectile(offset);
    }

    public void RestoreCharge()
    {
        if (_currentCharges < MaxCharges)
            _currentCharges++;
    }
    private void VisualShoot()
    {
        if (_baseData.visualProjectilePrefab == null || _shootPoint == null) return;

        GameObject proj = Object.Instantiate(
            _baseData.visualProjectilePrefab,
            _shootPoint.position,
            Quaternion.identity
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.up * _rt.visualShootForce;

        _runner.StartCoroutine(DisableAfterSeconds(proj, _rt.visualLifetime));
    }

    private IEnumerator DisableAfterSeconds(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private void SpawnRealProjectile(Vector3 spawnOffset)
    {
        if (_baseData.realProjectilePrefab == null || _ownerTransform == null) return;

        var b = _barrelFactory.Create();
        var cb = b.GetComponent<BarrelExplosion>();
        if (cb != null)
            cb.Setup(_ownerTransform, _rt, spawnOffset);
    }
    public int GetCurrentCharges() => _currentCharges;
    public int GetMaxCharges() => MaxCharges;
}