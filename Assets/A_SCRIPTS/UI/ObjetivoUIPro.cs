using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ObjetivoUIPro : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform panelObjetivo;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Posiciones")]
    [SerializeField] private Vector2 posicionFinal = new Vector2(0, 0);
    [SerializeField] private Vector2 posicionInicial = new Vector2(0, 400);
    [SerializeField] private Vector2 posicionSalida = new Vector2(0, 250);

    [Header("Tiempos")]
    [SerializeField] private float duracionEntrada = 1.0f;
    [SerializeField] private float tiempoVisible = 2.5f;
    [SerializeField] private float duracionSalida = 0.4f;

    [Header("Elasticidad Vertical")]
    [SerializeField] private float fuerzaRebote = 4.5f;
    [SerializeField] private float cantidadOscilaciones = 4.5f;

    [Header("Balanceo lateral")]
    [SerializeField] private float amplitudX = 35f;
    [SerializeField] private float frecuenciaX = 5f;
    [SerializeField] private float dampingX = 3f;

    [Header("Idle balanceo")]
    [SerializeField] private float idleAmplitudX = 5f;
    [SerializeField] private float idleFrecuenciaX = 2f;

    [Header("Rotacion opcional")]
    [SerializeField] private bool usarRotacion = true;
    [SerializeField] private float multiplicadorRotacion = 0.2f;


    private Coroutine rutinaActual;

    private void Awake()
    {
        if (panelObjetivo == null)
            panelObjetivo = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        MostrarObjetivo();
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        ResetUIInterno();
        Time.timeScale = 1f;
    }

    public void MostrarObjetivo()
    {
        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(SecuenciaObjetivo());
    }

    private IEnumerator SecuenciaObjetivo()
    {

        panelObjetivo.anchoredPosition = posicionInicial;
        panelObjetivo.localRotation = Quaternion.identity;
        canvasGroup.alpha = 0f;

        float tiempo = 0f;

        // Entrada: cae con rebote + balanceo en X
        while (tiempo < duracionEntrada)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionEntrada);

            float curvaY = ElasticEaseOut(t, fuerzaRebote, cantidadOscilaciones);
            float posY = Mathf.LerpUnclamped(posicionInicial.y, posicionFinal.y, curvaY);

            float offsetX = Mathf.Sin(t * frecuenciaX * Mathf.PI) *
                            Mathf.Exp(-dampingX * t) *
                            amplitudX;

            float posX = posicionFinal.x + offsetX;

            panelObjetivo.anchoredPosition = new Vector2(posX, posY);

            if (usarRotacion)
            {
                float rotZ = offsetX * multiplicadorRotacion;
                panelObjetivo.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        panelObjetivo.anchoredPosition = posicionFinal;
        canvasGroup.alpha = 1f;

        // Idle: mini balanceo suave
        float idleTiempo = 0f;
        while (idleTiempo < tiempoVisible)
        {
            idleTiempo += Time.unscaledDeltaTime;

            float offsetX = Mathf.Sin(idleTiempo * idleFrecuenciaX) * idleAmplitudX;
            panelObjetivo.anchoredPosition = new Vector2(posicionFinal.x + offsetX, posicionFinal.y);

            if (usarRotacion)
            {
                float rotZ = offsetX * multiplicadorRotacion;
                panelObjetivo.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }

            yield return null;
        }

        // Salida hacia arriba + fade out
        tiempo = 0f;
        Vector2 posicionInicioSalida = panelObjetivo.anchoredPosition;
        Quaternion rotacionInicioSalida = panelObjetivo.localRotation;

        while (tiempo < duracionSalida)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionSalida);

            float curva = t * t;

            panelObjetivo.anchoredPosition = Vector2.Lerp(posicionInicioSalida, posicionSalida, curva);
            panelObjetivo.localRotation = Quaternion.Lerp(rotacionInicioSalida, Quaternion.identity, curva);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, curva);

            yield return null;
        }

        rutinaActual = null;
        gameObject.SetActive(false);
    }


    private float ElasticEaseOut(float t, float amplitud, float oscilaciones)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;

        float decay = Mathf.Exp(-amplitud * t);
        float wave = Mathf.Cos(oscilaciones * Mathf.PI * t);

        return 1f - (decay * wave);
    }

    private void ResetUIInterno()
    {
        if (panelObjetivo != null)
        {
            panelObjetivo.anchoredPosition = posicionInicial;
            panelObjetivo.localRotation = Quaternion.identity;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}