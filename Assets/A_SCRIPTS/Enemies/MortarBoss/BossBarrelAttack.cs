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

    private RT_MortarData _rtMortarData;
    private int _pendingVisuals = 0;
    private Coroutine _visualRoutine;


    private void Start()
    {
        _rtMortarData = new RT_MortarData(_mortarData);
    }


    public void QueueVisual()
    {
        _pendingVisuals++;
        if (_visualRoutine != null) StopCoroutine(_visualRoutine);
        _visualRoutine = StartCoroutine(FireVisualsSequential());
    }

    private IEnumerator FireVisualsSequential()
    {
        yield return null; // espera un frame para que lleguen todas las llamadas
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
        if (_mortarData.visualProjectilePrefab == null || _shootPoint == null) return;
        GameObject proj = Instantiate(_mortarData.visualProjectilePrefab, _shootPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.up * _rtMortarData.visualShootForce;
        StartCoroutine(DisableAfterSeconds(proj, _rtMortarData.visualLifetime));
    }

    private IEnumerator DisableAfterSeconds(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private Transform CreateSpawnTransform(Vector3 pos)
    {
        var go = new GameObject("BarrelSpawnPoint");
        go.transform.position = pos;
        Destroy(go, 3f);
        return go.transform;
    }
}