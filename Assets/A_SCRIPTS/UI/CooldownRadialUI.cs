using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CooldownRadialUI : MonoBehaviour
{
    [SerializeField] private Image radialImage;
    [SerializeField] private float _maxRotationAngle = 45f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        radialImage.fillAmount = 0f;
        radialImage.enabled = false;
    }

    public void PlayCooldown(float duration)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(CooldownRoutine(duration));
    }

    public IEnumerator PlayCooldownRoutine(float duration)
    {
        yield return StartCoroutine(CooldownRoutine(duration));
    }

    public void TurnOff()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        radialImage.fillAmount = 0f;
        radialImage.enabled = false;
        currentRoutine = null;
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        radialImage.enabled = true;
        radialImage.fillAmount = 1f;
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            radialImage.fillAmount = 1f - (elapsed / duration);

            float t, zRotation;
            if (elapsed < halfDuration)
            {
                t = elapsed / halfDuration;
                zRotation = Mathf.Lerp(0f, _maxRotationAngle, t);
            }
            else
            {
                t = (elapsed - halfDuration) / halfDuration;
                zRotation = Mathf.Lerp(_maxRotationAngle, 0f, t);
            }

            yield return null;
        }

        radialImage.fillAmount = 0f;
        currentRoutine = null;
    }

    public void PlayCooldownNoHide(float duration)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(CooldownRoutineNoHide(duration));
    }

    private IEnumerator CooldownRoutineNoHide(float duration)
    {
        radialImage.enabled = true;
        radialImage.fillAmount = 1f;
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            radialImage.fillAmount = 1f - (elapsed / duration);

            float t, zRotation;
            if (elapsed < halfDuration)
            {
                t = elapsed / halfDuration;
                zRotation = Mathf.Lerp(0f, _maxRotationAngle, t);
            }
            else
            {
                t = (elapsed - halfDuration) / halfDuration;
                zRotation = Mathf.Lerp(_maxRotationAngle, 0f, t);
            }

            yield return null;
        }

        radialImage.fillAmount = 0f;
        currentRoutine = null;
        // sin radialImage.enabled = false
    }

    public Coroutine PlayQueue(Queue<float> queue)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(QueueRoutine(queue));
        return currentRoutine;
    }

    private IEnumerator QueueRoutine(Queue<float> queue)
    {
        while (queue.Count > 0)
        {
            float duration = queue.Dequeue();
            yield return StartCoroutine(CooldownRoutine(duration));
        }
        TurnOff();
        currentRoutine = null;
    }
}