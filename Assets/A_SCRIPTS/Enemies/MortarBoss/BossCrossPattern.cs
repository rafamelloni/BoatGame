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

    [Header("Debug")]
    [SerializeField] private KeyCode _debugKey = KeyCode.Z;

    [Header("Player")]
    [SerializeField] private Transform _player;

    private List<WarningOrb> _activeOrbs = new List<WarningOrb>();

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            FireCross();
    }

    public void FireCross()
    {
        ForceReturnAll();

        Vector3 forward = Vector3.forward;

        if (_player != null)
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                forward = dir.normalized;
        }

        if (_sequential)
            StartCoroutine(FireSequential(forward));
        else
            FireSimultaneous(forward);
    }

    private void ForceReturnAll()
    {
        foreach (var orb in _activeOrbs)
        {
            if (orb != null && orb.gameObject.activeSelf)
                WarningOrbPool.Instance.Return(orb);
        }
        _activeOrbs.Clear();
    }

    private void FireSimultaneous(Vector3 forward)
    {
        foreach (var pos in GetCrossPositions(forward))
            SpawnOrb(pos);
    }

    private IEnumerator FireSequential(Vector3 forward)
    {
        var positions = GetCrossPositions(forward);
        positions.Sort((a, b) =>
            Vector3.Distance(a, transform.position)
            .CompareTo(Vector3.Distance(b, transform.position)));

        int i = 0;
        while (i < positions.Count)
        {
            float currentDist = Mathf.Round(Vector3.Distance(positions[i], transform.position) * 100f);
            while (i < positions.Count &&
                   Mathf.Round(Vector3.Distance(positions[i], transform.position) * 100f) == currentDist)
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
        orb.Trigger(_orbGrowDuration, () =>
        {
            _activeOrbs.Remove(orb);
            WarningOrbPool.Instance.Return(orb);
        });
    }

    private List<Vector3> GetCrossPositions(Vector3 forward)
    {
        var positions = new List<Vector3>();
        Vector3 origin = new Vector3(transform.position.x, _spawnHeight, transform.position.z);
        Vector3 right = new Vector3(-forward.z, 0f, forward.x);

        for (int i = 1; i <= _orbsPerLine; i++)
        {
            float dist = i * _spacing;
            positions.Add(origin + forward * dist);
            positions.Add(origin - forward * dist);
            positions.Add(origin + right * dist);
            positions.Add(origin - right * dist);
        }

        return positions;
    }

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
