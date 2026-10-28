using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Torre que dispara patrones tipo bullet hell en vez de balas comunes:
//  - Espiral: chorro de balas que gira (1 o más brazos).
//  - Espiral Doble: dos espirales girando para lados opuestos (se cruzan).
//  - Anillos: anillos de balas con un hueco para escapar, cada anillo un poco girado.
// Las balas son lentas: la gracia es esquivarlas navegando.
public class SpiralTower : SpecialTower
{
    public enum Pattern { Espiral, EspiralDoble, Anillos }

    [Header("Patrón")]
    [SerializeField] private Pattern _pattern = Pattern.Espiral;
    [Tooltip("De dónde salen las balas. Vacío = 1.5 m arriba de la torre")]
    [SerializeField] private Transform _firePoint;

    [Header("Espiral")]
    [Tooltip("Cuánto dura el chorro (segundos)")]
    [SerializeField, Min(0.1f)] private float _spiralDuration = 2.5f;
    [Tooltip("Balas por segundo de cada brazo")]
    [SerializeField, Min(1f)] private float _shotsPerSecond = 10f;
    [Tooltip("Velocidad de giro (grados por segundo)")]
    [SerializeField] private float _spinSpeed = 200f;
    [Tooltip("Cantidad de brazos de la espiral")]
    [SerializeField, Range(1, 8)] private int _arms = 3;

    [Header("Anillos")]
    [SerializeField, Range(1, 10)] private int _rings = 3;
    [SerializeField, Range(4, 48)] private int _bulletsPerRing = 16;
    [SerializeField, Min(0.05f)] private float _timeBetweenRings = 0.5f;
    [Tooltip("Cuánto gira cada anillo respecto del anterior (grados)")]
    [SerializeField] private float _ringRotation = 11f;
    [Tooltip("Balas que faltan en cada anillo (el hueco por donde escapar). 0 = anillo cerrado")]
    [SerializeField, Range(0, 12)] private int _gapSize = 3;

    [Header("Balas")]
    [Tooltip("Prefab con TowerProjectile. Vacío = esfera simple")]
    [SerializeField] private TowerProjectile _projectilePrefab;
    [SerializeField, Min(0.1f)] private float _bulletSpeed = 7f;
    [SerializeField, Min(0f)] private float _bulletDamage = 8f;
    [SerializeField, Min(0.1f)] private float _bulletLifetime = 4.5f;
    [SerializeField, Min(0.05f)] private float _bulletSize = 0.6f;
    [Tooltip("Color de la esfera simple (solo si no hay prefab)")]
    [SerializeField] private Color _fallbackColor = new Color(1f, 0.35f, 0.75f);

    private readonly List<TowerProjectile> _pool = new List<TowerProjectile>();
    private Material _fallbackMat;

    protected override IEnumerator Attack()
    {
        switch (_pattern)
        {
            case Pattern.Anillos: yield return RingsRoutine(); break;
            case Pattern.EspiralDoble: yield return SpiralRoutine(true); break;
            default: yield return SpiralRoutine(false); break;
        }
    }

    private IEnumerator SpiralRoutine(bool doble)
    {
        float angle = AngleToPlayer();
        float angleB = angle;
        float t = 0f;
        float acc = 0f;
        float interval = 1f / _shotsPerSecond;

        while (t < _spiralDuration)
        {
            float dt = Time.deltaTime;
            t += dt;
            acc += dt;
            angle += _spinSpeed * dt;
            angleB -= _spinSpeed * dt;

            while (acc >= interval)
            {
                acc -= interval;
                for (int a = 0; a < _arms; a++)
                {
                    float offset = a * 360f / _arms;
                    Shoot(angle + offset);
                    if (doble) Shoot(angleB + offset);
                }
            }
            yield return null;
        }
    }

    private IEnumerator RingsRoutine()
    {
        // cada anillo tiene el hueco en un lugar al azar, así hay que moverse para encontrarlo
        float baseAngle = Random.Range(0f, 360f);
        float step = 360f / _bulletsPerRing;

        for (int r = 0; r < _rings; r++)
        {
            int gapStart = Random.Range(0, _bulletsPerRing);
            for (int i = 0; i < _bulletsPerRing; i++)
            {
                int fromGap = (i - gapStart + _bulletsPerRing) % _bulletsPerRing;
                if (fromGap < _gapSize) continue;
                Shoot(baseAngle + i * step);
            }
            baseAngle += _ringRotation;
            if (r < _rings - 1) yield return new WaitForSeconds(_timeBetweenRings);
        }
    }

    private float AngleToPlayer()
    {
        if (Player == null) return 0f;
        Vector3 d = Player.position - transform.position;
        return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }

    private void Shoot(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 pos = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1.5f;
        GetProjectile().Launch(pos, dir, _bulletSpeed, _bulletDamage, _bulletLifetime, _bulletSize);
    }

    private TowerProjectile GetProjectile()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i] != null && _pool[i].IsFree) return _pool[i];

        TowerProjectile p = _projectilePrefab != null ? Instantiate(_projectilePrefab) : CreateFallback();
        p.gameObject.SetActive(false);
        _pool.Add(p);
        return p;
    }

    private TowerProjectile CreateFallback()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "TowerBullet";
        int layer = LayerMask.NameToLayer("EnemyBulletLayer");
        if (layer >= 0) go.layer = layer;

        if (_fallbackMat == null)
        {
            var src = go.GetComponent<Renderer>().sharedMaterial;
            _fallbackMat = new Material(src);
            if (_fallbackMat.HasProperty("_BaseColor")) _fallbackMat.SetColor("_BaseColor", _fallbackColor);
            if (_fallbackMat.HasProperty("_Color")) _fallbackMat.SetColor("_Color", _fallbackColor);
            if (_fallbackMat.HasProperty("_EmissionColor"))
            {
                _fallbackMat.EnableKeyword("_EMISSION");
                _fallbackMat.SetColor("_EmissionColor", _fallbackColor * 0.6f);
            }
        }
        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = _fallbackMat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.AddComponent<TowerProjectile>();
    }

    protected override void OnTowerReset()
    {
        foreach (var p in _pool)
            if (p != null && !p.IsFree) p.Despawn();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var p in _pool)
            if (p != null) Destroy(p.gameObject);
        if (_fallbackMat != null) Destroy(_fallbackMat);
    }
}
