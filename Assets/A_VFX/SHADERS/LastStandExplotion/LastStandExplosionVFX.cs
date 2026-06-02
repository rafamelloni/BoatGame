using System.Collections;
using UnityEngine;

public class LastStandExplosionVFX : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Renderer sphereRenderer;

    [Header("Escala")]
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float endScale = 45f;

    [Header("Tiempos")]
    [SerializeField] private float expandDuration = 0.45f;
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Shader")]
    [SerializeField] private float startAlpha = 0.7f;
    [SerializeField] private float startInnerAlpha = 0.25f;
    [SerializeField] private float startEmission = 10f;
    [SerializeField] private float startInnerStrength = 2f;

    private Material mat;

    private void Awake()
    {
        if (sphereRenderer == null)
            sphereRenderer = GetComponent<Renderer>();

        mat = sphereRenderer.material;
    }

    private void OnEnable()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        SetShaderValues(startAlpha, startInnerAlpha, startEmission, startInnerStrength);

        transform.localScale = Vector3.one * startScale;

        float time = 0f;

        while (time < expandDuration)
        {
            time += Time.deltaTime;
            float t = time / expandDuration;

            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            float scale = Mathf.Lerp(startScale, endScale, smoothT);

            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        transform.localScale = Vector3.one * endScale;

        time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            float innerAlpha = Mathf.Lerp(startInnerAlpha, 0f, t);
            float emission = Mathf.Lerp(startEmission, 0f, t);
            float innerStrength = Mathf.Lerp(startInnerStrength, 0f, t);

            SetShaderValues(alpha, innerAlpha, emission, innerStrength);

            yield return null;
        }

        SetShaderValues(0f, 0f, 0f, 0f);
        Destroy(gameObject);
    }

    private void SetShaderValues(
        float alpha,
        float innerAlpha,
        float emission,
        float innerStrength)
    {
        mat.SetFloat("_alpha", alpha);
        mat.SetFloat("_InnerAlpha", innerAlpha);
        mat.SetFloat("_EmissionStrength", emission);
        mat.SetFloat("_InnerStrength", innerStrength);
    }
}