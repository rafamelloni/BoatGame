using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BotonHoverSimple : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform target;

    [Header("Escala")]
    [SerializeField] private float escalaHover = 1.12f;
    [SerializeField] private float duracion = 0.08f;
    [SerializeField] private float bounce = 1.04f;

    [Header("Movimiento")]
    [SerializeField] private float offsetY = 30f;

    private Vector3 escalaOriginal;
    private Vector3 posicionOriginal;

    private Coroutine rutinaActual;
    private Coroutine rutinaMovimiento;

    private void Awake()
    {
        if (target == null)
            target = transform;

        escalaOriginal = target.localScale;
        posicionOriginal = target.localPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
       // target.SetAsLastSibling();

        if (rutinaActual != null) StopCoroutine(rutinaActual);
        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);

        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * escalaHover * bounce,
            escalaOriginal * escalaHover
        ));

        rutinaMovimiento = StartCoroutine(AnimarMovimiento(
            posicionOriginal + new Vector3(0, offsetY, 0)
        ));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);

        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * 0.96f,
            escalaOriginal
        ));

        rutinaMovimiento = StartCoroutine(AnimarMovimiento(posicionOriginal));
    }

    private IEnumerator AnimarEscala(Vector3 intermedia, Vector3 final)
    {
        Vector3 inicio = target.localScale;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(inicio, intermedia, t / duracion);
            yield return null;
        }

        t = 0f;
        inicio = target.localScale;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(inicio, final, t / duracion);
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
            target.localPosition = Vector3.Lerp(inicio, destino, t / tiempo);
            yield return null;
        }

        target.localPosition = destino;
    }
}