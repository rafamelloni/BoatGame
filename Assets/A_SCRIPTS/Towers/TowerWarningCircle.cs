using UnityEngine;

// Aviso en el agua de dónde va a caer la bala del mortero.
//  - Un aro fino del tamaño del daño que late (cada vez más rápido a medida que se acerca la bala)
//  - Un relleno con brillo en el borde que crece desde el centro hasta llenar el aro
//  - Al caer: el aro se expande un poco y se desvanece (flash de impacto)
// Todo plano, sin luz y transparente, con texturas generadas por código (no necesita assets).
// Si querés tu propio aviso, hacé un prefab con este script y asigná Outer / Fill (modo viejo: escala).
public class TowerWarningCircle : MonoBehaviour
{
    [Tooltip("Opcional (modo prefab propio): disco del tamaño final")]
    [SerializeField] private Transform _outer;
    [Tooltip("Opcional (modo prefab propio): disco que se va llenando")]
    [SerializeField] private Transform _fill;
    [Tooltip("Altura sobre el agua para que no parpadee con la superficie")]
    [SerializeField] private float _heightOffset = 0.12f;

    [Header("Look (modo generado)")]
    [SerializeField] private Color _ringColor = new Color(1f, 0.3f, 0.18f, 1f);
    [SerializeField] private Color _fillColor = new Color(1f, 0.42f, 0.2f, 0.55f);
    [Tooltip("Latidos por segundo al principio y justo antes de caer")]
    [SerializeField] private Vector2 _pulseSpeed = new Vector2(2.5f, 9f);
    [SerializeField] private float _impactTime = 0.28f;
    [SerializeField] private float _impactGrow = 1.35f;

    private SpriteRenderer _ring, _fillSr;
    private float _radius = 1f;
    private float _progress;
    private float _pulseT;
    private float _impact = -1f;

    private static Sprite _ringSprite, _discSprite;

    public bool IsFree => !gameObject.activeSelf;

    // Modo prefab propio
    public void Setup(Transform outer, Transform fill)
    {
        _outer = outer;
        _fill = fill;
    }

    // Modo generado: arma el aro y el relleno como sprites planos
    public void BuildDefault(Color ring, Color fill)
    {
        _ringColor = ring;
        _fillColor = fill;
        EnsureSprites();
        _fillSr = MakeLayer("Fill", _discSprite, 0);
        _ring = MakeLayer("Ring", _ringSprite, 1);
    }

    private SpriteRenderer MakeLayer(string n, Sprite s, int order)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // acostado sobre el agua
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 40 + order;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;
        return sr;
    }

    public void Show(Vector3 position, float radius)
    {
        _radius = radius;
        _impact = -1f;
        _pulseT = 0f;
        transform.position = position + Vector3.up * _heightOffset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (_outer != null) _outer.localScale = new Vector3(radius * 2f, _outer.localScale.y, radius * 2f);
        if (_ring != null) _ring.transform.localScale = Vector3.one * radius * 2f;
        SetProgress(0f);
        gameObject.SetActive(true);
    }

    public void SetProgress(float k)
    {
        _progress = Mathf.Clamp01(k);
        if (_fill != null)
        {
            float r = _radius * 2f * _progress;
            _fill.localScale = new Vector3(r, _fill.localScale.y, r);
        }
        if (_fillSr != null)
        {
            // crece rápido al principio y se frena al llegar al borde
            float e = 1f - (1f - _progress) * (1f - _progress);
            _fillSr.transform.localScale = Vector3.one * Mathf.Max(0.001f, _radius * 2f * e);
        }
    }

    // La llama la bala al caer: flash y se apaga sola
    public void PlayImpact()
    {
        if (_ring == null) { Hide(); return; }
        _impact = 0f;
    }

    public void Hide()
    {
        _impact = -1f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_ring == null) return;

        if (_impact >= 0f)
        {
            _impact += Time.deltaTime;
            float k = Mathf.Clamp01(_impact / _impactTime);
            float ease = 1f - (1f - k) * (1f - k);
            _ring.transform.localScale = Vector3.one * _radius * 2f * Mathf.Lerp(1f, _impactGrow, ease);
            _ring.color = WithAlpha(Color.Lerp(Color.white, _ringColor, k), 1f - k);
            _fillSr.color = WithAlpha(Color.Lerp(Color.white, _fillColor, k * 0.5f), (1f - k) * 0.8f);
            if (k >= 1f) Hide();
            return;
        }

        // latido: más rápido cuanto más cerca está la bala
        _pulseT += Time.deltaTime * Mathf.Lerp(_pulseSpeed.x, _pulseSpeed.y, _progress * _progress);
        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseT * Mathf.PI * 2f);
        _ring.color = WithAlpha(_ringColor, Mathf.Lerp(0.55f, 1f, pulse));
        _ring.transform.localScale = Vector3.one * _radius * 2f * (1f + 0.025f * pulse);
        _fillSr.color = WithAlpha(_fillColor, _fillColor.a * Mathf.Lerp(0.5f, 1f, _progress));
    }

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // ---------- texturas generadas ----------

    private static void EnsureSprites()
    {
        if (_ringSprite == null) _ringSprite = MakeSprite(true);
        if (_discSprite == null) _discSprite = MakeSprite(false);
    }

    // ring = aro fino con un poco de glow hacia adentro; disc = relleno suave con borde más marcado
    private static Sprite MakeSprite(bool ring)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = ring ? "TowerWarnRing" : "TowerWarnDisc"
        };
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c; // 0 centro, 1 borde
                float a;
                if (ring)
                {
                    float line = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.94f) / 0.05f);     // aro
                    float glow = Mathf.Clamp01((d - 0.75f) / 0.2f) * 0.35f;            // brillo hacia adentro
                    a = Mathf.Max(Mathf.SmoothStep(0f, 1f, line), d < 0.94f ? glow : 0f);
                    a *= Mathf.Clamp01((1f - d) * c * 0.5f + 0.5f);                    // antialias afuera
                }
                else
                {
                    float body = 0.35f + 0.65f * Mathf.SmoothStep(0.55f, 1f, d);       // más fuerte en el borde
                    a = body * Mathf.Clamp01((1f - d) * c * 0.5f);                     // borde suave
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
