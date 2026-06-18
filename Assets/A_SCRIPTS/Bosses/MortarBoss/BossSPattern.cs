using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSPattern : MonoBehaviour
{
    [Header("Patrón")]
    [SerializeField] private int _orbsPerLine = 5;
    [SerializeField] private float _spacing = 1.5f;
    [SerializeField] private float _orbGrowDuration = 1.5f;

    [Header("Curva")]
    [SerializeField] private float _curveAmount = 3f;

    [Header("Modo")]
    [SerializeField] private bool _sequential = false;
    [SerializeField] private float _sequentialDelay = 0.1f;

    [Header("Altura")]
    [SerializeField] private float _spawnHeight = 0f;

    [Header("Player")]
    [SerializeField] private Transform _player;

    [Header("Debug")]
    [SerializeField] private KeyCode _debugKey = KeyCode.Space;

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            FireS();
    }

    [ContextMenu("Fire S")]
    public void FireS()
    {
        if (_player != null)
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        if (_sequential)
            StartCoroutine(FireSequential());
        else
            FireSimultaneous();
    }

    private void FireSimultaneous()
    {
        foreach (var pos in GetSPositions())
            SpawnOrb(pos);
    }

    private IEnumerator FireSequential()
    {
        var positions = GetSPositions();
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

    private List<Vector3> GetSPositions()
    {
        var positions = new List<Vector3>();
        Vector3 origin = new Vector3(transform.position.x, _spawnHeight, transform.position.z);

        for (int i = 1; i <= _orbsPerLine; i++)
        {
            float t = (float)i / _orbsPerLine;
            float dist = i * _spacing;
            float curve = Mathf.Sin(t * Mathf.PI) * _curveAmount;

            positions.Add(origin + transform.forward * dist + transform.right * curve);
            positions.Add(origin - transform.forward * dist - transform.right * curve);
            positions.Add(origin + transform.right * dist + transform.forward * curve);
            positions.Add(origin - transform.right * dist - transform.forward * curve);
        }

        return positions;
    }

    private void SpawnOrb(Vector3 pos)
    {
        var orb = WarningOrbPool.Instance.Get();
        orb.transform.position = pos;
        orb.Trigger(_orbGrowDuration, () => WarningOrbPool.Instance.Return(orb));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Vector3 origin = new Vector3(transform.position.x, _spawnHeight, transform.position.z);

        for (int i = 1; i <= _orbsPerLine; i++)
        {
            float t = (float)i / _orbsPerLine;
            float dist = i * _spacing;
            float curve = Mathf.Sin(t * Mathf.PI) * _curveAmount;

            Gizmos.DrawWireSphere(origin + transform.forward * dist + transform.right * curve, 0.2f);
            Gizmos.DrawWireSphere(origin - transform.forward * dist - transform.right * curve, 0.2f);
            Gizmos.DrawWireSphere(origin + transform.right * dist + transform.forward * curve, 0.2f);
            Gizmos.DrawWireSphere(origin - transform.right * dist - transform.forward * curve, 0.2f);
        }
    }
}