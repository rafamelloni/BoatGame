using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Olas animadas + espuma que respira para el mapa del pergamino.
// Usa tiempo sin escalar porque el pergamino pausa el juego.
public class MapOceanFX : MonoBehaviour
{
    [Header("Mapa")]
    [Tooltip("El Image del mapa (UpgradeMap_Base). Las olas se ubican relativas a su rect")]
    [SerializeField] private RectTransform mapArea;
    [Tooltip("Contenedor vacío de las olas, hijo del mapa. En la jerarquía ponelo ANTES de las islas")]
    [SerializeField] private RectTransform waveLayer;

    [Header("Olas")]
    [SerializeField] private Sprite[] waveSprites;
    [SerializeField] private int waveCount = 12;
    [Tooltip("Ancho de cada ola como fracción del ancho del mapa")]
    [SerializeField] private float waveWidth = 0.11f;
    [SerializeField] private Color waveColor = Color.white;
    [SerializeField] private Vector2 lifeTime = new Vector2(2.5f, 4.5f);
    [SerializeField] private float fadeTime = 0.6f;
    [Tooltip("Cuánto se corre de costado durante su vida (fracción del ancho del mapa)")]
    [SerializeField] private float drift = 0.03f;
    [Tooltip("Sube y baja (fracción del alto del mapa)")]
    [SerializeField] private float bobAmount = 0.004f;
    [SerializeField] private float bobSpeed = 2.5f;
    [Tooltip("Cuánto se estira la ola a mitad de su vida, como si rompiera")]
    [SerializeField] private float stretch = 0.15f;
    [Tooltip("Lugares posibles (0..1 sobre el mapa). Vienen calculados para caer en el mar, lejos de las islas y la brújula")]
    [SerializeField] private Vector2[] spawnPoints =
    {
        new Vector2(0.535f, 0.509f), new Vector2(0.705f, 0.794f), new Vector2(0.531f, 0.438f), new Vector2(0.804f, 0.698f),
        new Vector2(0.36f, 0.573f), new Vector2(0.398f, 0.506f), new Vector2(0.382f, 0.66f), new Vector2(0.885f, 0.318f),
        new Vector2(0.501f, 0.671f), new Vector2(0.395f, 0.364f), new Vector2(0.712f, 0.426f), new Vector2(0.396f, 0.42f),
        new Vector2(0.616f, 0.505f), new Vector2(0.786f, 0.431f), new Vector2(0.456f, 0.457f), new Vector2(0.585f, 0.662f),
        new Vector2(0.718f, 0.735f), new Vector2(0.584f, 0.578f), new Vector2(0.294f, 0.34f), new Vector2(0.16f, 0.424f),
        new Vector2(0.645f, 0.441f), new Vector2(0.164f, 0.368f), new Vector2(0.457f, 0.342f), new Vector2(0.838f, 0.357f),
        new Vector2(0.666f, 0.693f), new Vector2(0.165f, 0.515f), new Vector2(0.414f, 0.262f), new Vector2(0.231f, 0.367f),
        new Vector2(0.629f, 0.819f), new Vector2(0.807f, 0.295f), new Vector2(0.462f, 0.398f), new Vector2(0.7f, 0.488f),
        new Vector2(0.44f, 0.619f), new Vector2(0.612f, 0.746f), new Vector2(0.49f, 0.882f), new Vector2(0.867f, 0.73f),
        new Vector2(0.771f, 0.355f), new Vector2(0.362f, 0.315f), new Vector2(0.518f, 0.601f), new Vector2(0.291f, 0.561f)
    };
    [Tooltip("Distancia mínima entre olas (fracción del mapa)")]
    [SerializeField] private float minSeparation = 0.1f;

    [Header("Espuma de las islas")]
    [SerializeField] private RectTransform[] foams;
    [SerializeField] private float foamPulse = 0.035f;
    [SerializeField] private float foamSpeed = 1.6f;
    [SerializeField] private Vector2 foamAlpha = new Vector2(0.75f, 1f);

