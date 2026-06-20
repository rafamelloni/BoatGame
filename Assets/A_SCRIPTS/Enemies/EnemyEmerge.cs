using System.Collections;
using UnityEngine;

public class EnemyEmerge : MonoBehaviour
{
    [Header("Emerge")]
    [SerializeField] private float _startDepth = -5f;
    [SerializeField] private float _targetHeight = 5.3f;
    [SerializeField] private float _emergeSpeed = 3f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _tiltAngle = 45f;
    [SerializeField] private float _tiltSmoothSpeed = 5f;

    private Transform _player;
    private Behaviour[] _behaviours;
    private bool _emerging = false;

    public void Emerge(Transform player)
    {
        if (_emerging) return;
        _player = player;

        Vector3 pos = transform.position;
        pos.y = _startDepth;
        transform.position = pos;

        _behaviours = CollectBehaviours();
        foreach (var b in _behaviours)
            b.enabled = false;

        _emerging = true;
        StartCoroutine(EmergeRoutine());
    }

    public void Reset()
    {
        StopAllCoroutines();
        _emerging = false;
        enabled = true;

        if (_behaviours != null)
            foreach (var b in _behaviours)
                if (b != null) b.enabled = true;

        _behaviours = null;
    }

    private IEnumerator EmergeRoutine()
    {
        while (transform.position.y < _targetHeight)
        {
            Vector3 pos = transform.position;
            pos.y += _emergeSpeed * Time.deltaTime;
            pos.y = Mathf.Min(pos.y, _targetHeight);

            if (_player != null)
            {
                Vector3 dir = _player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    dir.Normalize();
                    pos.x += dir.x * _moveSpeed * Time.deltaTime;
                    pos.z += dir.z * _moveSpeed * Time.deltaTime;

                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _tiltSmoothSpeed * Time.deltaTime);
                }
            }

            transform.position = pos;

            float progress = Mathf.InverseLerp(_startDepth, _targetHeight, transform.position.y);
            float currentTilt = Mathf.Lerp(_tiltAngle, 0f, progress);
            Vector3 euler = transform.eulerAngles;
            euler.x = currentTilt;
            transform.eulerAngles = euler;

            yield return null;
        }

        Vector3 finalEuler = transform.eulerAngles;
        finalEuler.x = 0f;
        transform.eulerAngles = finalEuler;

        foreach (var b in _behaviours)
            b.enabled = true;

        _emerging = false;
        enabled = false;
    }

    private Behaviour[] CollectBehaviours()
    {
        var list = new System.Collections.Generic.List<Behaviour>();

        var group = GetComponent<EnemyGroup>();
        if (group != null)
        {
            list.Add(group);
            foreach (var b in GetComponentsInChildren<BasicEnemy>())
                list.Add(b);
            foreach (var b in GetComponentsInChildren<BasicEnemyShoot>())
                list.Add(b);

        }

        var ship = GetComponent<ShipEnemy>();
        if (ship != null) list.Add(ship);

        var rafa = GetComponent<RafaEnemy>();
        if (rafa != null) list.Add(rafa);

        var dashBoss = GetComponent<DashBossEnemy>();
        if (dashBoss != null) list.Add(dashBoss);

        return list.ToArray();
    }
}