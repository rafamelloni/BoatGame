using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Torre mortero: tira balas en parábola que caen donde vas a estar.
// Marca en el agua un círculo que se llena mientras la bala vuela: cuando se llena, cae.
// Tira ráfagas de varias balas: la primera apunta a donde vas a estar y las demás se abren alrededor.
public class MortarTower : SpecialTower
{
    [Header("Mortero")]
    [Tooltip("De dónde salen las balas. Vacío = 2 m arriba de la torre")]
    [SerializeField] private Transform _firePoint;
    [SerializeField, Range(1, 12)] private int _shellsPerVolley = 3;
    [SerializeField, Min(0f)] private float _timeBetweenShells = 0.35f;
    [Tooltip("Cuánto se adelanta a hacia dónde vas. 0 = tira donde estás, 1 = donde vas a estar cuando caiga")]
    [SerializeField, Range(0f, 1.5f)] private float _lead = 0.8f;
    [Tooltip("Qué tan desparramadas caen las balas que no son la primera (metros)")]
    [SerializeField, Min(0f)] private float _spread = 4f;
    [Tooltip("Segundos que tarda en caer (= tiempo que tenés para esquivar)")]
    [SerializeField, Min(0.3f)] private float _flightTime = 1.6f;
    [Tooltip("Altura máxima de la parábola (metros)")]
    [SerializeField, Min(0f)] private float _arcHeight = 12f;

    [Header("Impacto")]
    [SerializeField, Min(0f)] private float _damage = 25f;
    [Tooltip("Radio de daño al caer (y tamaño del círculo de aviso)")]
    [SerializeField, Min(0.2f)] private float _blastRadius = 2.8f;
    [Tooltip("Partícula al caer (tiene que estar en el ParticlePool). Opcional.")]
    [SerializeField] private GameObject _landingVfx;
    [Tooltip("Tamaño del anillo de espuma en el agua al caer")]
    [SerializeField] private float _splashSize = 1.6f;

    [Header("Visual")]
    [Tooltip("Prefab con TowerShell. Vacío = esfera simple")]
    [SerializeField] private TowerShell _shellPrefab;
    [SerializeField, Min(0.05f)] private float _shellSize = 0.9f;
    [Tooltip("Prefab con TowerWarningCircle. Vacío = dos discos simples")]
    [SerializeField] private TowerWarningCircle _warningPrefab;
    [SerializeField] private Color _shellColor = new Color(0.15f, 0.15f, 0.15f);
    [Tooltip("Color del aro de aviso")]
    [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.18f, 1f);
    [Tooltip("Color del relleno del aviso (el alpha es la opacidad máxima)")]
    [SerializeField] private Color _warningFillColor = new Color(1f, 0.42f, 0.2f, 0.55f);
    [Tooltip("Estela de la bala (en blanco se desvanece)")]
    [SerializeField] private Color _trailColor = new Color(1f, 0.85f, 0.6f, 0.8f);

    private readonly List<TowerShell> _shells = new List<TowerShell>();
    private readonly List<TowerWarningCircle> _warnings = new List<TowerWarningCircle>();
    private Material _shellMat, _trailMat;

    protected override IEnumerator Attack()
    {
        for (int i = 0; i < _shellsPerVolley; i++)
        {
            if (Player == null) yield break;
            FireShell(i == 0);
            if (i < _shellsPerVolley - 1 && _timeBetweenShells > 0f)
                yield return new WaitForSeconds(_timeBetweenShells);
        }
    }

    private void FireShell(bool aimed)
    {
        Vector3 target = Player.position;
        if (PlayerRb != null)
        {
            Vector3 v = PlayerRb.linearVelocity;
            v.y = 0f;
            target += v * (_flightTime * _lead);
        }
        if (!aimed && _spread > 0f)
        {
            Vector2 r = Random.insideUnitCircle * _spread;
            target += new Vector3(r.x, 0f, r.y);
        }
        target.y = Player.position.y; // altura del agua (misma idea que el crosshair)

        Vector3 from = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 2f;
        GetShell().Launch(from, target, _flightTime, _arcHeight, _damage, _blastRadius, _shellSize,
                          Player, GetWarning(), _landingVfx, _splashSize);
    }

    // ---------- pools propios ----------

    private TowerShell GetShell()
    {
        foreach (var s in _shells) if (s != null && s.IsFree) return s;
        TowerShell shell = _shellPrefab != null ? Instantiate(_shellPrefab) : CreateFallbackShell();
        shell.gameObject.SetActive(false);
        _shells.Add(shell);
        return shell;
    }

    private TowerWarningCircle GetWarning()
    {
        foreach (var w in _warnings) if (w != null && w.IsFree) return w;
        TowerWarningCircle warn = _warningPrefab != null ? Instantiate(_warningPrefab) : CreateFallbackWarning();
        warn.gameObject.SetActive(false);
        _warnings.Add(warn);
        return warn;
    }

    private TowerShell CreateFallbackShell()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MortarShell";
        var rend = go.GetComponent<Renderer>();
        if (_shellMat == null) _shellMat = MakeMat(rend.sharedMaterial, _shellColor, false);
        rend.sharedMaterial = _shellMat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // estela corta para que se lea la parábola
        var trail = go.AddComponent<TrailRenderer>();
        if (_trailMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _trailMat = new Material(sh);
        }
        trail.sharedMaterial = _trailMat;
        trail.time = 0.35f;
        trail.minVertexDistance = 0.1f;
        trail.widthMultiplier = 1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0f));
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(_trailColor, 0f), new GradientColorKey(new Color(0.35f, 0.35f, 0.38f), 1f) },
                  new[] { new GradientAlphaKey(_trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        return go.AddComponent<TowerShell>();
    }

    // Aviso generado: aro que late + relleno que crece + flash al caer (ver TowerWarningCircle)
    private TowerWarningCircle CreateFallbackWarning()
    {
        var root = new GameObject("MortarWarning");
        var w = root.AddComponent<TowerWarningCircle>();
        w.BuildDefault(_warningColor, _warningFillColor);
        return w;
    }

    private static Material MakeMat(Material src, Color c, bool glow)
    {
        var m = new Material(src);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (glow && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.5f);
        }
        return m;
    }

    protected override void OnTowerReset()
    {
        foreach (var s in _shells) if (s != null && !s.IsFree) s.Despawn();
        foreach (var w in _warnings) if (w != null) w.Hide();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var s in _shells) if (s != null) Destroy(s.gameObject);
        foreach (var w in _warnings) if (w != null) Destroy(w.gameObject);
        if (_shellMat != null) Destroy(_shellMat);
        if (_trailMat != null) Destroy(_trailMat);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _blastRadius);
    }
}
