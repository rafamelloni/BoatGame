using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Timing")]
    public float beatDuration = 1f;

    [Header("Scale")]
    public float baseScale = 1f;
    public float firstPeak = 1.18f;
    public float secondPeak = 1.1f;

    [Header("Beat Shape")]
    [Range(0f, 1f)] public float firstPeakTime = 0.15f;
    [Range(0f, 1f)] public float returnTime = 0.3f;
    [Range(0f, 1f)] public float secondPeakTime = 0.45f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        float t = (timer % beatDuration) / beatDuration;

        float scale = EvaluateBeat(t);
        transform.localScale = Vector3.one * scale;
    }

    float EvaluateBeat(float t)
    {
        if (t < firstPeakTime)
            return Mathf.Lerp(baseScale, firstPeak, EaseInOut(t / firstPeakTime));

        if (t < returnTime)
            return Mathf.Lerp(firstPeak, baseScale, EaseInOut((t - firstPeakTime) / (returnTime - firstPeakTime)));

        if (t < secondPeakTime)
            return Mathf.Lerp(baseScale, secondPeak, EaseInOut((t - returnTime) / (secondPeakTime - returnTime)));

        return Mathf.Lerp(secondPeak, baseScale, EaseInOut((t - secondPeakTime) / (1f - secondPeakTime)));
    }

    float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}