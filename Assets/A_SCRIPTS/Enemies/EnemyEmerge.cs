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

    [Header("Partículas")]
    [SerializeField] private GameObject _emergeParticlePrefab;
    [SerializeField] private float _spawnParticleYOffset = 5f;

    [Header("Turbulencia del agua")]
    [Tooltip("Segundos que el agua se agita antes de que el enemigo empiece a subir (0 = sube directo)")]
    [SerializeField] private float _telegraphTime = 0f;
    [SerializeField] private float _turbulenceRadius = 4f;
    [SerializeField] private float _turbulenceFadeIn = 0.6f;
    [SerializeField] private float _turbulenceFadeOut = 1f;

    private Transform _player;
    private Behaviour[] _behaviours;
    private bool _emerging = false;
    private TurbulenceSource _turbulence;

    public bool IsEmerging => _emerging;

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

        if (WaterTurbulenceManager.Instance != null)
            _turbulence = WaterTurbulenceManager.Instance.Create(transform.position, _turbulenceRadius);

        if (_emergeParticlePrefab != null && ParticlePool.Instance != null)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.y += _spawnParticleYOffset;
            GameObject particle = ParticlePool.Instance.GetParticle(_emergeParticlePrefab, spawnPos);
            if (particle != null)
            {
                ParticleSystem ps = particle.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }
        }

        StartCoroutine(EmergeRoutine());
    }

    public void Reset()
    {
        StopAllCoroutines();
        ReleaseTurbulence(0f);
        _emerging = false;
        enabled = true;
        if (_behaviours != null)
            foreach (var b in _behaviours)
                if (b != null) b.enabled = true;
        _behaviours = null;
    }

    private void OnDisable()
    {
        // si lo matan o lo devuelven al pool a mitad de camino, que la turbulencia se apague sola
        ReleaseTurbulence(_turbulenceFadeOut);
    }

    private IEnumerator EmergeRoutine()
    {
        float telegraph = 0f;
        while (telegraph < _telegraphTime)
        {
            telegraph += Time.deltaTime;
            UpdateTurbulence();
            yield return null;
        }

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
            UpdateTurbulence();
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

        ReleaseTurbulence(_turbulenceFadeOut);

        yield return new WaitForSeconds(1f);
        foreach (var b in _behaviours)
            b.enabled = true;
        _emerging = false;
        enabled = false;
    }

    private void UpdateTurbulence()
    {
        if (_turbulence == null) return;
        _turbulence.Position = transform.position;
        float step = _turbulenceFadeIn > 0f ? Time.deltaTime / _turbulenceFadeIn : 1f;
        _turbulence.Intensity = Mathf.MoveTowards(_turbulence.Intensity, 1f, step);
    }

    private void ReleaseTurbulence(float fadeOut)
    {
        if (_turbulence == null) return;
        if (WaterTurbulenceManager.Instance != null)
            WaterTurbulenceManager.Instance.Release(_turbulence, fadeOut);
        _turbulence = null;
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
        var zombieGroup = GetComponent<ZombieGroup>();
        if (zombieGroup != null)
        {
            list.Add(zombieGroup);
            foreach (var b in GetComponentsInChildren<ZombieEnemy>())
                list.Add(b);
        }
        var ship = GetComponent<ShipEnemy>();
        if (ship != null) list.Add(ship);
        var rafa = GetComponent<RafaEnemy>();
        if (rafa != null) list.Add(rafa);
        var dashBoss = GetComponent<DashBossEnemy>();
        if (dashBoss != null) list.Add(dashBoss);
        var spike = GetComponent<SpikeBallEnemy>();
        if (spike != null) list.Add(spike);
        var heavy = GetComponent<HeavyRangedEnemy>();
        if (heavy != null) list.Add(heavy);
        return list.ToArray();
    }
}
