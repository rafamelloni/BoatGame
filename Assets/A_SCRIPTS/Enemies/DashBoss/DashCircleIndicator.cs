using System;
using System.Collections;
using UnityEngine;

public class DashCircleIndicator : MonoBehaviour
{
    [SerializeField] private Transform _innerCircle;

    private Coroutine _routine;

    /// <summary>
    /// Escala el círculo interno de 1 a 0 en <duration> segundos.
    /// Llama onComplete cuando termina (ahí ejecutás el dash).
    /// </summary>
    public void Play(float duration, Action onComplete = null)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Animate(duration, onComplete));
    }

    public void Cancel()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator Animate(float duration, Action onComplete)
    {
        _innerCircle.localScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(1f, 0f, t);
            _innerCircle.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        _innerCircle.localScale = Vector3.zero;
        onComplete?.Invoke();
    }
}
