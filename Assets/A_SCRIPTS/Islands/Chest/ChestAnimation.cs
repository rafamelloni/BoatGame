using System.Collections;
using UnityEngine;

public class ChestAnimation : MonoBehaviour
{
    [Header("Salto")]
    [SerializeField] private float _jumpHeight = 0.3f;
    [SerializeField] private float _jumpDuration = 0.2f;
    [Header("Rotacion")]
    [SerializeField] private float _maxRotAngle = 15f;
    [Header("Timing")]
    [SerializeField] private float _minInterval = 0.8f;
    [SerializeField] private float _maxInterval = 2f;
    [Header("Apertura")]
    [SerializeField] private float _openAngle = 110f;
    [SerializeField] private float _openDuration = 0.5f;
    [SerializeField] private Transform _lidTransform;

    private Vector3 _originalPos;
    private Quaternion _originalRot;
    private Vector3 _originalScale;
    private bool _animating = false;

    private void OnEnable()
    {
        _originalPos = transform.localPosition;
        _originalRot = transform.localRotation;
        _originalScale = transform.localScale;
        StartLoop();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _animating = false;
        transform.localPosition = _originalPos;
        transform.localRotation = _originalRot;
        transform.localScale = _originalScale;
    }

    public void StartLoop()
    {
        if (_animating) return;
        _animating = true;
        StartCoroutine(Loop());
    }

    public void StopAnimation()
    {
        StopAllCoroutines();
        _animating = false;
        transform.localPosition = _originalPos;
        transform.localRotation = _originalRot;
        transform.localScale = _originalScale;
    }

    public void OpenChest()
    {
        OpenChest(_lidTransform, _openDuration);
    }

    public void OpenChest(Transform lidTransform, float duration)
    {
        StopAnimation();
        StartCoroutine(DoOpen(lidTransform, duration));
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_minInterval, _maxInterval));
            yield return StartCoroutine(DoJump());
        }
    }

    private IEnumerator DoJump()
    {
        yield return StartCoroutine(Scale(_originalScale, Vector3.Scale(_originalScale, new Vector3(1.2f, 0.8f, 1.2f)), 0.08f));

        float t = 0f;
        float randomRotZ = Random.Range(-_maxRotAngle, _maxRotAngle);
        float randomRotX = Random.Range(-_maxRotAngle * 0.5f, _maxRotAngle * 0.5f);
        transform.localScale = Vector3.Scale(_originalScale, new Vector3(0.9f, 1.2f, 0.9f));

        while (t < _jumpDuration)
        {
            t += Time.deltaTime;
            float progress = t / _jumpDuration;
            float height = Mathf.Sin(progress * Mathf.PI) * _jumpHeight;
            transform.localPosition = _originalPos + Vector3.up * height;
            float rotProgress = Mathf.Sin(progress * Mathf.PI);
            transform.localRotation = _originalRot * Quaternion.Euler(
                rotProgress * randomRotX,
                0f,
                rotProgress * randomRotZ
            );
            yield return null;
        }

        transform.localPosition = _originalPos;
        transform.localRotation = _originalRot;
        yield return StartCoroutine(Scale(Vector3.Scale(_originalScale, new Vector3(1.2f, 0.8f, 1.2f)), _originalScale, 0.1f));
    }

    private IEnumerator DoOpen(Transform lid, float duration)
    {
        Quaternion startRot = lid.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(-_openAngle, 0f, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = EaseOutBack(t / duration);
            lid.localRotation = Quaternion.Lerp(startRot, targetRot, progress);
            yield return null;
        }

        lid.localRotation = targetRot;
    }

    private IEnumerator Scale(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        transform.localScale = to;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}