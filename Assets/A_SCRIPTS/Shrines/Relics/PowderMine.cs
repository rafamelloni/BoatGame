using UnityEngine;

// Mina de la reliquia "Mina de pólvora". La crea RelicEffects cuando una bala cae al agua.
// Flota unos segundos y explota si un enemigo se acerca. Se arma sola con primitivas
// (si querés usar un modelo tuyo, cambiá BuildVisual).
public class PowderMine : MonoBehaviour
{
    private float _lifetime, _triggerRadius, _explosionRadius, _damage;
    private LayerMask _layers;
    private GameObject _vfx;

    private float _age;
    private float _nextCheck;
    private Vector3 _basePos;
    private Transform _body;
    private Renderer _light;
    private Material _lightMat;
    private readonly Collider[] _hits = new Collider[16];

    private static readonly Color LightOn = new Color(1f, 0.2f, 0.15f);
    private static readonly Color LightOff = new Color(0.35f, 0.08f, 0.06f);

    public static PowderMine Spawn(Vector3 point, float lifetime, float triggerRadius, float explosionRadius,
        float damage, LayerMask layers, GameObject explosionVfx)
    {
        var go = new GameObject("PowderMine");
        go.transform.position = point + Vector3.up * 0.15f;
        var mine = go.AddComponent<PowderMine>();
        mine._lifetime = lifetime;
        mine._triggerRadius = triggerRadius;
        mine._explosionRadius = explosionRadius;
        mine._damage = damage;
        mine._layers = layers;
        mine._vfx = explosionVfx;
        mine._basePos = go.transform.position;
        mine.BuildVisual();
        return mine;
    }

    private void BuildVisual()
    {
        _body = MakePart(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * 0.9f, new Color(0.22f, 0.24f, 0.3f)).transform;
        // pinches
        for (int i = 0; i < 6; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
            var spike = MakePart(PrimitiveType.Cylinder, _body, dir * 0.5f, new Vector3(0.12f, 0.12f, 0.12f), new Color(0.45f, 0.5f, 0.58f));
            spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
        MakePart(PrimitiveType.Cylinder, _body, Vector3.up * 0.5f, new Vector3(0.12f, 0.1f, 0.12f), new Color(0.45f, 0.5f, 0.58f));
        _light = MakePart(PrimitiveType.Sphere, _body, Vector3.up * 0.62f, Vector3.one * 0.14f, LightOn).GetComponent<Renderer>();
        _lightMat = _light.material;
        transform.localScale = Vector3.zero; // aparece con pop
    }

    private static GameObject MakePart(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetColor(r.material, color);
        return go;
    }

    private static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // pop al aparecer + flotar
        float pop = Mathf.Clamp01(_age / 0.25f);
        float s = pop < 1f ? Mathf.Sin(pop * Mathf.PI * 0.5f) * 1.15f : 1f;
        transform.localScale = Vector3.one * s;
        transform.position = _basePos + Vector3.up * Mathf.Sin(_age * 3f) * 0.08f;
        if (_body != null) _body.localRotation = Quaternion.Euler(Mathf.Sin(_age * 2f) * 6f, _age * 20f, Mathf.Cos(_age * 1.7f) * 6f);

        // la luz titila más rápido cuando está por desaparecer
        float left = _lifetime - _age;
        float blink = left < 1.5f ? 10f : 3f;
        if (_lightMat != null) SetColor(_lightMat, Mathf.Sin(_age * blink * Mathf.PI) > 0f ? LightOn : LightOff);

        if (left <= 0f) { Destroy(gameObject); return; }

        if (_age > 0.25f && Time.time >= _nextCheck)
        {
            _nextCheck = Time.time + 0.1f;
            int n = Physics.OverlapSphereNonAlloc(transform.position, _triggerRadius, _hits, _layers);
            for (int i = 0; i < n; i++)
            {
                if (_hits[i].GetComponentInParent<EnemyHealth>() != null) { Explode(); return; }
            }
        }
    }

    private void Explode()
    {
        Vector3 c = transform.position;
        var hits = Physics.OverlapSphere(c, _explosionRadius, _layers);
        bool prevSuppress = RelicRuntime.SuppressProcs;
        RelicRuntime.SuppressProcs = true; // lo que mate la mina no larga Metralla
        DamageContext.Set(false, DamageKind.Normal);
        for (int i = 0; i < hits.Length; i++)
        {
            var d = hits[i].GetComponentInParent<IDamageable>();
            if (d != null) d.TakeDamage(_damage);
        }
        DamageContext.Clear();
        RelicRuntime.SuppressProcs = prevSuppress;

        if (_vfx != null && ParticlePool.Instance != null) ParticlePool.Instance.GetParticle(_vfx, c);
        WaterImpacts.Spawn(c, 1.4f);
        Destroy(gameObject);
    }
}
