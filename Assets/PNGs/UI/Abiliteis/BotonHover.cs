using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BotonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform target;

    [Header("Escala")]
    [SerializeField] private float escalaHover = 1.12f;
    [SerializeField] private float duracion = 0.08f;
    [SerializeField] private float bounce = 1.04f;

    [Header("Movimiento")]
    [SerializeField] private float offsetY = 30f;

    [Header("Nombre fijo")]
    [SerializeField] private TextMeshProUGUI nombreHabilidad;
    [SerializeField] private string nombre;

    [Header("Descripcion (fade)")]
    [SerializeField] private TextMeshProUGUI descripcionText;

    [TextArea(2, 4)]
    [SerializeField] private string descripcion;

    private Vector3 escalaOriginal;
    private Vector3 posicionOriginal;

    private Coroutine rutinaActual;
    private Coroutine rutinaMovimiento;
    private Coroutine rutinaDescripcion;

    private void Awake()
    {
        if (target == null)
            target = transform;

        escalaOriginal = target.localScale;
        posicionOriginal = target.localPosition;

        // Nombre siempre visible
        if (nombreHabilidad != null)
            nombreHabilidad.text = nombre;

        // Descripción invisible al inicio
        if (descripcionText != null)
        {
            descripcionText.text = descripcion;

            Color c = descripcionText.color;
            c.a = 0f;
            descripcionText.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        target.SetAsLastSibling();

        if (rutinaActual != null) StopCoroutine(rutinaActual);
        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);
        if (rutinaDescripcion != null) StopCoroutine(rutinaDescripcion);

        // Escala con bounce
        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * escalaHover * bounce,
            escalaOriginal * escalaHover
        ));

        // Movimiento hacia arriba
        rutinaMovimiento = StartCoroutine(AnimarMovimiento(
            posicionOriginal + new Vector3(0, offsetY, 0)
        ));

        // Mostrar descripción
        rutinaDescripcion = StartCoroutine(FadeTexto(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);
        if (rutinaDescripcion != null) StopCoroutine(rutinaDescripcion);

        // Volver a normal
        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * 0.96f,
            escalaOriginal
        ));

        rutinaMovimiento = StartCoroutine(AnimarMovimiento(posicionOriginal));

        // Ocultar descripción
        rutinaDescripcion = StartCoroutine(FadeTexto(0f));
    }

    private IEnumerator AnimarEscala(Vector3 intermedia, Vector3 final)
    {
        Vector3 inicio = target.localScale;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duracion;
            target.localScale = Vector3.Lerp(inicio, intermedia, p);
            yield return null;
        }

        t = 0f;
        inicio = target.localScale;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duracion;
            target.localScale = Vector3.Lerp(inicio, final, p);
            yield return null;
        }

        target.localScale = final;
    }

    private IEnumerator AnimarMovimiento(Vector3 destino)
    {
        Vector3 inicio = target.localPosition;
        float t = 0f;
        float tiempo = duracion * 2f;

        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            float p = t / tiempo;
            target.localPosition = Vector3.Lerp(inicio, destino, p);
            yield return null;
        }

        target.localPosition = destino;
    }

    private IEnumerator FadeTexto(float alphaFinal)
    {
        if (descripcionText == null)
            yield break;

        float t = 0f;
        float tiempo = duracion * 2f;

        Color colorInicial = descripcionText.color;
        float alphaInicial = colorInicial.a;

        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            float p = t / tiempo;

            float alpha = Mathf.Lerp(alphaInicial, alphaFinal, p);

            Color c = descripcionText.color;
            c.a = alpha;
            descripcionText.color = c;

            yield return null;
        }

        Color final = descripcionText.color;
        final.a = alphaFinal;
        descripcionText.color = final;
    }
}