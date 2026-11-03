using System.Collections;
using TMPro;
using UnityEngine;

public class BossArrivalText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _text;

    [Header("Timing")]
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    [Header("Punch Scale")]
    [SerializeField] private float _punchScale = 1.3f;
    [SerializeField] private float _punchDuration = 0.15f;

    private string _defaultText;
    private Coroutine _routine;

    public void Play()
    {
        Play(null);
    }

    // Sin mensaje usa el texto original del cartel (el del boss)
    public void Play(string message)
    {
        if (_defaultText == null) _defaultText = _text.text;
        _text.text = string.IsNullOrEmpty(message) ? _defaultText : message;

        gameObject.SetActive(true);
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // Reset
        Color c = _text.color;
        c.a = 0f;
        _text.color = c;
        _text.transform.localScale = Vector3.one;

        // Fade in + punch scale
        float t = 0f;
        while (t < _fadeInDuration)
        {
            t += Time.deltaTime;
            float progress = t / _fadeInDuration;

            c.a = Mathf.Lerp(0f, 1f, progress);
            _text.color = c;

            float scale = Mathf.Lerp(1f, _punchScale, Mathf.Sin(progress * Mathf.PI));
            _text.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        c.a = 1f;
        _text.color = c;
        _text.transform.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(_holdDuration);

        // Fade out
        t = 0f;
        while (t < _fadeOutDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / _fadeOutDuration);
            _text.color = c;
            yield return null;
        }

        c.a = 0f;
        _text.color = c;
        gameObject.SetActive(false);
    }
}