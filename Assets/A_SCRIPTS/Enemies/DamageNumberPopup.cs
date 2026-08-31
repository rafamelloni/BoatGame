using UnityEngine;
using TMPro;

// Va en el prefab del numero de daño (un Canvas en World Space con un
// TextMeshProUGUI adentro, armado a mano). Solo se encarga de mostrar el
// valor y animarse (subir + desvanecerse) mientras esta activo. El pool
// (DamageNumberPool) decide cuando se desactiva y lo devuelve; este script
// solo hace la parte visual mientras tanto - mismo reparto de
// responsabilidades que ya usás entre ParticlePool y el ParticleSystem de
// cada partícula.
public class DamageNumberPopup : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI _text;
    [Tooltip("Lo que rota para mirar a camara. Si lo dejas vacio, usa el transform del texto (no el del objeto raiz, asi el movimiento no se ve afectado).")]
    [SerializeField] private Transform _billboardTarget;

    [Header("Pop (escala al aparecer)")]
    [SerializeField] private float _popDuration = 0.12f;
    [SerializeField] private AnimationCurve _popCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Flote")]
    [SerializeField] private float _moveSpeed = 1f;
    [Tooltip("0 = siempre derecho para arriba. Mas alto = mas variacion hacia los costados.")]
    [Range(0f, 1f)]
    [SerializeField] private float _sidewaysRandomness = 0.4f;

    [Header("Fade")]
    [SerializeField] private float _fadeDuration = 0.8f;

    private float _elapsed;
    private Color _baseColor;
    private Camera _cam;
    private Vector3 _moveDir;
    private Vector3 _targetScale; // la escala que VOS configuraste a mano en el prefab (ej 0.01)

    private void Awake()
    {
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        if (_text != null) _baseColor = _text.color;
        if (_billboardTarget == null && _text != null) _billboardTarget = _text.transform;

        // Guardamos la escala del prefab ANTES de tocarla para nada, asi el
        // pop anima hacia el tamaño real que armaste en el editor y no a un
        // (1,1,1) fijo que te lo agranda todo.
        _targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        if (_cam == null) _cam = Camera.main;
        if (_text != null)
        {
            _baseColor.a = 1f;
            _text.color = _baseColor;
        }

        // Direccion random para el flote: mayormente para arriba, con algo
        // de variacion hacia un costado u otro para que no salgan todos en
        // fila recta.
        float sideways = Random.Range(-_sidewaysRandomness, _sidewaysRandomness);
        _moveDir = new Vector3(sideways, 1f, 0f).normalized;

        transform.localScale = Vector3.zero;
    }

    // Llamado por el pool justo despues de reactivar el objeto.
    public void Show(float damage)
    {
        if (_text != null)
            _text.text = Mathf.RoundToInt(damage).ToString();
    }

    private void Update()
    {
        // Pop: escala de 0 a _targetScale (la que armaste en el prefab)
        // durante _popDuration, despues se queda fija ahi.
        if (_elapsed < _popDuration)
        {
            float popT = _popDuration > 0f ? _elapsed / _popDuration : 1f;
            float scale = _popCurve.Evaluate(popT);
            transform.localScale = _targetScale * scale;
        }
        else if (transform.localScale != _targetScale)
        {
            transform.localScale = _targetScale;
        }

        transform.position += _moveDir * _moveSpeed * Time.deltaTime;

        // Billboard: solo el texto rota para mirar a camara, no el objeto
        // raiz (ese sigue moviendose/escalando sin que la rotacion lo afecte).
        if (_cam != null && _billboardTarget != null)
            _billboardTarget.rotation = _cam.transform.rotation;

        _elapsed += Time.deltaTime;
        if (_text != null)
        {
            float t = Mathf.Clamp01(_elapsed / _fadeDuration);
            Color c = _baseColor;
            c.a = 1f - t;
            _text.color = c;
        }
    }
}
