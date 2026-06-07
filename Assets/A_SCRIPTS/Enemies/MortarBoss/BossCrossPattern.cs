using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossCrossPattern : MonoBehaviour
{
    [Header("Patrón")]
    [SerializeField] private int _orbsPerLine = 5;
    [SerializeField] private float _spacing = 1.5f;
    [SerializeField] private float _orbGrowDuration = 1.5f;

    [Header("Modo")]
    [SerializeField] private bool _sequential = false;
    [SerializeField] private float _sequentialDelay = 0.1f;

    [Header("Altura")]
    [SerializeField] private float _spawnHeight = 0f;

    [Header("Referencias")]
    [SerializeField] private BossBarrelAttack _barrelAttack;
    [SerializeField] private Transform _player;

    [Header("Debug")]
    [SerializeField] private KeyCode _debugKey = KeyCode.Z;

    private List<WarningOrb> _activeOrbs = new List<WarningOrb>();

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            FireCross();
    }

    // llamado desde el loop normal del controller
    public void FireCross()
    {
        FireCross(transform.position);
    }

    // llamado desde el charge con posición exacta del momento del disparo
    public void FireCross(Vector3 origin)
    {
        

        Vector3 forward = Vector3.forward;
        if (_player != null)
        {
            Vector3 dir = _player.position - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                forward = dir.normalized;
        }

        if (_sequential)
            StartCoroutine(FireSequential(forward, origin));
        else
            FireSimultaneous(forward, origin);
    }

    private void ForceReturnAll()
    {
        var copy = new List<WarningOrb>(_activeOrbs);
        _activeOrbs.Clear();

        foreach (var orb in copy)
        {
            if (orb != null && orb.gameObject.activeSelf)
                WarningOrbPool.Instance.Return(orb);
        }
    }

    private void FireSimultaneous(Vector3 forward, Vector3 origin)
    {
        foreach (var pos in GetCrossPositions(forward, origin))
            SpawnOrb(pos);
    }

    private IEnumerator FireSequential(Vector3 forward, Vector3 origin)
    {
        var positions = GetCrossPositions(forward, origin);
        positions.Sort((a, b) =>
            Vector3.Distance(a, origin)
            .CompareTo(Vector3.Distance(b, origin)));

        int i = 0;
        while (i < positions.Count)
        {
            float currentDist = Mathf.Round(Vector3.Distance(positions[i], origin) * 100f);
            while (i < positions.Count &&
                   Mathf.Round(Vector3.Distance(positions[i], origin) * 100f) == currentDist)
            {
                SpawnOrb(positions[i]);
                i++;
            }
            yield return new WaitForSeconds(_sequentialDelay);
        }
    }

    private void SpawnOrb(Vector3 pos)
    {
        var orb = WarningOrbPool.Instance.Get();
        orb.transform.position = pos;
        _activeOrbs.Add(orb);
        _barrelAttack.QueueVisual();
        orb.Trigger(_orbGrowDuration, () =>
        {
            _activeOrbs.Remove(orb);
            WarningOrbPool.Instance.Return(orb);
            _barrelAttack.SpawnBarrel(pos);
        });
    }

    private List<Vector3> GetCrossPositions(Vector3 forward, Vector3 origin)
    {
        var positions = new List<Vector3>();
        Vector3 o = new Vector3(origin.x, _spawnHeight, origin.z);
        Vector3 right = new Vector3(-forward.z, 0f, forward.x);

        for (int i = 1; i <= _orbsPerLine; i++)
        {
            float dist = i * _spacing;
            positions.Add(o + forward * dist);
            positions.Add(o - forward * dist);
            positions.Add(o + right * dist);
            positions.Add(o - right * dist);
        }

        return positions;
    }

    public void FireCross8(Vector3 origin)
    {
        // ForceReturnAll();
        Debug.Log("[FireCross8] llamado");

        Vector3 forward = Vector3.forward;
        if (_player != null)
        {
            Vector3 dir = _player.position - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                forward = dir.normalized;
        }

        var positions = new List<Vector3>();
        Vector3 o = new Vector3(origin.x, _spawnHeight, origin.z);
        Vector3 right = new Vector3(-forward.z, 0f, forward.x);

        // 4 diagonales: forward+right, forward-right, -forward+right, -forward-right
        Vector3 diagFR = (forward + right).normalized;
        Vector3 diagFL = (forward - right).normalized;
        Vector3 diagBR = (-forward + right).normalized;
        Vector3 diagBL = (-forward - right).normalized;

        Vector3[] dirs = { forward, -forward, right, -right, diagFR, diagFL, diagBR, diagBL };

        foreach (var d in dirs)
            for (int i = 1; i <= _orbsPerLine; i++)
                positions.Add(o + d * (i * _spacing));
        Debug.Log($"[FireCross8] Generando {positions.Count} posiciones");
        foreach (var pos in positions)
            SpawnOrb(pos);
    }

    public void FireCross8() => FireCross8(transform.position);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Vector3 origin = transform.position;
        for (int i = 1; i <= _orbsPerLine; i++)
        {
            float dist = i * _spacing;
            Gizmos.DrawWireSphere(origin + Vector3.right * dist, 0.2f);
            Gizmos.DrawWireSphere(origin + Vector3.left * dist, 0.2f);
            Gizmos.DrawWireSphere(origin + Vector3.forward * dist, 0.2f);
            Gizmos.DrawWireSphere(origin + Vector3.back * dist, 0.2f);
        }
    }
}