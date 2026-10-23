using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Genera el mapa del Overworld: reparte un "continente" de agua entre todos los
// OceanRegion hijos (Voronoi), le pone bordes irregulares, grosor y bordes oscuros.
// Corre en el Editor (ExecuteAlways): mover un océano o cambiar un valor rehace el mapa.
// Lo generado no se guarda en la escena (HideFlags.DontSave), se rehace al abrirla/al darle Play.
[ExecuteAlways]
public class OverworldMap : MonoBehaviour
{
    [Header("Material (URP Unlit) para bordes/fondo")]
    [SerializeField] private Material _oceanMaterial;

    [Header("Agua (opcional)")]
    [Tooltip("Material con tu shader de agua. Si está vacío, los océanos se pintan con color plano.")]
    [SerializeField] private Material _waterMaterial;
    [Tooltip("Qué tan abajo de la superficie queda el arrecife entre océanos (contra él se forma la espuma que los separa)")]
    [SerializeField] private float _ridgeBelowSurface = 0.03f;
    [Tooltip("Cuánto sobresale del agua el borde exterior del mapa")]
    [SerializeField] private float _coastLip = 0.08f;
    [Tooltip("Profundidad del fondo: más profundo = más color 'deep' del shader")]
    [SerializeField] private float _seabedDepth = 1.6f;
    [SerializeField] private float _waterUVScale = 0.1f;
    [SerializeField, Range(1, 12)] private int _waterRings = 6;
    [Tooltip("Cuánto se oscurece el color del océano para el color profundo del agua")]
    [SerializeField, Range(0f, 1f)] private float _deepDarken = 0.6f;
    [Tooltip("Cuánto se oscurece el color del océano para el fondo")]
    [SerializeField, Range(0f, 1f)] private float _seabedDarken = 0.55f;

    [Header("Forma general")]
    [SerializeField] private float _radius = 26f;
    [SerializeField, Range(0f, 0.5f)] private float _coastNoise = 0.18f;
    [SerializeField] private float _coastNoiseScale = 1.6f;
    [SerializeField] private int _coastPoints = 72;
    [SerializeField] private int _noiseSeed = 7;

    [Header("Bordes entre océanos")]
    [SerializeField] private float _edgeSegmentLength = 1.2f;
    [SerializeField] private float _edgeJitter = 0.55f;
    [SerializeField] private float _edgeJitterScale = 0.18f;
    [SerializeField] private float _gap = 0.35f;
    [SerializeField] private float _borderWidth = 0.4f;
    [SerializeField] private float _thickness = 0.8f;

    [Header("Colores")]
    [SerializeField, Range(0f, 1f)] private float _borderDarken = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _sideDarken = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _lockedDesaturate = 0.65f;
    [SerializeField, Range(0f, 1f)] private float _lockedDarken = 0.25f;

    [Header("Selección")]
    [SerializeField] private float _highlightLift = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _highlightBrighten = 0.18f;
    [SerializeField] private float _highlightSpeed = 8f;

    [Header("Nombres")]
    [SerializeField] private bool _showLabels = true;
    [SerializeField] private float _labelSize = 6f;
    [SerializeField] private string _lockedLabel = "Próximamente";

    private const string GeneratedName = "__Generated";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // Propiedades del WaterShaderTurbulence (reference names del Shader Graph)
    // Shader Graph del juego usa estos nombres; el shader Botes/OverworldWater usa _ShallowColor/_DeepColor
    public const string ShallowColorProp = "Color_F01C36BF";
    public const string DeepColorProp = "Color_7D9A58EC";
    public const string ShallowColorProp2 = "_ShallowColor";
    public const string DeepColorProp2 = "_DeepColor";

