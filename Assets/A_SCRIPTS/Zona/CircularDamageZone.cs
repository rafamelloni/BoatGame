using System.Collections;
using UnityEngine;
using TMPro;

// Zona segura circular: si el player se aleja más de _radius del centro del mapa,
// después de un período de gracia empieza a recibir daño que escala con el tiempo.
// Misma lógica que DamageZone, pero por distancia en vez de triggers cuadrados.
public class CircularDamageZone : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Centro del mapa. Si está vacío usa este mismo objeto.")]
    [SerializeField] private Transform _center;
    [SerializeField] private float _radius = 200f;

    [Header("Player")]
    [SerializeField] private RT_PlayerHealth _playerHealth;

    [Header("Daño")]
    [SerializeField] private float _damagePerSecond = 5f;
    [SerializeField] private float _gracePeriod = 2f;
    [SerializeField] private float _damageScalePerSecond = 1.5f;

    [Header("Aviso")]
    [SerializeField] private TextMeshProUGUI _warningText;
    [SerializeField] private float _blinkInterval = 0.5f;

    private Coroutine _damageRoutine;
    private Coroutine _blinkRoutine;
    private bool _isOutside;

    public float Radius => _radius;
    public Vector3 Center => _center != null ? _center.position : transform.position;

    private void Update()
    {
        if (_playerHealth == null) return;

        Vector3 offset = _playerHealth.transform.position - Center;
        offset.y = 0f;
        bool outside = offset.sqrMagnitude > _radius * _radius;

        if (outside && !_isOutside) OnExitZone();
        else if (!outside && _isOutside) OnEnterZone();
    }

    private void OnExitZone()
    {
        _isOutside = true;
        _damageRoutine = StartCoroutine(DamageRoutine());
    }

    private void OnEnterZone()
    {
        _isOutside = false;
        StopAll();
    }

    private void OnDisable()
    {
        _isOutside = false;
        StopAll();
    }

    private void StopAll()
    {
        if (_damageRoutine != null) { StopCoroutine(_damageRoutine); _damageRoutine = null; }
        if (_blinkRoutine != null) { StopCoroutine(_blinkRoutine); _blinkRoutine = null; }
        if (_warningText != null)
        {
            _warningText.alpha = 1f;
            _warningText.gameObject.SetActive(false);
        }
    }

    private IEnumerator DamageRoutine()
    {
        yield return new WaitForSeconds(_gracePeriod);

        if (_warningText != null)
        {
            _warningText.gameObject.SetActive(true);
            _blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        float timeOutside = 0f;
        while (true)
        {
            float damage = _damagePerSecond * Mathf.Pow(_damageScalePerSecond, timeOutside / 5f);
            _playerHealth.TakeDamage(damage);
            timeOutside += 1f;
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            _warningText.alpha = 1f;
            yield return new WaitForSeconds(_blinkInterval);
            _warningText.alpha = 0f;
            yield return new WaitForSeconds(_blinkInterval);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 c = Center;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        const int segments = 128;
        Vector3 prev = c + new Vector3(_radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
