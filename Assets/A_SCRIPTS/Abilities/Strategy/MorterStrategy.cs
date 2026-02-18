using UnityEngine;
using System.Collections;

public class MorterStrategy : IAbilityStrategy
{
    // SO: referencias base (NO se modifican)
    private readonly SO_MorterData _baseData;

    // Runtime: números modificables (SÍ se modifican)
    private readonly RT_MortarData _rt;

    private readonly Transform _ownerTransform;
    private readonly Transform _shootPoint;
    private readonly CoroutineRunner _runner;

    private float nextFireTime = 0f;

    public MorterStrategy(SO_MorterData baseData , Transform ownerTransform, Transform shootPoint, CoroutineRunner runner)
    {
        this._baseData = baseData;
        _rt = new RT_MortarData(_baseData);
        this._ownerTransform = ownerTransform;
        this._shootPoint = shootPoint;
        this._runner = runner;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + _rt.cooldown;

        VisualShoot();
        SpawnRealProjectile();
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
        {
            // Si estás en Unity normal es rb.velocity; rb.linearVelocity es DOTS/Unity Physics
            rb.linearVelocity = Vector3.up * _rt.visualShootForce;
        }

        if (_runner != null)
            _runner.StartCoroutine(DisableAfterSeconds(proj, _rt.visualLifetime));
        else
            Object.Destroy(proj, _rt.visualLifetime); // fallback si no tenés runner
    }

    private IEnumerator DisableAfterSeconds(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private void SpawnRealProjectile()
    {
        if (_baseData.realProjectilePrefab == null) return;
        if (_ownerTransform == null) return;

        Vector3 backDir = -_ownerTransform.forward;
        backDir.y = 0f;
        backDir.Normalize();

        Vector3 spawnPos =
            _ownerTransform.position +
            backDir * _rt.backDistance +
            Vector3.up * _rt.height;

        Vector2 rand = Random.insideUnitCircle * _rt.randomRadius;
        spawnPos += new Vector3(rand.x, 0f, rand.y);

        GameObject real = Object.Instantiate(_baseData.realProjectilePrefab, spawnPos, Quaternion.identity);

        var barrel = real.GetComponent<BarrelExplosion>();
        if (barrel != null)
        {
            barrel.Setup(_baseData.explosionVfx, _baseData.circleVfx);
        }
    }
}
