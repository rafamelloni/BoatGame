using TMPro;
using UnityEngine;

// Hace flotar cada letra de un TextMeshPro por separado, como un idle.
[RequireComponent(typeof(TMP_Text))]
public class TMPLetterFloat : MonoBehaviour
{
    [Header("Movimiento (relativo al tamaño de la fuente)")]
    [Tooltip("Cuánto sube y baja cada letra (0.08 = 8% del tamaño de la fuente)")]
    [SerializeField] private float amplitude = 0.08f;
    [Tooltip("Cuánto se mece de costado")]
    [SerializeField] private float horizontal = 0.02f;
    [Tooltip("Grados que rota cada letra")]
    [SerializeField] private float rotation = 4f;

    [Header("Ritmo")]
    [SerializeField] private float speed = 2f;
    [Tooltip("Desfase entre una letra y la siguiente")]
    [SerializeField] private float letterOffset = 0.7f;
    [Tooltip("0 = todas iguales, 1 = cada letra con velocidad y fase bien distintas")]
    [SerializeField, Range(0f, 1f)] private float randomness = 0.5f;
    [Tooltip("Tildado: se mueve aunque el juego esté en pausa (timeScale 0)")]
    [SerializeField] private bool unscaledTime = true;

    private TMP_Text _text;
    private TMP_MeshInfo[] _cached;
    private bool _dirty = true;
    private bool _updating;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _dirty = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        if (_text != null) _text.ForceMeshUpdate(); // deja las letras en su lugar
    }

    private void OnTextChanged(Object obj)
    {
        if (!_updating && obj == _text) _dirty = true;
    }

    // Número "aleatorio" fijo por letra (siempre el mismo para la misma posición)
    private static float Hash(int i)
    {
        float h = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    private void LateUpdate()
    {
        if (_dirty)
        {
            _updating = true;
            _text.ForceMeshUpdate();
            _updating = false;
            _cached = _text.textInfo.CopyMeshInfoVertexData();
            _dirty = false;
        }

        TMP_TextInfo info = _text.textInfo;
        if (info.characterCount == 0 || _cached == null) return;

        float t = unscaledTime ? Time.unscaledTime : Time.time;
        float size = _text.fontSize;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            int mat = ch.materialReferenceIndex;
            int v = ch.vertexIndex;
            if (mat >= _cached.Length) continue;

            Vector3[] src = _cached[mat].vertices;
            Vector3[] dst = info.meshInfo[mat].vertices;

            float r = Hash(i);
            float phase = i * letterOffset + r * 6.28f * randomness;
            float sp = speed * (1f + (r - 0.5f) * 0.8f * randomness);

            Vector3 center = (src[v] + src[v + 2]) * 0.5f;
            Vector3 offset = new Vector3(
                Mathf.Cos(t * sp * 0.7f + phase) * horizontal * size,
                Mathf.Sin(t * sp + phase) * amplitude * size,
                0f);
            Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Sin(t * sp * 0.8f + phase * 1.3f) * rotation);

            for (int k = 0; k < 4; k++)
                dst[v + k] = rot * (src[v + k] - center) + center + offset;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            _text.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}
