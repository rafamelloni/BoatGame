using UnityEngine;
using System.Collections;

public class BurnStatus : MonoBehaviour
{
    private IDamageable _target;
    private Coroutine _routine;
    private GameObject _vfxInstance;

    public void ApplyBurn(IDamageable target, float damagePerTick, float tickInterval, float duration, GameObject fireVfxPrefab, Vector3 vfxOffset)
    {
        if (!gameObject.activeInHierarchy) return;

        _target = target;

        if (_routine != null)
            StopCoroutine(_routine);

        if (_vfxInstance == null && fireVfxPrefab != null)
        {
            _vfxInstance = Instantiate(fireVfxPrefab, transform);
            _vfxInstance.transform.localPosition = vfxOffset;
        }

        _routine = StartCoroutine(BurnRoutine(damagePerTick, tickInterval, duration));
    }

    private IEnumerator BurnRoutine(float damagePerTick, float tickInterval, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
            _target?.TakeDamage(damagePerTick);
        }

        if (_vfxInstance != null)
            Destroy(_vfxInstance);

        _routine = null;
        Destroy(this);
    }
}