using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private ParticleSystem hitParticles;

    private MaterialPropertyBlock _mpb;
    private Coroutine _flashCoroutine;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        // excluir ParticleSystemRenderer
        renderers = System.Array.FindAll(renderers, r => r is not ParticleSystemRenderer);
    }

    private void OnDisable()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
        SetEmission(Color.black);
    }

    public void Flash()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
        hitParticles?.Play();
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float t = 1f - (elapsed / flashDuration);
            SetEmission(flashColor * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetEmission(Color.black);
        _flashCoroutine = null;
    }

    private void SetEmission(Color color)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColor, color);
            r.SetPropertyBlock(_mpb);
        }
    }
}