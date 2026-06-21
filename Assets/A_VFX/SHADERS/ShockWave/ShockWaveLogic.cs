using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShockwaveController : MonoBehaviour
{
    [Header("Renderer Feature apagado/prendido")]
    [SerializeField] private ScriptableRendererFeature shockwaveFeature;

    [Header("Material del Full Screen Pass")]
    [SerializeField] private Material shockwaveMaterial;

    [Header("Debug")]
    [SerializeField] private KeyCode debugKey = KeyCode.Space;

    [Header("Shockwave")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private float magnitude = 0.03f;
    [SerializeField] private float size = 0.04f;

    private Coroutine shockwaveCoroutine;

    private void Awake()
    {
        SetShockwaveFeature(false);
        TurnOffMaterial();
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            PlayShockwave();
        }
    }

    public void PlayShockwave()
    {
        if (shockwaveCoroutine != null)
            StopCoroutine(shockwaveCoroutine);

        shockwaveCoroutine = StartCoroutine(ShockwaveRoutine());
    }

    private IEnumerator ShockwaveRoutine()
    {
        // 1. PRENDER EL FULL SCREEN PASS
        SetShockwaveFeature(true);

        // 2. RESETEAR MATERIAL
        shockwaveMaterial.SetFloat("_Progress", 0f);
        shockwaveMaterial.SetFloat("_Magnitude", magnitude);
        shockwaveMaterial.SetFloat("_Size", size);

        float timer = 0f;

        // 3. EJECUTAR SHOCKWAVE UNA SOLA VEZ
        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            shockwaveMaterial.SetFloat("_Progress", progress);

            yield return null;
        }

        // 4. APAGAR TODO
        TurnOffMaterial();
        SetShockwaveFeature(false);

        shockwaveCoroutine = null;
    }

    private void TurnOffMaterial()
    {
        if (shockwaveMaterial == null) return;

        shockwaveMaterial.SetFloat("_Progress", 1f);
        shockwaveMaterial.SetFloat("_Magnitude", 0f);
        shockwaveMaterial.SetFloat("_Size", 0f);
    }

    private void SetShockwaveFeature(bool enabled)
    {
        if (shockwaveFeature == null) return;

        shockwaveFeature.SetActive(enabled);
    }
}