    public static bool TryGetWaterColors(Material m, out Color shallow, out Color deep)
    {
        shallow = deep = Color.white;
        if (m == null) return false;
        if (m.HasProperty(ShallowColorProp2)) { shallow = m.GetColor(ShallowColorProp2); deep = m.HasProperty(DeepColorProp2) ? m.GetColor(DeepColorProp2) : shallow; return true; }
        if (m.HasProperty(ShallowColorProp)) { shallow = m.GetColor(ShallowColorProp); deep = m.HasProperty(DeepColorProp) ? m.GetColor(DeepColorProp) : shallow; return true; }
        return false;
    }

    public static void SetWaterColors(Material m, Color shallow, Color deep)
    {
        if (m == null) return;
        if (m.HasProperty(ShallowColorProp2)) m.SetColor(ShallowColorProp2, shallow);
        if (m.HasProperty(DeepColorProp2)) m.SetColor(DeepColorProp2, deep);
        if (m.HasProperty(ShallowColorProp)) m.SetColor(ShallowColorProp, shallow);
        if (m.HasProperty(DeepColorProp)) m.SetColor(DeepColorProp, deep);
    }
    private const string ClipRadiusProp = "_ClipRadius";
    private const string ClipCenterProp = "_ClipCenterXZ";

    private bool UseWater => _waterMaterial != null;
    public Material WaterMaterial => _waterMaterial;

    private readonly List<OceanRegion> _regions = new List<OceanRegion>();
    private readonly List<Object> _generatedAssets = new List<Object>();
    private Transform _generatedRoot;
    private OceanRegion _highlighted;
    private int _lastHash;
    private MaterialPropertyBlock _block;

    public IReadOnlyList<OceanRegion> Regions => _regions;
    public float WalkableRadius => _radius * (1f - _coastNoise) - 1f;

