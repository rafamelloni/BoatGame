using System.Collections.Generic;
using UnityEngine;

// Contorno por objeto (inverted hull). Ponelo en la raíz de un barco/enemigo:
// al activarse le agrega el material de outline a todos sus MeshRenderer / SkinnedMeshRenderer hijos,
// y lo saca al desactivarse. Funciona con pools (OnEnable/OnDisable).
// Grosor y color por objeto (si no, usa los del material M_ToonOutline de Resources).
public class ToonOutline : MonoBehaviour
{
    private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");

    [Tooltip("Si queda vacío usa Resources/M_ToonOutline")]
    [SerializeField] private Material _outlineMaterial;
    [Tooltip("Pisa el grosor y color del material solo para este objeto")]
    [SerializeField] private bool _overrideMaterial = true;
    [SerializeField, Range(0f, 12f)] private float _width = 3f;
    [SerializeField] private Color _color = new Color(0.16f, 0.08f, 0.06f, 1f);
    [Tooltip("Renderers a los que NO ponerle outline (ej. velas, banderas)")]
    [SerializeField] private List<Renderer> _exclude = new List<Renderer>();

    private readonly List<Renderer> _applied = new List<Renderer>();
    private MaterialPropertyBlock _mpb;

    public float Width { get => _width; set { _width = value; ApplyProperties(); } }
    public Color Color { get => _color; set { _color = value; ApplyProperties(); } }

    private void OnEnable()
    {
        if (_outlineMaterial == null) _outlineMaterial = Resources.Load<Material>("M_ToonOutline");
        if (_outlineMaterial == null)
        {
            Debug.LogWarning("[ToonOutline] No encontré el material M_ToonOutline en Resources.", this);
            return;
        }

        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (_exclude.Contains(r)) continue;

            var mats = r.sharedMaterials;
            bool already = false;
            foreach (var m in mats) if (m == _outlineMaterial) { already = true; break; }
            if (already) continue;

            var list = new List<Material>(mats) { _outlineMaterial };
            r.sharedMaterials = list.ToArray();
            _applied.Add(r);
        }
        ApplyProperties();
    }

    private void OnDisable()
    {
        foreach (var r in _applied)
        {
            if (r == null) continue;
            var list = new List<Material>(r.sharedMaterials);
            int idx = list.LastIndexOf(_outlineMaterial);
            if (idx >= 0)
            {
                if (_overrideMaterial) r.SetPropertyBlock(null, idx);
                list.RemoveAt(idx);
            }
            r.sharedMaterials = list.ToArray();
        }
        _applied.Clear();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled) ApplyProperties();
    }

    private void ApplyProperties()
    {
        if (!_overrideMaterial) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        foreach (var r in _applied)
        {
            if (r == null) continue;
            // solo al slot del outline (el último), así no toca el material del barco
            int idx = r.sharedMaterials.Length - 1;
            r.GetPropertyBlock(_mpb, idx);
            _mpb.SetColor(ColorID, _color);
            _mpb.SetFloat(WidthID, _width);
            r.SetPropertyBlock(_mpb, idx);
        }
    }
}
