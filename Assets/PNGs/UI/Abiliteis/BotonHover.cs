using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BotonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform target;

    [Header("Escala")]
    [SerializeField] private float escalaHover = 1.12f;
    [SerializeField] private float duracion = 0.08f;
    [SerializeField] private float bounce = 1.04f;

    private Vector3 escalaOriginal;
    private Coroutine rutinaActual;

    void Awake()
    {
        if (target == null)
            target = transform;

        escalaOriginal = target.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * escalaHover * bounce,
            escalaOriginal * escalaHover
        ));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(AnimarEscala(
            escalaOriginal * 0.96f,
            escalaOriginal
        ));
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
}