using System.Collections;
using UnityEngine;
using TMPro;

public class DamageZone : MonoBehaviour
{
    [SerializeField] private float _damagePerSecond = 5f;
    [SerializeField] private float _gracePeriod = 2f;
    [SerializeField] private float _damageScalePerSecond = 1.5f;
    [SerializeField] private TextMeshProUGUI _warningText;
    [SerializeField] private float _blinkInterval = 0.5f;

    private Coroutine _damageRoutine;
    private Coroutine _blinkRoutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RT_PlayerHealth health = other.GetComponent<RT_PlayerHealth>();
        if (health == null) return;
        _damageRoutine = StartCoroutine(DamageRoutine(health));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_damageRoutine != null) { StopCoroutine(_damageRoutine); _damageRoutine = null; }
        if (_blinkRoutine != null) { StopCoroutine(_blinkRoutine); _blinkRoutine = null; }
        if (_warningText != null)
        {
            _warningText.alpha = 1f;
            _warningText.gameObject.SetActive(false);
        }
    }

    private IEnumerator DamageRoutine(RT_PlayerHealth health)
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
            health.TakeDamage(damage);
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
}