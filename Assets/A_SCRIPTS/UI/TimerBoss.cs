using UnityEngine;
using TMPro;

public class TimerBoss : MonoBehaviour
{
    [Header("Tiempo")]
    [SerializeField] private float tiempoInicial = 180f;

    [Header("UI")]
    [SerializeField] private TMP_Text textoTimer;

    [Tooltip("RectTransform de la barra naranja (Image Type = Sliced)")]
    [SerializeField] private RectTransform barraTimer;

    [SerializeField] private float anchoMaximo = 1000f;

    [Header("Objeto a activar al llegar a 0")]
    [SerializeField] private GameObject objetoAlTerminar;
    [SerializeField] private EnemySpawner enemySpawner;

    private float tiempoActual;
    private bool timerActivo = true;

    private void Start()
    {
        tiempoActual = tiempoInicial;

        if (objetoAlTerminar != null)
            objetoAlTerminar.SetActive(false);

        ActualizarUI();
    }

    private void Update()
    {
        if (!timerActivo)
            return;

        tiempoActual -= Time.deltaTime;

        if (tiempoActual <= 0f)
        {
            tiempoActual = 0f;
            timerActivo = false;

            if (objetoAlTerminar != null)
                objetoAlTerminar.SetActive(true);

            if (enemySpawner != null)
                enemySpawner.DespawnAll();
        }

        ActualizarUI();
    }

    private void ActualizarUI()
    {
        float porcentaje = tiempoActual / tiempoInicial;

        int minutos = Mathf.FloorToInt(tiempoActual / 60f);
        int segundos = Mathf.FloorToInt(tiempoActual % 60f);

        if (textoTimer != null)
            textoTimer.text = $"{minutos:00}:{segundos:00}";

        if (barraTimer != null)
        {
            barraTimer.sizeDelta = new Vector2(
                anchoMaximo * porcentaje,
                barraTimer.sizeDelta.y
            );
        }
    }

    public void ResetTimer()
    {
        tiempoActual = tiempoInicial;
        timerActivo = true;

        if (objetoAlTerminar != null)
            objetoAlTerminar.SetActive(false);

        ActualizarUI();
    }
}