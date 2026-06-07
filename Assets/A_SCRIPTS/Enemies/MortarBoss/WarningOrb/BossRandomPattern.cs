using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRandomPattern : MonoBehaviour
{
    [Header("Patrón")]
    [SerializeField] private int _orbCount = 10;
    [SerializeField] private float _minRadius = 5f;
    [SerializeField] private float _maxRadius = 20f;
    [SerializeField] private float _orbGrowDuration = 1.5f;
    [SerializeField] private float _minDistBetweenOrbs = 3f;

    [Header("Modo")]
    [SerializeField] private bool _sequential = false;
    [SerializeField] private float _sequentialDelay = 0.1f;

    [Header("Altura")]
    [SerializeField] private float _spawnHeight = 0f;

    [Header("Debug")]
    [SerializeField] private KeyCode _debugKey = KeyCode.Space;
    [SerializeField] private BossBarrelAttack _barrelAttack;

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            FireRandom();
    }

    // llamado desde el loop normal del controller
    [ContextMenu("Fire Random")]
    public void FireRandom()
    {
        FireRandom(transform.position);
    }

    // llamado desde el charge con posición exacta del momento del disparo
    public void FireRandom(Vector3 origin)
    {
        if (_sequential)
            StartCoroutine(FireSequential(origin));
        else
            FireSimultaneous(origin);
    }

    private void FireSimultaneous(Vector3 origin)
    {
        foreach (var pos in GetRandomPositions(origin))
            SpawnOrb(pos);
    }

    private IEnumerator FireSequential(Vector3 origin)
    {
        foreach (var pos in GetRandomPositions(origin))
        {
            SpawnOrb(pos);
            yield return new WaitForSeconds(_sequentialDelay);
        }
    }

    private List<Vector3> GetRandomPositions(Vector3 origin)
    {
        var positions = new List<Vector3>();
        Vector3 o = new Vector3(origin.x, _spawnHeight, origin.z);
        int maxAttempts = 100;

        for (int i = 0; i < _orbCount; i++)
        {
            int attempts = 0;
            Vector3 candidate;
            do
            {
                Vector2 random = Random.insideUnitCircle.normalized * Random.Range(_minRadius, _maxRadius);
                candidate = o + new Vector3(random.x, 0f, random.y);
                attempts++;
            }
            while (!IsValidPosition(candidate, positions) && attempts < maxAttempts);

            if (attempts < maxAttempts)
                positions.Add(candidate);
        }

        return positions;
    }

    private bool IsValidPosition(Vector3 candidate, List<Vector3> existing)
    {
        foreach (var pos in existing)
        {
            if (Vector3.Distance(candidate, pos) < _minDistBetweenOrbs)
                return false;
        }
        return true;
    }

    private void SpawnOrb(Vector3 pos)
    {
        var orb = WarningOrbPool.Instance.Get();
        orb.transform.position = pos;
        _barrelAttack.QueueVisual();
        orb.Trigger(_orbGrowDuration, () =>
        {
            WarningOrbPool.Instance.Return(orb);
            _barrelAttack.SpawnBarrel(pos);
        });
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = new Vector3(transform.position.x, _spawnHeight, transform.position.z);
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(origin, _maxRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(origin, _minRadius);
    }
}