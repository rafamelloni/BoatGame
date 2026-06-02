using System.Collections;
using UnityEngine;

public class WarningOrb : MonoBehaviour
{
    [Header("Escala")]
    [SerializeField] private float _startScale = 0.1f;
    [SerializeField] private float _endScale = 1f;

    [Header("Daño")]
    [SerializeField] private float _damageRadius = 0.5f;
    [SerializeField] private LayerMask _damageLayer;
    [SerializeField] private float _damage = 10f;

    [Header("Debug")]
    [SerializeField] private bool _triggerOnPlay = false;
    [SerializeField] private KeyCode _debugKey = KeyCode.Space;

    private float _growDuration = 1.5f;
    private Material _mat;
    private Coroutine _routine;
    private System.Action _onComplete;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _mat = GetComponent<MeshRenderer>().material;
    }

    private void OnEnable()
    {
        _mat = GetComponent<MeshRenderer>().material;
        var mr = GetComponent<MeshRenderer>();
        mr.localBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        Reset();
    }

    private void Start()
    {
        if (_triggerOnPlay)
            Trigger(_growDuration);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            Trigger(_growDuration);
    }

    public void Trigger(float duration, System.Action onComplete = null)
    {
        _growDuration = duration;
        _onComplete = onComplete;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(GrowRoutine());
    }

    private IEnumerator GrowRoutine()
    {
        Reset();
        float elapsed = 0f;

        while (elapsed < _growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _growDuration;
            float smooth = t * t * (3f - 2f * t);

            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, smooth);
            SetAlpha(Mathf.Lerp(0.15f, 0.6f, smooth));

            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        SetAlpha(1f);

        Collider[] hits = Physics.OverlapSphere(transform.position, _damageRadius, _damageLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(_damage);
        }

        _onComplete?.Invoke();
    }

    private void Reset()
    {
        transform.localScale = Vector3.one * _startScale;
        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        if (_mat == null) return;
        Color c = _mat.GetColor(ColorProp);
        c.a = a;
        _mat.SetColor(ColorProp, c);
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        // Invocar antes de nullear para que el pattern limpie _activeOrbs
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

}