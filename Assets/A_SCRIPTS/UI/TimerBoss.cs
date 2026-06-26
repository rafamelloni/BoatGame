using UnityEngine;
using TMPro;

public class TimerBoss : MonoBehaviour
{
    [Header("Tiempo")]
    [SerializeField] private float tiempoTotal = 900f;

    [Header("Boss Timestamps")]
    [SerializeField] private float[] bossTimestamps;

    [Header("UI")]
    [SerializeField] private TMP_Text textoTimer;
    [SerializeField] private RectTransform barraTimer;
    [SerializeField] private float anchoMaximo = 1000f;

    [Header("Referencias")]
    [SerializeField] private BossSequenceManager _bossSequenceManager;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private IslandSpawnManager _islandSpawner;

    private float tiempoActual;
    private bool timerActivo = false;
    private int _nextBossIndex = 0;

    private void Start()
    {
        tiempoActual = tiempoTotal;
        //timerActivo = false;
        ActualizarUI();
    }

    private void Update()
    {

        if (!timerActivo) return;

        tiempoActual -= Time.deltaTime;

        if (_nextBossIndex < bossTimestamps.Length)
        {
            float tiempoRestanteParaBoss = tiempoTotal - bossTimestamps[_nextBossIndex];
            if (tiempoActual <= tiempoRestanteParaBoss)
            {
                timerActivo = false;
                _enemySpawner?.DespawnAll();
                _islandSpawner?.ResetIslands();
                _bossSequenceManager?.ActivateNextBoss();
                _nextBossIndex++;
            }
        }

        if (tiempoActual <= 0f)
            tiempoActual = 0f;

        ActualizarUI();
    }

    private void ActualizarUI()
    {
        float porcentaje = 1f - (tiempoActual / tiempoTotal); int minutos = Mathf.FloorToInt(tiempoActual / 60f);
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

    public void StartTimer()
    {
        timerActivo = true;
    }

    public void ResumeTimer()
    {
        timerActivo = true;
    }

    public void ResetTimer()
    {
        tiempoActual = tiempoTotal;
        timerActivo = false;
        _nextBossIndex = 0;
        ActualizarUI();
    }
}