    private void OnEnable()
    {
        Generate();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // No se puede destruir/crear objetos dentro de OnValidate: lo dejamos para el próximo tick
        UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Generate(); };
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            // En el Editor: si moviste/cambiaste un océano, rehacer
            int hash = ComputeHash();
            if (hash != _lastHash) Generate();
            else ApplyColors(); // para ver en vivo cambios de color/material
        }
        AnimateHighlight(Application.isPlaying ? Time.deltaTime : 1f);
    }

    // ───────────────────────── API ─────────────────────────

    public OceanRegion GetRegionAt(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        Vector2 p = new Vector2(local.x, local.z);
        foreach (var r in _regions)
            if (r != null && r.Contains(p)) return r;
        return null;
    }

    public void SetHighlighted(OceanRegion region)
    {
        _highlighted = region;
    }

    // Altura (mundo) de la superficie del agua en esa posición, incluyendo el "lift" de la zona elegida
    public float GetSurfaceY(Vector3 worldPos)
    {
        var r = GetRegionAt(worldPos);
        float lift = r != null ? r.currentLift : 0f;
        return transform.position.y + lift;
    }

    public Vector3 ClampToMap(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        Vector2 xz = new Vector2(local.x, local.z);
        float max = WalkableRadius;
        if (xz.magnitude > max)
        {
            xz = xz.normalized * max;
            local.x = xz.x;
            local.z = xz.y;
            return transform.TransformPoint(local);
        }
        return worldPos;
    }

    public Vector3 GetRegionCenterWorld(OceanRegion region)
    {
        return transform.TransformPoint(new Vector3(region.centroid.x, 0f, region.centroid.y));
    }

    [ContextMenu("Regenerar mapa")]
    public void Generate()
    {
        Clear();
        CollectRegions();
        _lastHash = ComputeHash();
        if (_regions.Count == 0) return;

        _generatedRoot = new GameObject(GeneratedName).transform;
        _generatedRoot.SetParent(transform, false);
        _generatedRoot.gameObject.hideFlags = HideFlags.DontSave;

        List<Vector2> coast = BuildCoast();
        var seeds = new Vector2[_regions.Count];
        for (int i = 0; i < _regions.Count; i++)
        {
            Vector3 lp = transform.InverseTransformPoint(_regions[i].transform.position);
            seeds[i] = new Vector2(lp.x, lp.z);
        }

        var cells = new List<Vector2>[_regions.Count];
        for (int i = 0; i < _regions.Count; i++)
        {
            List<Vector2> cell = new List<Vector2>(coast);
            for (int j = 0; j < seeds.Length && cell.Count >= 3; j++)
            {
                if (i == j) continue;
                cell = ClipHalfPlane(cell, seeds[i], seeds[j]);
            }
            if (cell.Count < 3) { cells[i] = null; continue; }
            cell = Jitter(cell);
            EnsureCCW(cell);
            cells[i] = cell;
        }

        for (int i = 0; i < _regions.Count; i++)
        {
            var cell = cells[i];
            if (cell == null) { _regions[i].polygon = null; continue; }
            Vector2 c = Centroid(cell);

            if (UseWater)
            {
                // Océanos pegados: sin hueco ni borde, la separación la marca la espuma
                _regions[i].polygon = cell.ToArray();
                _regions[i].centroid = c;
                BuildWaterVisual(_regions[i], i, cells, c);
                continue;
            }
            List<Vector2> outer = Shrink(cell, c, _gap);
            List<Vector2> inner = Shrink(cell, c, _gap + _borderWidth);

            var region = _regions[i];
            region.polygon = outer.ToArray();
            region.centroid = c;
            BuildVisual(region, outer, inner, c);
        }

        ApplyColors();
        MarkDontSave(_generatedRoot);
    }

    // Que nada de lo generado (objetos y componentes) se guarde en la escena
    private static void MarkDontSave(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.hideFlags = HideFlags.DontSave;
            foreach (var c in t.GetComponents<Component>())
                if (c != null) c.hideFlags = HideFlags.DontSave;
        }
    }

    // ───────────────────────── Generación ─────────────────────────

    private void CollectRegions()
    {
        _regions.Clear();
        foreach (Transform child in transform)
        {
            var r = child.GetComponent<OceanRegion>();
            if (r != null && r.gameObject.activeSelf) _regions.Add(r);
        }
    }

    private List<Vector2> BuildCoast()
    {
        var pts = new List<Vector2>(_coastPoints);
        for (int i = 0; i < _coastPoints; i++)
        {
            float a = (float)i / _coastPoints * Mathf.PI * 2f;
            float nx = Mathf.Cos(a) * _coastNoiseScale + _noiseSeed * 13.1f;
            float ny = Mathf.Sin(a) * _coastNoiseScale + _noiseSeed * 7.7f;
            float n = Mathf.PerlinNoise(nx, ny) * 2f - 1f;
            float r = _radius * (1f + _coastNoise * n);
            pts.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        return pts;
    }

    // Sutherland–Hodgman: se queda con la mitad del plano más cerca de "own" que de "other"
    private static List<Vector2> ClipHalfPlane(List<Vector2> poly, Vector2 own, Vector2 other)
    {
        Vector2 mid = (own + other) * 0.5f;
        Vector2 n = (other - own).normalized;
        var result = new List<Vector2>(poly.Count + 2);
        for (int k = 0; k < poly.Count; k++)
        {
            Vector2 a = poly[k];
            Vector2 b = poly[(k + 1) % poly.Count];
            float da = Vector2.Dot(a - mid, n);
            float db = Vector2.Dot(b - mid, n);
            bool ina = da <= 0f, inb = db <= 0f;
            if (ina) result.Add(a);
            if (ina != inb)
            {
                float t = da / (da - db);
                result.Add(Vector2.Lerp(a, b, t));
            }
        }
        return result;
    }

    // Subdivide los bordes y los desplaza con ruido según la posición en el mundo,
    // así el borde compartido por dos océanos queda igual de los dos lados.
    private List<Vector2> Jitter(List<Vector2> poly)
    {
        var result = new List<Vector2>();
        for (int k = 0; k < poly.Count; k++)
        {
            Vector2 a = poly[k];
            Vector2 b = poly[(k + 1) % poly.Count];
            int segs = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / Mathf.Max(0.1f, _edgeSegmentLength)));
            for (int s = 0; s < segs; s++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)s / segs);
                result.Add(p + NoiseOffset(p));
            }
        }
        return result;
    }

    private Vector2 NoiseOffset(Vector2 p)
    {
        // Redondeo para que el mismo punto calculado desde dos celdas dé exactamente igual
        p = new Vector2(Mathf.Round(p.x * 100f) / 100f, Mathf.Round(p.y * 100f) / 100f);
        float s = _edgeJitterScale;
        float ox = Mathf.PerlinNoise(p.x * s + 31.7f + _noiseSeed, p.y * s + 11.3f) * 2f - 1f;
        float oy = Mathf.PerlinNoise(p.x * s + 77.1f, p.y * s + 53.9f + _noiseSeed) * 2f - 1f;
        return new Vector2(ox, oy) * _edgeJitter;
    }

    private static void EnsureCCW(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
            area += a.x * b.y - b.x * a.y;
        }
        if (area < 0f) poly.Reverse();
    }

    private static Vector2 Centroid(List<Vector2> poly)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in poly) sum += p;
        return sum / poly.Count;
    }

    private static List<Vector2> Shrink(List<Vector2> poly, Vector2 c, float amount)
    {
        var result = new List<Vector2>(poly.Count);
        foreach (var p in poly)
        {
            Vector2 dir = p - c;
            float d = dir.magnitude;
            result.Add(d > 0.0001f ? c + dir * (Mathf.Max(0.05f, d - amount) / d) : p);
        }
        return result;
    }

    private void BuildVisual(OceanRegion region, List<Vector2> outer, List<Vector2> inner, Vector2 c)
    {
        var go = new GameObject(region.name + "_Visual");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(_generatedRoot, false);

        var verts = new List<Vector3>();
        var fill = new List<int>();
        var border = new List<int>();
        var sides = new List<int>();
        int n = outer.Count;

        // Relleno (abanico desde el centro sobre el polígono interior)
        int center = verts.Count;
        verts.Add(new Vector3(c.x, 0f, c.y));
        int innerStart = verts.Count;
        for (int i = 0; i < n; i++) verts.Add(new Vector3(inner[i].x, 0f, inner[i].y));
        for (int i = 0; i < n; i++)
        {
            int a = innerStart + i, b = innerStart + (i + 1) % n;
            fill.Add(center); fill.Add(b); fill.Add(a);
        }

        // Borde (anillo entre exterior e interior)
        int bo = verts.Count;
        for (int i = 0; i < n; i++) verts.Add(new Vector3(outer[i].x, 0f, outer[i].y));
        int bi = verts.Count;
        for (int i = 0; i < n; i++) verts.Add(new Vector3(inner[i].x, 0f, inner[i].y));
        for (int i = 0; i < n; i++)
        {
            int o0 = bo + i, o1 = bo + (i + 1) % n, i0 = bi + i, i1 = bi + (i + 1) % n;
            border.Add(o0); border.Add(i1); border.Add(o1);
            border.Add(o0); border.Add(i0); border.Add(i1);
        }

        // Costados (grosor)
        for (int i = 0; i < n; i++)
        {
            Vector3 a = new Vector3(outer[i].x, 0f, outer[i].y);
            Vector3 b = new Vector3(outer[(i + 1) % n].x, 0f, outer[(i + 1) % n].y);
            Vector3 down = Vector3.down * _thickness;
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(b + down); verts.Add(a + down);
            sides.Add(s); sides.Add(s + 1); sides.Add(s + 2);
            sides.Add(s); sides.Add(s + 2); sides.Add(s + 3);
        }

        // Sin DontSave: si queda huérfana la limpia Unity solo (y no se guarda porque su MeshFilter es DontSave)
        var mesh = new Mesh { name = region.name + "_Mesh" };
        mesh.SetVertices(verts);
        mesh.subMeshCount = 3;
        mesh.SetTriangles(fill, 0);
        mesh.SetTriangles(border, 1);
        mesh.SetTriangles(sides, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _generatedAssets.Add(mesh);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { _oceanMaterial, _oceanMaterial, _oceanMaterial };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        region.visual = go.transform;
        region.visualRenderer = mr;
        region.waterRenderer = null;
        region.currentLift = 0f;
        region.currentHighlight = 0f;

        if (_showLabels) BuildLabel(region, c, go.transform);
    }

    // Versión con agua: fondo + paredes (unlit) y la superficie de agua encima.
    // Entre dos océanos la pared queda apenas sumergida (arrecife): no se ve, pero el shader
    // detecta la poca profundidad y dibuja la línea de espuma que los separa.
    // En el borde exterior del mapa la pared sobresale un poco del agua.
    private void BuildWaterVisual(OceanRegion region, int index, List<Vector2>[] cells, Vector2 c)
    {
        var poly = cells[index];
        var go = new GameObject(region.name + "_Visual");
        go.transform.SetParent(_generatedRoot, false);
        int n = poly.Count;
        float bottom = -Mathf.Max(_seabedDepth, 0.1f);

        var verts = new List<Vector3>();
        var seabed = new List<int>();
        var ridges = new List<int>();
        var coastWalls = new List<int>();

        // Fondo
        int ce = verts.Count;
        verts.Add(new Vector3(c.x, bottom, c.y));
        int sb = verts.Count;
        for (int i = 0; i < n; i++) verts.Add(new Vector3(poly[i].x, bottom, poly[i].y));
        for (int i = 0; i < n; i++) { seabed.Add(ce); seabed.Add(sb + (i + 1) % n); seabed.Add(sb + i); }

        // Paredes por borde
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % n];
            int neighbor = FindNeighbor(a, b, index, cells);
            if (neighbor < 0)
                AddWall(verts, coastWalls, a, b, _coastLip, bottom);          // borde del mapa
            else if (neighbor > index)
                AddWall(verts, ridges, a, b, -_ridgeBelowSurface, bottom);    // arrecife (lo arma uno solo de los dos)
        }

        var rimMesh = new Mesh { name = region.name + "_Rim" };
        rimMesh.SetVertices(verts);
        rimMesh.subMeshCount = 3;
        rimMesh.SetTriangles(seabed, 0);
        rimMesh.SetTriangles(ridges, 1);
        rimMesh.SetTriangles(coastWalls, 2);
        rimMesh.RecalculateNormals();
        rimMesh.RecalculateBounds();
        _generatedAssets.Add(rimMesh);

        go.AddComponent<MeshFilter>().sharedMesh = rimMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { _oceanMaterial, _oceanMaterial, _oceanMaterial };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Superficie del agua (subdividida en anillos para las olas)
        var waterGo = new GameObject("Water");
        waterGo.transform.SetParent(go.transform, false);
        var waterMesh = BuildWaterSurface(poly, c);
        waterMesh.name = region.name + "_Water";
        _generatedAssets.Add(waterMesh);
        waterGo.AddComponent<MeshFilter>().sharedMesh = waterMesh;
        var wr = waterGo.AddComponent<MeshRenderer>();
        wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Se usa el material tal cual (sin copiarlo), así si tocás sus parámetros se ve en vivo.
        // Los colores se pisan por MaterialPropertyBlock (para el resaltado y los bloqueados).
        wr.sharedMaterial = region.waterMaterialOverride != null ? region.waterMaterialOverride : _waterMaterial;

        region.visual = go.transform;
        region.visualRenderer = mr;
        region.waterRenderer = wr;
        region.currentLift = 0f;
        region.currentHighlight = 0f;

        if (_showLabels) BuildLabel(region, c, go.transform);
    }

    // Índice del océano del otro lado del borde a-b, o -1 si es el borde exterior del mapa
    private static int FindNeighbor(Vector2 a, Vector2 b, int self, List<Vector2>[] cells)
    {
        Vector2 mid = (a + b) * 0.5f;
        Vector2 d = (b - a).normalized;
        Vector2 outward = new Vector2(d.y, -d.x); // polígono antihorario: afuera queda a la derecha
        Vector2 probe = mid + outward * 0.08f;
        for (int j = 0; j < cells.Length; j++)
        {
            if (j == self || cells[j] == null) continue;
            if (PointInPolygon(probe, cells[j])) return j;
        }
        return -1;
    }

    private static bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i], b = poly[j];
            if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x))
                inside = !inside;
        }
        return inside;
    }

    private static void AddWall(List<Vector3> verts, List<int> tris, Vector2 a2, Vector2 b2, float top, float bottom)
    {
        Vector3 a = new Vector3(a2.x, top, a2.y), b = new Vector3(b2.x, top, b2.y);
        Vector3 down = new Vector3(0f, bottom - top, 0f);
        int s = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(b + down); verts.Add(a + down);
        tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
        tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
    }

    private Mesh BuildWaterSurface(List<Vector2> poly, Vector2 c)
    {
        int n = poly.Count;
        int rings = Mathf.Max(1, _waterRings);
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        verts.Add(new Vector3(c.x, 0f, c.y));
        uvs.Add(c * _waterUVScale);
        for (int r = 1; r <= rings; r++)
        {
            float t = (float)r / rings;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(c, poly[i], t);
                verts.Add(new Vector3(p.x, 0f, p.y));
                uvs.Add(p * _waterUVScale);
            }
        }
        // centro
        for (int i = 0; i < n; i++) { tris.Add(0); tris.Add(1 + (i + 1) % n); tris.Add(1 + i); }
        // anillos
        for (int r = 1; r < rings; r++)
        {
            int a0 = 1 + (r - 1) * n, b0 = 1 + r * n;
            for (int i = 0; i < n; i++)
            {
                int i1 = (i + 1) % n;
                tris.Add(a0 + i); tris.Add(a0 + i1); tris.Add(b0 + i1);
                tris.Add(a0 + i); tris.Add(b0 + i1); tris.Add(b0 + i);
            }
        }
        var mesh = new Mesh();
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildLabel(OceanRegion region, Vector2 c, Transform parent)
    {
        var labelGo = new GameObject("Label");
        labelGo.hideFlags = HideFlags.DontSave;
        labelGo.transform.SetParent(parent, false);
        labelGo.transform.localPosition = new Vector3(c.x, 0.05f, c.y);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.text = region.unlocked ? region.displayName : $"{region.displayName}\n<size=60%>{_lockedLabel}</size>";
        tmp.fontSize = _labelSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, region.unlocked ? 0.9f : 0.55f);
        tmp.rectTransform.sizeDelta = new Vector2(14f, 5f);
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }

    // ───────────────────────── Colores / selección ─────────────────────────

    private Color BaseColor(OceanRegion r)
    {
        return r.unlocked ? r.color : Desaturate(r.color);
    }

    private Color Desaturate(Color c)
    {
        float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        c = Color.Lerp(c, new Color(lum, lum, lum, c.a), _lockedDesaturate);
        return Color.Lerp(c, new Color(0f, 0f, 0f, c.a), _lockedDarken);
    }

    private void ApplyColors()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        foreach (var r in _regions)
        {
            if (r == null || r.visualRenderer == null) continue;
            float hl = r.currentHighlight * _highlightBrighten;
            Color top = Color.Lerp(BaseColor(r), Color.white, hl);
            if (r.waterRenderer != null)
            {
                // Modo agua: 0 fondo, 1 arrecifes entre océanos, 2 borde exterior
                SetColor(r.visualRenderer, 0, Color.Lerp(top, Color.black, _seabedDarken));
                SetColor(r.visualRenderer, 1, Color.Lerp(top, Color.black, _borderDarken));
                SetColor(r.visualRenderer, 2, Color.Lerp(top, Color.black, _sideDarken));
                ApplyWaterColors(r, hl);
                continue;
            }
            SetColor(r.visualRenderer, 0, top);
            SetColor(r.visualRenderer, 1, Color.Lerp(top, Color.black, _borderDarken));
            SetColor(r.visualRenderer, 2, Color.Lerp(top, Color.black, _sideDarken));
        }
    }

    private void ApplyWaterColors(OceanRegion r, float hl)
    {
        Material src = r.waterRenderer.sharedMaterial;
        if (src == null) return;

        Color shallow, deep;
        if (r.waterMaterialOverride != null)
        {
            // Material propio: respeta sus colores (desaturados si está bloqueado)
            TryGetWaterColors(src, out shallow, out deep);
            if (!r.unlocked) { shallow = Desaturate(shallow); deep = Desaturate(deep); }
        }
        else
        {
            // Sin material propio: el agua toma el color del océano
            shallow = BaseColor(r);
            deep = Color.Lerp(shallow, Color.black, _deepDarken);
        }

        _block.Clear();
        Color s = Color.Lerp(shallow, Color.white, hl);
        Color d = Color.Lerp(deep, Color.white, hl * 0.5f);
        _block.SetColor(ShallowColorProp, s);
        _block.SetColor(DeepColorProp, d);
        _block.SetColor(ShallowColorProp2, s);
        _block.SetColor(DeepColorProp2, d);
        _block.SetFloat(ClipRadiusProp, 100000f);           // sin recorte circular del juego
        _block.SetVector(ClipCenterProp, Vector4.zero);
        r.waterRenderer.SetPropertyBlock(_block);
    }

    private void SetColor(MeshRenderer mr, int index, Color c)
    {
        _block.Clear();
        _block.SetColor(BaseColorId, c);
        mr.SetPropertyBlock(_block, index);
    }

    private void AnimateHighlight(float dt)
    {
        bool changed = false;
        float k = Mathf.Clamp01(dt * _highlightSpeed);
        foreach (var r in _regions)
        {
            if (r == null || r.visual == null) continue;
            float target = r == _highlighted ? 1f : 0f;
            if (Mathf.Abs(r.currentHighlight - target) < 0.001f) continue;
            r.currentHighlight = Mathf.Lerp(r.currentHighlight, target, k);
            r.currentLift = UseWater ? 0f : r.currentHighlight * _highlightLift; // pegados: solo se aclara
            r.visual.localPosition = new Vector3(0f, r.currentLift, 0f);
            changed = true;
        }
        if (changed) ApplyColors();
    }

    // ───────────────────────── Limpieza ─────────────────────────

    private void Clear()
    {
        foreach (var a in _generatedAssets)
            if (a != null) DestroySafe(a);
        _generatedAssets.Clear();

        // Por si quedó algo de una generación anterior (recompilación, etc.)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == GeneratedName) DestroySafe(child.gameObject);
        }
        _generatedRoot = null;
    }

    private static void DestroySafe(Object o)
    {
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

    private int ComputeHash()
    {
        unchecked
        {
            int h = 17;
            foreach (Transform child in transform)
            {
                var r = child.GetComponent<OceanRegion>();
                if (r == null) continue;
                h = h * 31 + child.localPosition.GetHashCode();
                h = h * 31 + r.color.GetHashCode();
                h = h * 31 + r.unlocked.GetHashCode();
                h = h * 31 + (r.waterMaterialOverride != null ? r.waterMaterialOverride.GetInstanceID() : 0);
                h = h * 31 + (r.displayName != null ? r.displayName.GetHashCode() : 0);
                h = h * 31 + (r.gameObject.activeSelf ? 1 : 0);
            }
            return h;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Vector3 c = transform.position;
        float r = WalkableRadius;
        Vector3 prev = c + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
