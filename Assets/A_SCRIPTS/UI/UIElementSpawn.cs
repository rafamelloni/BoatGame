using System.Collections;
using UnityEngine;

public class UIElementSpawn : MonoBehaviour
{
    [Header("Elementos UI ya ubicados")]
    [SerializeField] private GameObject[] elementosUI;

    [Header("Spawn Points de partículas")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Tiempos")]
    [SerializeField] private float delayInicial = 1f;
    [SerializeField] private float tiempoEntreActivaciones = 0.35f;

    [Header("Partícula")]
    [SerializeField] private ParticleSystem particlePrefab;
    [SerializeField] private float escalaParticula = 0.02f;

    [Header("Efecto Pop Up")]
    [SerializeField] private float duracionPop = 0.25f;
    [SerializeField] private float escalaInicial = 0.15f;
    [SerializeField] private float escalaOvershoot = 1.18f;

    [Header("Shake")]
    [SerializeField] private float intensidadShake = 8f;
    [SerializeField] private float duracionShake = 0.18f;

    private bool secuenciaIniciada;

    private void Start()
    {
        foreach (GameObject elemento in elementosUI)
        {
            if (elemento != null)
                elemento.SetActive(false);
        }

        IniciarSecuencia();
    }

    public void IniciarSecuencia()
    {
        if (secuenciaIniciada) return;

        secuenciaIniciada = true;
        StartCoroutine(ActivarElementos());
    }

    private IEnumerator ActivarElementos()
    {
        yield return new WaitForSeconds(delayInicial);

        for (int i = 0; i < elementosUI.Length; i++)
        {
            if (elementosUI[i] == null) continue;

            GameObject elemento = elementosUI[i];
            elemento.SetActive(true);

            RectTransform rect = elemento.GetComponent<RectTransform>();

            if (rect != null)
            {
                StartCoroutine(PopUI(rect));
                StartCoroutine(ShakeUI(rect));
            }

            CrearParticula(i);

            yield return new WaitForSeconds(tiempoEntreActivaciones);
        }
    }

    private void CrearParticula(int index)
    {
        if (particlePrefab == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (index >= spawnPoints.Length) return;
        if (spawnPoints[index] == null) return;

        Transform spawnPoint = spawnPoints[index];

        ParticleSystem ps = Instantiate(
            particlePrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        ps.transform.localScale = Vector3.one * escalaParticula;
        ps.Play();

        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    private IEnumerator PopUI(RectTransform rect)
    {
        Vector3 escalaOriginal = rect.localScale;

        Vector3 escalaInicio = escalaOriginal * escalaInicial;
        Vector3 escalaGrande = escalaOriginal * escalaOvershoot;
        Vector3 escalaFinal = escalaOriginal;

        rect.localScale = escalaInicio;

        float t = 0f;

        while (t < duracionPop)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duracionPop);

            if (progress < 0.7f)
            {
                float p = progress / 0.7f;

                rect.localScale = Vector3.Lerp(
                    escalaInicio,
                    escalaGrande,
                    EaseOutBack(p)
                );
            }
            else
            {
                float p = (progress - 0.7f) / 0.3f;

                rect.localScale = Vector3.Lerp(
                    escalaGrande,
                    escalaFinal,
                    EaseOutQuad(p)
                );
            }

            yield return null;
        }

        rect.localScale = escalaFinal;
    }

    private IEnumerator ShakeUI(RectTransform rect)
    {
        Vector2 posicionOriginal = rect.anchoredPosition;
        float t = 0f;

        while (t < duracionShake)
        {
            t += Time.deltaTime;

            float fuerza = 1f - Mathf.Clamp01(t / duracionShake);
            Vector2 offset = Random.insideUnitCircle * intensidadShake * fuerza;

            rect.anchoredPosition = posicionOriginal + offset;

            yield return null;
        }

        rect.anchoredPosition = posicionOriginal;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseOutQuad(float x)
    {
        return 1f - (1f - x) * (1f - x);
    }
}