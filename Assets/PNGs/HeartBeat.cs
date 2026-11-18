using UnityEngine;
using UnityEngine.UI;

public class HeartBeat : MonoBehaviour
{
    [Header("Timing")]
    public float beatDuration = 1f;

    [Header("Scale")]
    public float baseScale = 1f;
    public float firstPeak = 1.18f;
    public float secondPeak = 1.1f;

    [Header("Beat Shape")]
    [Range(0f, 1f)] public float firstPeakTime = 0.15f;
    [Range(0f, 1f)] public float returnTime = 0.3f;
    [Range(0f, 1f)] public float secondPeakTime = 0.45f;

    [Header("Vida (si queda vacío lo busca solo)")]
    [SerializeField] private RT_PlayerHealth _health;

    [Header("Golpe")]
    [SerializeField] private float _hitPunch = 0.35f;       // cuánto crece de golpe (relativo a baseScale)
    [SerializeField] private float _hitSquash = 0.25f;      // aplastar/estirar mientras rebota
    [SerializeField] private float _hitShake = 10f;         // temblor en unidades de UI
    [SerializeField] private float _hitTilt = 18f;          // grados que se ladea
    [SerializeField] private float _hitDuration = 0.4f;
    [SerializeField] private Color _hitColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Vida baja")]
    [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;
    [SerializeField] private float _lowHealthBeatDuration = 0.45f;  // late más rápido
    [SerializeField] private float _lowHealthExtraPeak = 0.08f;     // y más fuerte
    [SerializeField, Range(0f, 1f)] private float _lowHealthTint = 0.4f;

    private float timer;            // en ciclos de latido
    private float _hitTimer;
    private float _shakeSeed;
    private bool _subscribed;
    private Image _image;
    private Color _baseColor = Color.white;
    private RectTransform _rect;

    // Lo que el golpe le sumó en el frame anterior, para sacarlo antes de sumar lo nuevo.
    // Así no se pisa la posición/rotación que tenga el corazón.
    private Vector2 _appliedOffset;
    private float _appliedTilt;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null) _baseColor = _image.color;
        _rect = transform as RectTransform;
    }

    void OnEnable()
    {
        TryBind();
    }

    void OnDisable()
    {
        if (_subscribed && _health != null) _health.OnDamage -= OnHit;
        _subscribed = false;
        ApplyHitOffset(Vector2.zero, 0f); // si se apaga en medio de un golpe, vuelve a su lugar
        _hitTimer = 0f;
    }

    void TryBind()
    {
        if (_subscribed) return;
        if (_health == null)
            _health = FindFirstObjectByType<RT_PlayerHealth>(FindObjectsInactive.Include);
        if (_health == null) return;
        _health.OnDamage += OnHit;
        _subscribed = true;
    }

    void OnHit()
    {
        _hitTimer = _hitDuration;
        _shakeSeed = Random.value * 100f;
    }

    void Update()
    {
        if (!_subscribed) TryBind();
        float dt = Time.deltaTime;

        // Con poca vida late más rápido y más fuerte
        float ratio = (_health != null && _health.MaxHealth > 0f) ? _health.CurrentHealth / _health.MaxHealth : 1f;
        float low = _lowHealthThreshold > 0f ? Mathf.Clamp01(1f - ratio / _lowHealthThreshold) : 0f;
        float duration = Mathf.Lerp(beatDuration, _lowHealthBeatDuration, low);

        timer += dt / Mathf.Max(duration, 0.01f);
        float t = timer % 1f;
        float extra = low * _lowHealthExtraPeak;
        float scale = EvaluateBeat(t, extra);

        Vector3 finalScale = Vector3.one * scale;
        Vector2 offset = Vector2.zero;
        float tilt = 0f;
        Color color = _baseColor;

        if (low > 0f)
        {
            float beatAmount = Mathf.InverseLerp(baseScale, firstPeak + extra, scale);
            color = Color.Lerp(_baseColor, _hitColor, low * _lowHealthTint * beatAmount);
        }

        if (_hitTimer > 0f)
        {
            _hitTimer -= dt;
            float k = Mathf.Clamp01(_hitTimer / _hitDuration);        // 1 → 0
            float e = k * k;                                         // cae rápido
            float wobble = Mathf.Sin((1f - k) * Mathf.PI * 3f) * k;  // rebote que se apaga

            finalScale = new Vector3(
                scale + baseScale * (_hitPunch * e + _hitSquash * wobble),
                scale + baseScale * (_hitPunch * e - _hitSquash * wobble),
                1f);

            float n = Time.time * 60f;
            offset = new Vector2(Mathf.PerlinNoise(_shakeSeed, n) - 0.5f,
                                 Mathf.PerlinNoise(n, _shakeSeed) - 0.5f) * 2f * _hitShake * e;
            tilt = _hitTilt * wobble;
            color = Color.Lerp(color, _hitColor, e);
        }

        transform.localScale = finalScale;
        ApplyHitOffset(offset, tilt);
        if (_image != null) _image.color = color;
    }

    // Suma el temblor/ladeo como diferencia respecto del frame anterior
    void ApplyHitOffset(Vector2 offset, float tilt)
    {
        if (_rect != null && offset != _appliedOffset)
        {
            _rect.anchoredPosition += offset - _appliedOffset;
            _appliedOffset = offset;
        }
        if (!Mathf.Approximately(tilt, _appliedTilt))
        {
            transform.localRotation *= Quaternion.Euler(0f, 0f, tilt - _appliedTilt);
            _appliedTilt = tilt;
        }
    }

    float EvaluateBeat(float t, float extra)
    {
        float p1 = firstPeak + extra;
        float p2 = secondPeak + extra;

        if (t < firstPeakTime)
            return Mathf.Lerp(baseScale, p1, EaseInOut(t / firstPeakTime));

        if (t < returnTime)
            return Mathf.Lerp(p1, baseScale, EaseInOut((t - firstPeakTime) / (returnTime - firstPeakTime)));

        if (t < secondPeakTime)
            return Mathf.Lerp(baseScale, p2, EaseInOut((t - returnTime) / (secondPeakTime - returnTime)));

        return Mathf.Lerp(p2, baseScale, EaseInOut((t - secondPeakTime) / (1f - secondPeakTime)));
    }

    float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}