using UnityEngine;

// Franja sobre el agua que muestra el recorrido exacto de un dash.
// Usa el mismo shader que el indicador del Spike (Botes/DashTelegraph), pero no depende
// de ningún enemigo: cualquier boss puede usarla con Spawn(desde, hasta, ...).
//  - Spawn: aparece y se llena de atrás hacia adelante durante fillDuration.
//  - Fire: flash y se desvanece en el lugar (se destruye sola).
//  - Cancel: la borra en el momento.
// Es un objeto suelto para que no herede la inclinación ni la rotación del boss.
public class DashLineTelegraph : MonoBehaviour
{
    private const float FadeIn = 0.08f;
    private const float FadeOut = 0.25f;
    private const float HoverOverWater = 0.35f;
    private const float FallbackDrop = 1f;

    private static readonly int ProgressID = Shader.PropertyToID("_Progress");
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");
    private static readonly int FlashID = Shader.PropertyToID("_Flash");
    private static readonly int LengthID = Shader.PropertyToID("_Length");
    private static readonly int WidthID = Shader.PropertyToID("_Width");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int EdgeAlphaID = Shader.PropertyToID("_EdgeAlpha");
    private static readonly int FillAlphaID = Shader.PropertyToID("_FillAlpha");
    private static readonly int ChevronAlphaID = Shader.PropertyToID("_ChevronAlpha");
    private static readonly int EndMarkerID = Shader.PropertyToID("_EndMarker");

    private static Mesh _sharedMesh;

    private Material _material;
    private float _fillDuration;
    private float _elapsed;
    private float _alpha;
    private float _flash;
    private bool _fired;
    private float _waterY;
    private float _width;

    public static DashLineTelegraph Spawn(Vector3 from, Vector3 to, float fillDuration, float width, Color color)
    {
        Shader shader = Shader.Find("Botes/DashTelegraph");
        if (shader == null)
        {
            Debug.LogWarning("[DashLineTelegraph] No encontré el shader 'Botes/DashTelegraph'.");
            return null;
        }

        var go = new GameObject("DashLineTelegraph", typeof(MeshFilter), typeof(MeshRenderer));
        var line = go.AddComponent<DashLineTelegraph>();
        line.Init(shader, from, to, fillDuration, width, color);
        return line;
    }

    public void Fire()
    {
        if (_fired) return;
        _fired = true;
        _flash = 1f;
        _material.SetFloat(ProgressID, 1f);
    }

    // Para enemigos que siguen apuntando mientras se llena la franja
    public void UpdatePath(Vector3 from, Vector3 to)
    {
        if (_fired) return;
        ApplyPath(from, to);
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    private void Init(Shader shader, Vector3 from, Vector3 to, float fillDuration, float width, Color color)
    {
        _fillDuration = fillDuration;
        _width = width;
        _waterY = FindWaterY(from);

        _material = new Material(shader) { name = "DashLineTelegraph (runtime)" };
        _material.SetColor(ColorID, color);
        _material.SetFloat(EdgeAlphaID, 0.7f);
        _material.SetFloat(FillAlphaID, 0.16f);
        _material.SetFloat(ChevronAlphaID, 0.5f);
        _material.SetFloat(EndMarkerID, 0f);
        _material.SetFloat(WidthID, width);
        _material.SetFloat(ProgressID, 0f);
        _material.SetFloat(AlphaID, 0f);
        _material.SetFloat(FlashID, 0f);

        GetComponent<MeshFilter>().sharedMesh = GetMesh();
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        ApplyPath(from, to);
    }

    private void ApplyPath(Vector3 from, Vector3 to)
    {
        Vector3 flat = to - from;
        flat.y = 0f;
        float length = Mathf.Max(flat.magnitude, 1f);
        Vector3 direction = flat.sqrMagnitude > 0.001f ? flat.normalized : Vector3.forward;

        Vector3 start = from;
        start.y = _waterY;
        transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction, Vector3.up));
        transform.localScale = new Vector3(_width, 1f, length);
        _material.SetFloat(LengthID, length);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (!_fired)
        {
            _elapsed += dt;
            _alpha = Mathf.MoveTowards(_alpha, 1f, dt / FadeIn);
            _material.SetFloat(ProgressID, _fillDuration > 0f ? Mathf.Clamp01(_elapsed / _fillDuration) : 1f);
        }
        else
        {
            _alpha = Mathf.MoveTowards(_alpha, 0f, dt / FadeOut);
            _flash = Mathf.MoveTowards(_flash, 0f, dt / (FadeOut * 0.6f));
            if (_alpha <= 0.001f)
            {
                Destroy(gameObject);
                return;
            }
        }

        _material.SetFloat(AlphaID, _alpha);
        _material.SetFloat(FlashID, _flash);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    private static float FindWaterY(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f, ~0, QueryTriggerInteraction.Collide);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
            if (hit.collider.CompareTag("Floor") && hit.point.y > best) best = hit.point.y;

        return best > float.NegativeInfinity ? best + HoverOverWater : position.y - FallbackDrop;
    }

    // Quad plano: x = ancho centrado, z = 0..1 hacia adelante (se escala al largo)
    private static Mesh GetMesh()
    {
        if (_sharedMesh != null) return _sharedMesh;

        _sharedMesh = new Mesh { name = "DashLineTelegraphQuad" };
        _sharedMesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 0f, 1f), new Vector3(0.5f, 0f, 1f)
        };
        _sharedMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        _sharedMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        _sharedMesh.RecalculateNormals();
        // El largo se aplica con la escala del transform, los bounds siguen siendo correctos
        _sharedMesh.RecalculateBounds();
        return _sharedMesh;
    }
}
