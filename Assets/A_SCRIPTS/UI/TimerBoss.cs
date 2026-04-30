using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerBoss : MonoBehaviour
{
    [Header("Tiempo")]
    [SerializeField] private float tiempoInicial = 180f; // 3 minutos

    [Header("UI")]
    [SerializeField] private TMP_Text textoTimer;
    [SerializeField] private Image barraTimer;

    [Header("Objeto a activar al llegar a 0")]
    [SerializeField] private GameObject objetoAlTerminar;

    private float tiempoActual;
    private bool timerActivo = true;

    private void Start()
    {
        tiempoActual = tiempoInicial;

        if (objetoAlTerminar != null)
            objetoAlTerminar.SetActive(false);

        ActualizarTexto();
    }

    private void Update()
    {
        if (!timerActivo) return;

        tiempoActual -= Time.deltaTime;

        if (tiempoActual <= 0f)
        {
            tiempoActual = 0f;
            timerActivo = false;

            ActualizarTexto();

            if (objetoAlTerminar != null)
                objetoAlTerminar.SetActive(true);

            return;
        }

        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        int minutos = Mathf.FloorToInt(tiempoActual / 60f);
        int segundos = Mathf.FloorToInt(tiempoActual % 60f);
        textoTimer.text = minutos.ToString("00") + ":" + segundos.ToString("00");

        if (barraTimer != null)
            barraTimer.fillAmount = tiempoActual / tiempoInicial;
    }

    public void ResetTimer()
    
    {
        tiempoActual = tiempoInicial;


    }

}