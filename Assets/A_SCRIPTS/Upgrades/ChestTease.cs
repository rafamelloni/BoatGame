using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// El cofre "se quiere abrir": brilla por dentro (cerradura + rendija), resplandor y rayos atrás,
// y cada tanto se agacha, salta sacudiéndose, se le escapa la luz y tira chispitas.
// Va en un objeto PADRE del cofre (así no choca con el pop que le hace UpgradePathMap al cofre).
public class ChestTease : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("El Image del cofre (se usa para medir el tamaño)")]
    [SerializeField] private RectTransform chest;
    [Tooltip("Cofre_KeyholeLight, hijo del cofre estirado")]
    [SerializeField] private Graphic keyholeLight;
    [Tooltip("Cofre_SeamLight, hijo del cofre estirado")]
    [SerializeField] private Graphic seamLight;
    [Tooltip("Chest_Glow, detrás del cofre")]
    [SerializeField] private RectTransform glow;
    [Tooltip("Chest_Rays, detrás del cofre")]
    [SerializeField] private RectTransform rays;
    [SerializeField] private Sprite sparkleSprite;

    [Header("Idle")]
    [Tooltip("Brillo de adentro en reposo (mínimo y máximo del pulso)")]
    [SerializeField] private Vector2 lightIdle = new Vector2(0.35f, 0.7f);
    [SerializeField] private float lightPulseSpeed = 2.2f;
    [SerializeField] private float glowPulse = 0.08f;
    [Tooltip("Grados por segundo")]
    [SerializeField] private float raysSpeed = 15f;
    [SerializeField] private Vector2 raysAlpha = new Vector2(0.45f, 0.8f);

    [Header("Intento de abrirse")]
    [Tooltip("Cada cuántos segundos (mínimo y máximo)")]
    [SerializeField] private Vector2 interval = new Vector2(2.5f, 4.5f);
    [SerializeField] private float squashTime = 0.12f;
    [SerializeField] private float hopTime = 0.38f;
    [Tooltip("Altura del salto como fracción del alto del cofre")]
    [SerializeField] private float hopHeight = 0.15f;
    [SerializeField] private float shakeAngle = 7f;
    [SerializeField] private int shakes = 3;
    [SerializeField] private float settleTime = 0.22f;
    [SerializeField] private int sparkles = 5;
    [Tooltip("Tamaño de las chispas como fracción del alto del cofre")]
    [SerializeField] private float sparkleSize = 0.18f;
    [SerializeField] private bool unscaledTime = true;

    private float _timer;
    private float _phaseTime = -1f;     // tiempo dentro del salto (-1 = en reposo)
    private float _flare;               // 0..1, luz extra durante el salto
    private Vector2 _appliedOffset;
    private float _appliedAngle;
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _glowBaseScale = Vector3.one;   // la escala que le pusiste al glow en la escena
    private RectTransform _rt;

    private class Spark { public RectTransform rt; public Image img; public Vector2 from, to; public float age, life, spin; }
    private readonly List<Spark> _sparks = new List<Spark>();

    private float Dt => unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private float Now => unscaledTime ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        if (glow != null) _glowBaseScale = glow.localScale;
        _timer = Random.Range(interval.x, interval.y);
    }

    private void OnDisable()
    {
        Apply(Vector2.zero, 0f);
        _rt.localScale = _baseScale;
        foreach (var s in _sparks) if (s.rt != null) Destroy(s.rt.gameObject);
        _sparks.Clear();
        _phaseTime = -1f;
    }

    private float ChestHeight()
    {
        if (chest == null) return 100f;
        // alto del cofre medido en el espacio de este objeto
        Vector3 a = _rt.InverseTransformPoint(chest.TransformPoint(new Vector3(0f, chest.rect.yMin, 0f)));
        Vector3 b = _rt.InverseTransformPoint(chest.TransformPoint(new Vector3(0f, chest.rect.yMax, 0f)));
        return Mathf.Abs(b.y - a.y);
    }

    private void Update()
    {
        float dt = Dt, now = Now;

        // ---- idle: brillo que respira, resplandor y rayos
        float pulse = (Mathf.Sin(now * lightPulseSpeed) + 1f) * 0.5f;
        float light = Mathf.Max(Mathf.Lerp(lightIdle.x, lightIdle.y, pulse), _flare);
        SetAlpha(keyholeLight, light);
        SetAlpha(seamLight, Mathf.Max(light * 0.8f, _flare));
        if (glow != null) glow.localScale = _glowBaseScale * (1f + Mathf.Sin(now * lightPulseSpeed * 0.7f) * glowPulse + _flare * 0.15f);
        if (rays != null)
        {
            rays.localRotation = Quaternion.Euler(0f, 0f, -now * raysSpeed);
            SetAlpha(rays.GetComponent<Graphic>(), Mathf.Lerp(raysAlpha.x, raysAlpha.y, pulse) + _flare * 0.3f);
        }
        _flare = Mathf.MoveTowards(_flare, 0f, dt * 1.8f);

        // ---- intento de abrirse
        Vector2 offset = Vector2.zero; float angle = 0f; Vector3 squash = Vector3.one;
        if (_phaseTime < 0f)
        {
            _timer -= dt;
            if (_timer <= 0f) { _phaseTime = 0f; _timer = Random.Range(interval.x, interval.y); }
        }
        else
        {
            _phaseTime += dt;
            float h = ChestHeight();
            float t = _phaseTime;
            if (t < squashTime)                                   // se agacha
            {
                float k = Mathf.Sin(t / squashTime * Mathf.PI * 0.5f);
                squash = new Vector3(1f + 0.08f * k, 1f - 0.1f * k, 1f);
            }
            else if (t < squashTime + hopTime)                    // salta sacudiéndose y se le escapa la luz
            {
                float k = (t - squashTime) / hopTime;
                if (_flare < 1f && k < 0.1f) { _flare = 1f; SpawnSparks(h); }
                offset = new Vector2(0f, Mathf.Sin(k * Mathf.PI) * hopHeight * h);
                angle = Mathf.Sin(k * Mathf.PI * 2f * shakes) * shakeAngle * (1f - k);
                squash = new Vector3(1f - 0.05f * Mathf.Sin(k * Mathf.PI), 1f + 0.08f * Mathf.Sin(k * Mathf.PI), 1f);
            }
            else if (t < squashTime + hopTime + settleTime)       // aterriza
            {
                float k = (t - squashTime - hopTime) / settleTime;
                float s = Mathf.Sin(k * Mathf.PI) * (1f - k);
                squash = new Vector3(1f + 0.1f * s, 1f - 0.12f * s, 1f);
            }
            else _phaseTime = -1f;
        }
        Apply(offset, angle);
        _rt.localScale = Vector3.Scale(_baseScale, squash);

        // ---- chispitas
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var s = _sparks[i];
            s.age += dt;
            float k = s.age / s.life;
            if (k >= 1f) { Destroy(s.rt.gameObject); _sparks.RemoveAt(i); continue; }
            s.rt.anchoredPosition = Vector2.Lerp(s.from, s.to, 1f - (1f - k) * (1f - k));
            s.rt.localScale = Vector3.one * Mathf.Sin(k * Mathf.PI);
            s.rt.localRotation = Quaternion.Euler(0f, 0f, s.spin * k);
        }
    }

    private void SpawnSparks(float h)
    {
        if (sparkleSprite == null) return;
        for (int i = 0; i < sparkles; i++)
        {
            var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rt, false);
            var img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.raycastTarget = false;
            float size = sparkleSize * h * Random.Range(0.6f, 1.2f);
            rt.sizeDelta = new Vector2(size, size);
            float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;      // hacia arriba y a los costados
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 from = dir * h * 0.25f;
            _sparks.Add(new Spark
            {
                rt = rt, img = img, from = from, to = from + dir * h * Random.Range(0.45f, 0.8f),
                life = Random.Range(0.45f, 0.75f), spin = Random.Range(-180f, 180f)
            });
            rt.anchoredPosition = from;
            rt.localScale = Vector3.zero;
        }
    }

    // Suma el salto/sacudón como diferencia, para no pisar la posición que le pongas
    private void Apply(Vector2 offset, float angle)
    {
        if (offset != _appliedOffset) { _rt.anchoredPosition += offset - _appliedOffset; _appliedOffset = offset; }
        if (!Mathf.Approximately(angle, _appliedAngle))
        {
            _rt.localRotation *= Quaternion.Euler(0f, 0f, angle - _appliedAngle);
            _appliedAngle = angle;
        }
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color; c.a = Mathf.Clamp01(a); g.color = c;
    }
}
