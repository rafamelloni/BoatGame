using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePathMap : MonoBehaviour
{
    [Header("Nodos (en orden I, II, III, IV)")]
    [SerializeField] private TextMeshProUGUI[] nodeTexts;
    [SerializeField] private Image[] nodeCircles;

    [Header("Colores")]
    [SerializeField] private Color colorNotTaken;
    [SerializeField] private Color colorNext;
    [SerializeField] private Color colorCompleted;

    [Header("Referencias")]
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;

    [Header("Carteles (siempre visibles)")]
    [Tooltip("Texto que dice de qué camino es el mapa")]
    [SerializeField] private TextMeshProUGUI pathLabel;
    [SerializeField] private string pathLabelFormat = "{0} Path";
    [Tooltip("Texto debajo del cofre con lo que da el nivel IV")]
    [SerializeField] private TextMeshProUGUI rewardLabel;

    [Header("Tooltip Cofre")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDescription;

    [Header("Islas (en orden I, II, III, Cofre)")]
    [SerializeField] private Image[] islands;
    [SerializeField] private Sprite[] islandColorSprites;
    [SerializeField] private Sprite[] islandGraySprites;
    [SerializeField] private float islandPopScale = 1.25f;
    [SerializeField] private float islandPopTime = 0.35f;

    [Header("Ruta")]
    [Tooltip("Punto de donde sale el barquito antes del tier I")]
    [SerializeField] private RectTransform routeStart;
    [Tooltip("Contenedor vacío para los puntos de la línea. En la jerarquía ponelo ANTES de las islas para que quede debajo")]
    [SerializeField] private RectTransform routeLayer;
    [SerializeField] private Sprite dashSprite;
    [SerializeField] private Color dashColor = new Color32(128, 66, 24, 255);
    [SerializeField] private Vector2 dashSize = new Vector2(24f, 9f);
    [SerializeField] private float dashGap = 18f;
    [Tooltip("Cuánto se curva la línea (0 = recta)")]
    [SerializeField] private float curveBend = 0.2f;
    [Tooltip("Espacio que deja la línea al salir y al llegar a cada parada")]
    [SerializeField] private float stopPadding = 45f;

    [Header("Barquito")]
    [SerializeField] private RectTransform boat;
    [Tooltip("Dónde se estaciona el barquito respecto del centro de cada isla, en unidades de pantalla (para no tapar el número)")]
    [SerializeField] private Vector2 dockOffset = new Vector2(-70f, -55f);
    [SerializeField] private float sailDuration = 1.2f;
    [SerializeField] private float boatBobAmount = 4f;
    [SerializeField] private float boatTiltAmount = 6f;
    [SerializeField] private float boatBobSpeed = 5f;
    [Tooltip("Tildalo si el PNG del barco mira a la derecha")]
    [SerializeField] private bool boatFacesRight = true;

    [Header("Secuencia al mejorar")]
    [Tooltip("Apagado: la isla se pinta al confirmar. Prendido: se pinta cuando llega el barquito")]
    [SerializeField] private bool lightIslandOnArrival = false;
    [SerializeField] private float holdAfterArrival = 0.4f;

    private SO_UpgradePath _currentPath;

    private class Segment
    {
        public readonly List<Vector2> points = new List<Vector2>();
        public readonly List<float> cumulative = new List<float>();
        public float length;
        public readonly List<Image> dashes = new List<Image>();
        public readonly List<float> dashAt = new List<float>();
    }

    private readonly List<Segment> _segments = new List<Segment>();
    private bool _animating;
    private Vector2 _boatLocal;         // posición del barco en el espacio del RouteLayer (se mueve con el panel)
    private float _boatFacing = 1f;
    private readonly List<RectTransform> _poppingDashes = new List<RectTransform>();
    private RectTransform _canvasRT;
    private Quaternion _boatBaseRot = Quaternion.identity;   // la rotación con la que lo pusiste en la escena
    private Vector3 _boatBaseScale = Vector3.one;            // y su escala

    private Sprite[] _originalIslandSprites;   // el sprite que cada isla tiene puesto en la escena

    private void Awake()
    {
        if (islands != null)
        {
            _originalIslandSprites = new Sprite[islands.Length];
            for (int i = 0; i < islands.Length; i++)
                if (islands[i] != null) _originalIslandSprites[i] = islands[i].sprite;
        }
        if (boat != null)
        {
            _boatBaseRot = boat.localRotation;
            _boatBaseScale = boat.localScale;
        }
    }

    private int StopCount => (islands != null ? islands.Length : 0) + 1;

    // Las medidas del inspector están en unidades del Canvas; esto las pasa a unidades del RouteLayer
    private RectTransform CanvasRT
    {
        get
        {
            if (_canvasRT == null && routeLayer != null)
            {
                var canvas = routeLayer.GetComponentInParent<Canvas>();
                if (canvas != null) _canvasRT = canvas.rootCanvas.transform as RectTransform;
            }
            return _canvasRT;
        }
    }

    private float K
    {
        get
        {
            if (routeLayer == null || CanvasRT == null) return 1f;
            float layer = routeLayer.lossyScale.x;
            return Mathf.Abs(layer) > 1e-6f ? CanvasRT.lossyScale.x / layer : 1f;
        }
    }

    // ---------------------------------------------------------------- mapa

    public void UpdateMap(SO_UpgradePath path)
    {
        _currentPath = path;
        int currentLevel = playerUpgrades.GetLevel(path);

        for (int i = 0; i < nodeTexts.Length; i++)
        {
            if (i < currentLevel)
            {
                nodeTexts[i].color = colorCompleted;
                nodeCircles[i].enabled = true;
            }
            else if (i == currentLevel)
            {
                nodeTexts[i].color = colorNext;
                nodeCircles[i].enabled = true;
            }
            else
            {
                nodeTexts[i].color = colorNotTaken;
                nodeCircles[i].enabled = false;
            }
        }

        if (pathLabel != null)
            pathLabel.text = string.Format(pathLabelFormat, path.pathName);

        if (rewardLabel != null)
        {
            SO_UpgradeStep tier4 = path.GetStep(3);
            rewardLabel.text = tier4 != null ? tier4.displayName : "";
        }

        if (!_animating) SetRouteState(currentLevel);
    }

    public void ShowTooltip()
    {
        if (_currentPath == null) return;

        SO_UpgradeStep tier4 = _currentPath.GetStep(3);
        if (tier4 == null) return;

        tooltipTitle.text = tier4.displayName;
        tooltipDescription.text = tier4.description;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // Si la animación se cortó a la mitad (se cerró el panel, etc.), destraba el mapa
    public void CancelAnimation()
    {
        _animating = false;
    }

    public void ResetMap()
    {
        foreach (var node in nodeTexts)
            node.color = colorNotTaken;

        foreach (var circle in nodeCircles)
            circle.enabled = false;

        if (pathLabel != null) pathLabel.text = "";
        if (rewardLabel != null) rewardLabel.text = "";

        _currentPath = null;
        if (!_animating) SetRouteState(0);
    }

    // ---------------------------------------------------------------- islas, línea y barquito

    // Islas hechas a color, el resto en gris; la línea dibujada hasta la última isla hecha; el barco ahí
    private void SetRouteState(int level)
    {
        if (islands == null) return;
        Rebuild();

        for (int i = 0; i < islands.Length; i++)
            SetIsland(i, i < level);

        for (int k = 0; k < _segments.Count; k++)
            foreach (var dash in _segments[k].dashes)
            {
                dash.gameObject.SetActive(k < level);
                dash.rectTransform.localScale = Vector3.one;
            }
        _poppingDashes.Clear();

        if (routeLayer != null)
            _boatLocal = RoutePoint(Mathf.Clamp(level, 0, StopCount - 1));
    }

    private bool _warned;

    private void SetIsland(int index, bool colored)
    {
        if (index < 0 || index >= islands.Length || islands[index] == null) return;
        Sprite s = colored ? Get(islandColorSprites, index) : Get(islandGraySprites, index);
        // Si esa lista está vacía, vuelve al sprite que la isla tiene en la escena
        if (s == null) s = Get(_originalIslandSprites, index);
        if (s != null) islands[index].sprite = s;
        else if (!_warned)
        {
            _warned = true;
            Debug.LogWarning($"[UpgradePathMap] Falta el sprite {(colored ? "a color" : "gris")} de la isla {index + 1}. " +
                             "Revisá que Island Color Sprites e Island Gray Sprites tengan 4 elementos, en el mismo orden que Islands.", this);
        }
    }

    private static Sprite Get(Sprite[] arr, int i) => arr != null && i < arr.Length ? arr[i] : null;

    // Llamalo DESPUÉS de aplicar la mejora: anima del nivel fromLevel al siguiente
    public IEnumerator PlayAdvance(SO_UpgradePath path, int fromLevel)
    {
        int toLevel = fromLevel + 1;
        if (islands == null || routeLayer == null || toLevel > islands.Length)
        {
            if (path != null) UpdateMap(path);
            yield break;
        }

        SetRouteState(fromLevel);
        _animating = true;

        if (!lightIslandOnArrival)
            yield return PopIsland(toLevel - 1);

        // Navegar: el barquito va por la curva y los puntos aparecen detrás
        Segment seg = _segments[fromLevel];
        foreach (var dash in seg.dashes) dash.gameObject.SetActive(false);
        int nextDash = 0;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, sailDuration);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float s = e * seg.length;

            _boatLocal = Evaluate(seg, s, out Vector2 dir);
            float dx = BoatParentDirX(dir);
            if (Mathf.Abs(dx) > 0.05f) _boatFacing = Mathf.Sign(dx) * (boatFacesRight ? 1f : -1f);

            while (nextDash < seg.dashes.Count && seg.dashAt[nextDash] <= s)
            {
                var d = seg.dashes[nextDash];
                d.gameObject.SetActive(true);
                d.rectTransform.localScale = Vector3.one * 0.2f;
                _poppingDashes.Add(d.rectTransform);
                nextDash++;
            }
            yield return null;
        }

        if (lightIslandOnArrival)
            yield return PopIsland(toLevel - 1);

        float hold = 0f;
        while (hold < holdAfterArrival) { hold += Time.unscaledDeltaTime; yield return null; }

        _animating = false;
        if (path != null) UpdateMap(path);
    }

    private IEnumerator PopIsland(int index)
    {
        if (index < 0 || index >= islands.Length || islands[index] == null) yield break;
        RectTransform rt = islands[index].rectTransform;
        Vector3 baseScale = rt.localScale;
        SetIsland(index, true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, islandPopTime);
            float k = Mathf.Clamp01(t);
            float bump = Mathf.Sin(k * Mathf.PI) * (1f - k * 0.3f);   // sube y vuelve
            rt.localScale = baseScale * (1f + (islandPopScale - 1f) * bump);
            yield return null;
        }
        rt.localScale = baseScale;
    }

    private void Update()
    {
        if (boat != null && routeLayer != null && CanvasRT != null)
        {
            float tt = Time.unscaledTime * boatBobSpeed;
            // Se recalcula cada frame desde el RouteLayer: si el panel se mueve o se balancea, el barco lo sigue
            Vector3 world = routeLayer.TransformPoint(_boatLocal);
            world += CanvasRT.TransformVector(Vector3.up * Mathf.Sin(tt) * boatBobAmount);
            boat.position = world;
            // Respeta la rotación/escala que tenía en la escena: suma el mecido y se espeja en X según el rumbo
            boat.localRotation = _boatBaseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(tt * 0.8f) * boatTiltAmount);
            boat.localScale = new Vector3(Mathf.Abs(_boatBaseScale.x) * _boatFacing, _boatBaseScale.y, _boatBaseScale.z);
        }

        for (int i = _poppingDashes.Count - 1; i >= 0; i--)
        {
            var rt = _poppingDashes[i];
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            if (Mathf.Abs(rt.localScale.x - 1f) < 0.01f) { rt.localScale = Vector3.one; _poppingDashes.RemoveAt(i); }
        }
    }

    // ---------------------------------------------------------------- armado de la línea

    // Parada 0 = salida; parada i = isla i-1 (corrida con dockOffset para no tapar el número)
    private Vector2 RoutePoint(int stop)
    {
        RectTransform rt = stop == 0 ? routeStart : (islands[stop - 1] != null ? islands[stop - 1].rectTransform : null);
        if (rt == null) return Vector2.zero;
        Vector2 p = routeLayer.InverseTransformPoint(rt.position);
        return stop == 0 ? p : p + FromCanvasDir(dockOffset);
    }

    // Vector en unidades/ejes del Canvas -> unidades/ejes del RouteLayer (y al revés)
    private Vector2 FromCanvasDir(Vector2 v) =>
        CanvasRT != null ? (Vector2)routeLayer.InverseTransformVector(CanvasRT.TransformVector(v)) : v;
    // Componente X del rumbo visto desde el padre del barco
    private float BoatParentDirX(Vector2 dirInLayer)
    {
        Vector3 world = routeLayer.TransformVector(dirInLayer);
        Transform p = boat != null ? boat.parent : null;
        Vector3 local = p != null ? p.InverseTransformVector(world) : world;
        return local.normalized.x;
    }

    private Vector2 ToCanvasDir(Vector2 v) =>
        CanvasRT != null ? (Vector2)CanvasRT.InverseTransformVector(routeLayer.TransformVector(v)) : v;

    // Arma la línea desde cero con la posición actual de las islas
    private void Rebuild()
    {
        if (routeLayer == null || islands == null || islands.Length == 0) return;
        foreach (var seg in _segments)
            foreach (var dash in seg.dashes)
                if (dash != null) Destroy(dash.gameObject);
        _segments.Clear();
        _poppingDashes.Clear();
        float unit = K;

        for (int k = 0; k < StopCount - 1; k++)
        {
            var seg = new Segment();
            Vector2 a = RoutePoint(k), b = RoutePoint(k + 1);
            Vector2 mid = (a + b) * 0.5f;
            Vector2 perp = new Vector2(-(b - a).y, (b - a).x);
            Vector2 ctrl = mid + perp * curveBend * (k % 2 == 0 ? 1f : -1f);

            const int samples = 48;
            Vector2 prev = a;
            for (int i = 0; i <= samples; i++)
            {
                float u = i / (float)samples;
                Vector2 p = (1 - u) * (1 - u) * a + 2 * (1 - u) * u * ctrl + u * u * b;
                seg.length += i == 0 ? 0f : Vector2.Distance(prev, p);
                seg.points.Add(p);
                seg.cumulative.Add(seg.length);
                prev = p;
            }

            float dashLen = dashSize.x * unit, pad = stopPadding * unit;
            float step = (dashSize.x + dashGap) * unit;
            for (float s = pad; s + dashLen <= seg.length - pad; s += step)
            {
                float center = s + dashLen * 0.5f;
                Vector2 pos = Evaluate(seg, center, out Vector2 dir);
                var go = new GameObject("RouteDash", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(routeLayer, false);
                rt.sizeDelta = dashSize * unit;
                rt.localPosition = pos;
                Vector2 cd = ToCanvasDir(dir);
                rt.rotation = CanvasRT != null
                    ? CanvasRT.rotation * Quaternion.Euler(0f, 0f, Mathf.Atan2(cd.y, cd.x) * Mathf.Rad2Deg)
                    : Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                var img = go.GetComponent<Image>();
                img.sprite = dashSprite;
                img.color = dashColor;
                img.raycastTarget = false;
                go.SetActive(false);
                seg.dashes.Add(img);
                seg.dashAt.Add(center);
            }
            _segments.Add(seg);
        }
    }

    private static Vector2 Evaluate(Segment seg, float s, out Vector2 dir)
    {
        var c = seg.cumulative;
        if (s <= 0f) { dir = (seg.points[1] - seg.points[0]).normalized; return seg.points[0]; }
        for (int i = 1; i < c.Count; i++)
        {
            if (c[i] >= s)
            {
                float u = Mathf.InverseLerp(c[i - 1], c[i], s);
                dir = (seg.points[i] - seg.points[i - 1]).normalized;
                return Vector2.Lerp(seg.points[i - 1], seg.points[i], u);
            }
        }
        int n = seg.points.Count;
        dir = (seg.points[n - 1] - seg.points[n - 2]).normalized;
        return seg.points[n - 1];
    }
}
