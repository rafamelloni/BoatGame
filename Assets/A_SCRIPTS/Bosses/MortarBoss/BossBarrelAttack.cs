using System.Collections;
using UnityEngine;

public class BossBarrelAttack : MonoBehaviour
{
    [SerializeField] private BulletFactory _factory;
    [SerializeField] private SO_MorterData _mortarData;
    [SerializeField] private float _spawnHeight = 20f;
    [SerializeField] private Transform _shootPoint;

    [Header("Visual Shoot")]
    [SerializeField] private float _visualDelay = 0.1f;
    [SerializeField] private ParticleSystem _shootParticle;
    [SerializeField] private int _visualProjectilePoolSize = 10;

    private RT_MortarData _rtMortarData;
    private int _pendingVisuals = 0;
    private Coroutine _visualRoutine;
    private ObjectPool<Rigidbody> _visualPool;

    private void Start()
    {
        _rtMortarData = new RT_MortarData(_mortarData);

        if (_mortarData.visualProjectilePrefab != null)
        {
            _visualPool = new ObjectPool<Rigidbody>(
                () =>
                {
                    var go = Instantiate(_mortarData.visualProjectilePrefab);
                    go.SetActive(false);
                    return go.GetComponent<Rigidbody>();
                },
                rb => rb.gameObject.SetActive(true),
                rb =>
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.gameObject.SetActive(false);
                },
                _visualProjectilePoolSize
            );
        }
    }

    public void QueueVisual()
    {
        _pendingVisuals++;
        if (_visualRoutine != null) StopCoroutine(_visualRoutine);
        _visualRoutine = StartCoroutine(FireVisualsSequential());
    }

    private IEnumerator FireVisualsSequential()
    {
        yield return null;
        while (_pendingVisuals > 0)
        {
            VisualShoot();
            _pendingVisuals--;
            yield return new WaitForSeconds(_visualDelay);
        }
        _visualRoutine = null;
    }

    public void SpawnBarrel(Vector3 targetPosition)
    {
        var barrel = _factory.Create() as BarrelExplosion;
        if (barrel == null) return;
        Vector3 spawnPos = new Vector3(targetPosition.x, _spawnHeight, targetPosition.z);
        barrel.Setup(CreateSpawnTransform(spawnPos), _rtMortarData, targetPosition);
    }

    private void VisualShoot()
    {
        if (_visualPool == null || _shootPoint == null) return;

        var rb = _visualPool.Get();
        rb.transform.position = _shootPoint.position;
        rb.transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.up * _rtMortarData.visualShootForce;

        if (_shootParticle != null)
        {
            _shootParticle.gameObject.SetActive(true);
            _shootParticle.Emit(1);
        }

        StartCoroutine(ReturnVisualAfterSeconds(rb, _rtMortarData.visualLifetime));
    }

    private IEnumerator ReturnVisualAfterSeconds(Rigidbody rb, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (rb != null && rb.gameObject.activeSelf)
            _visualPool.Return(rb);
    }

    private Transform CreateSpawnTransform(Vector3 pos)
    {
        var go = new GameObject("BarrelSpawnPoint");
        go.transform.position = pos;
        Destroy(go, 3f);
        return go.transform;
    }
}