    private class Wave
    {
        public RectTransform rt;
        public Image img;
        public Vector2 start;
        public float age, life, dir, phase;
        public int spawn;
    }

    private readonly List<Wave> _waves = new List<Wave>();
    private readonly List<Vector3> _foamBaseScale = new List<Vector3>();
    private readonly List<Graphic> _foamGraphics = new List<Graphic>();

    private void Awake()
    {
        foreach (var f in foams)
        {
            _foamBaseScale.Add(f != null ? f.localScale : Vector3.one);
            _foamGraphics.Add(f != null ? f.GetComponent<Graphic>() : null);
        }
    }

    private void OnEnable()
    {
        if (_waves.Count == 0) BuildWaves();
    }

    private void BuildWaves()
    {
        if (mapArea == null || waveLayer == null || waveSprites == null || waveSprites.Length == 0 || spawnPoints.Length == 0) return;
        for (int i = 0; i < waveCount; i++)
        {
            var go = new GameObject("Wave", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(waveLayer, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var w = new Wave { rt = rt, img = img };
            Respawn(w);
            w.age = Random.Range(0f, w.life);  // arrancan desfasadas
            _waves.Add(w);
        }
    }

    private void Respawn(Wave w)
    {
        // un lugar libre y lejos de las otras olas
        int pick = -1;
        for (int tries = 0; tries < 30 && pick < 0; tries++)
        {
            int i = Random.Range(0, spawnPoints.Length);
            bool ok = true;
            foreach (var o in _waves)
                if (o != w && (o.spawn == i || Vector2.Distance(o.start, spawnPoints[i]) < minSeparation)) { ok = false; break; }
            if (ok) pick = i;
        }
        if (pick < 0) pick = Random.Range(0, spawnPoints.Length);

        w.spawn = pick;
        w.start = spawnPoints[pick];
        w.age = 0f;
        w.life = Random.Range(lifeTime.x, lifeTime.y);
        w.dir = Random.value < 0.5f ? -1f : 1f;
        w.phase = Random.value * 10f;
        w.img.sprite = waveSprites[Random.Range(0, waveSprites.Length)];
        w.img.SetNativeSize();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        if (mapArea != null && waveLayer != null)
        {
            Rect r = mapArea.rect;
            float unit = Mathf.Abs(waveLayer.lossyScale.x) > 1e-6f ? mapArea.lossyScale.x / waveLayer.lossyScale.x : 1f;

            foreach (var w in _waves)
            {
                w.age += dt;
                if (w.age >= w.life) Respawn(w);
                float t = w.age / w.life;

                float a = Mathf.Clamp01(w.age / fadeTime) * Mathf.Clamp01((w.life - w.age) / fadeTime);
                Color c = waveColor; c.a *= a;
                w.img.color = c;

                Vector2 n = w.start + new Vector2(w.dir * drift * t, Mathf.Sin(w.phase + time * bobSpeed) * bobAmount);
                Vector3 local = new Vector3(r.xMin + n.x * r.width, r.yMin + n.y * r.height, 0f);
                w.rt.position = mapArea.TransformPoint(local);

                // tamaño en proporción al mapa (respeta el aspecto del PNG)
                Vector2 native = w.img.sprite != null ? w.img.sprite.rect.size : new Vector2(170f, 60f);
                float width = waveWidth * r.width * unit;
                w.rt.sizeDelta = new Vector2(width, width * native.y / native.x);

                float s = Mathf.Sin(t * Mathf.PI);
                w.rt.localScale = new Vector3((1f + s * stretch) * w.dir, 1f - s * stretch * 0.4f, 1f);
            }
        }

        for (int i = 0; i < foams.Length; i++)
        {
            if (foams[i] == null) continue;
            float k = Mathf.Sin(time * foamSpeed + i * 1.7f);
            foams[i].localScale = _foamBaseScale[i] * (1f + k * foamPulse);
            var g = _foamGraphics[i];
            if (g != null)
            {
                Color c = g.color;
                c.a = Mathf.Lerp(foamAlpha.x, foamAlpha.y, (k + 1f) * 0.5f);
                g.color = c;
            }
        }
    }
}
