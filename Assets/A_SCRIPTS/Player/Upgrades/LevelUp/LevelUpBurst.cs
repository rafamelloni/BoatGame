using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Efecto al subir de nivel: cura un poco, sale un anillo amarillo del barco que empuja
// a los enemigos, destellos, y recién después se abre el pergamino de mejoras.
public class LevelUpBurst : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _origin;            // el barco
    [SerializeField] private RT_PlayerHealth _playerHealth;

    [Header("Curación")]
    [SerializeField] private float _healAmount = 5f;

    [Header("Anillo")]
    [SerializeField] private Material _ringMaterial;
    [SerializeField] private Color _ringColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float _ringStartRadius = 0.5f;
    [SerializeField] private float _ringEndRadius = 12f;
    [SerializeField] private float _ringStartWidth = 0.8f;
    [SerializeField] private float _ringEndWidth = 0.05f;
    [SerializeField] private float _ringDuration = 0.35f;
    [SerializeField] private float _ringHeight = 0.3f;
    [SerializeField] private int _ringSegments = 64;

    [Header("Empuje")]
    [SerializeField] private bool _pushEnemies = true;
    [SerializeField] private LayerMask _enemyLayers = ~0;
    [SerializeField] private float _pushDistance = 6f;          // cuánto se mueve un enemigo pegado al barco
    [SerializeField, Range(0f, 1f)] private float _pushAtEdge = 0.3f; // fracción del empuje en el borde del anillo
    [SerializeField] private float _pushDuration = 0.25f;

    [Header("Destellos")]
    [SerializeField] private ParticleSystem _sparklesPrefab;

    [Header("Tiempo")]
    [SerializeField, Range(0.05f, 1f)] private float _slowMoScale = 0.25f;
    [SerializeField] private float _holdAfterRing = 0.15f;

    private LineRenderer _ring;
    private Coroutine _routine;

    private class PushTarget
    {
        public Transform target;
        public float distance;     // distancia al barco al inicio
        public Vector3 offset;     // desplazamiento total a aplicar
        public float applied;      // 0..1 cuánto ya se aplicó
        public bool started;
        public float t;
    }

    private readonly List<PushTarget> _pushTargets = new List<PushTarget>();
    private readonly HashSet<Enemy> _seen = new HashSet<Enemy>();

    private void Awake()
    {
        if (_origin == null) _origin = transform;

        GameObject ringGO = new GameObject("LevelUpRing");
        ringGO.transform.SetParent(transform, false);
        _ring = ringGO.AddComponent<LineRenderer>();
        _ring.loop = true;
        _ring.useWorldSpace = true;
        _ring.positionCount = _ringSegments;
        _ring.material = _ringMaterial;
        _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _ring.receiveShadows = false;
        _ring.enabled = false;
    }

    private void OnDisable()
    {
        // Si se resetea la escena en medio del efecto, que no quede el anillo colgado
        if (_ring != null) _ring.enabled = false;
        _routine = null;
        _pushTargets.Clear();
    }

    public void Play(Action onComplete)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(BurstRoutine(onComplete));
    }

    private IEnumerator BurstRoutine(Action onComplete)
    {
        // 1. Curar primero, así no sube de nivel y se muere a la vez
        if (_playerHealth != null)
            _playerHealth.Heal(_healAmount);

        // 2. Slow-mo
        Time.timeScale = _slowMoScale;

        // 3. Destellos
        if (_sparklesPrefab != null)
        {
            ParticleSystem ps = Instantiate(_sparklesPrefab, _origin.position, Quaternion.identity);
            var main = ps.main;
            main.useUnscaledTime = true; // siguen moviéndose aunque el juego esté pausado
            ps.Play();
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.5f);
        }

        // 4. Juntar enemigos dentro del radio
        CollectPushTargets();

        // 5. Anillo expandiéndose + empuje cuando el anillo llega a cada enemigo
        _ring.enabled = true;
        float t = 0f;
        while (t < 1f)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt / _ringDuration;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

            float radius = Mathf.Lerp(_ringStartRadius, _ringEndRadius, eased);
            float width = Mathf.Lerp(_ringStartWidth, _ringEndWidth, eased);
            Color c = _ringColor;
            c.a = Mathf.Lerp(_ringColor.a, 0f, eased);

            _ring.startWidth = width;
            _ring.endWidth = width;
            _ring.startColor = c;
            _ring.endColor = c;
            UpdateRingPositions(radius);

            UpdatePushes(radius, dt);
            yield return null;
        }
        _ring.enabled = false;

        // Terminar los empujes que todavía estén en curso
        float hold = 0f;
        while (hold < _holdAfterRing)
        {
            float dt = Time.unscaledDeltaTime;
            hold += dt;
            UpdatePushes(float.MaxValue, dt);
            yield return null;
        }
        FinishPushes();

        _routine = null;

        // 6. Lo que siga (abrir pergamino, que pone timeScale en 0)
        if (onComplete != null) onComplete();
        else Time.timeScale = 1f;
    }

    private void CollectPushTargets()
    {
        _pushTargets.Clear();
        _seen.Clear();
        if (!_pushEnemies) return;

        Vector3 center = _origin.position;
        Collider[] hits = Physics.OverlapSphere(center, _ringEndRadius, _enemyLayers, QueryTriggerInteraction.Collide);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            if (!_seen.Add(enemy)) continue;

            Vector3 dir = enemy.transform.position - center;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.01f) dir = UnityEngine.Random.insideUnitSphere;
            dir.y = 0f;
            dir.Normalize();

            float falloff = Mathf.Lerp(1f, _pushAtEdge, Mathf.Clamp01(dist / _ringEndRadius));

            _pushTargets.Add(new PushTarget
            {
                target = enemy.transform,
                distance = dist,
                offset = dir * _pushDistance * falloff
            });
        }
    }

    private void UpdatePushes(float ringRadius, float dt)
    {
        foreach (PushTarget p in _pushTargets)
        {
            if (p.target == null || !p.target.gameObject.activeInHierarchy) continue;
            if (!p.started && ringRadius >= p.distance) p.started = true;
            if (!p.started || p.applied >= 1f) continue;

            p.t = Mathf.Min(1f, p.t + dt / _pushDuration);
            float eased = 1f - Mathf.Pow(1f - p.t, 3f);
            float delta = eased - p.applied;
            p.target.position += p.offset * delta;
            p.applied = eased;
        }
    }

    private void FinishPushes()
    {
        foreach (PushTarget p in _pushTargets)
        {
            if (p.target == null || !p.target.gameObject.activeInHierarchy) continue;
            float remaining = 1f - p.applied;
            if (remaining > 0f) p.target.position += p.offset * remaining;
        }
        _pushTargets.Clear();
    }

    private void UpdateRingPositions(float radius)
    {
        Vector3 center = _origin.position + Vector3.up * _ringHeight;
        for (int i = 0; i < _ringSegments; i++)
        {
            float a = (float)i / _ringSegments * Mathf.PI * 2f;
            _ring.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform o = _origin != null ? _origin : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(o.position, _ringEndRadius);
    }
}
