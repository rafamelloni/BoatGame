using System;
using UnityEngine;

public enum TumultoSpawnType { Group, Ship, Rafa }

[Serializable]
public struct SpawnPhase
{
    [Tooltip("Nombre descriptivo de la fase, solo para identificarla en el inspector.")]
    public string name;

    [Tooltip("Segundo de sesión en el que arranca esta fase.")]
    public float startAtSecond;

    [Space(6)]
    [Header("——— Grupos ———")]
    [Tooltip("Cada cuántos segundos intenta spawnear un nuevo grupo.")]
    public float groupSpawnInterval;

    [Tooltip("Índice del prefab de grupo a spawnear. -1 = aleatorio.")]
    public int groupPrefabIndex;

    [Tooltip("Máximo de grupos activos en pantalla al mismo tiempo.")]
    public int maxActiveGroups;

    [Tooltip("Health de cada miembro del grupo. 0 = usar valor del prefab.")]
    public float groupHealth;

    [Space(6)]
    [Header("——— Ships ———")]
    [Tooltip("Cada cuántos segundos intenta spawnear un nuevo ship.")]
    public float shipSpawnInterval;

    [Tooltip("Máximo de ships activos en pantalla al mismo tiempo.")]
    public int maxActiveShips;

    [Tooltip("Health del ship. 0 = usar valor del prefab.")]
    public float shipHealth;

    [Space(6)]
    [Header("——— Rafas ———")]
    [Tooltip("Cada cuántos segundos intenta spawnear un nuevo Rafa.")]
    public float rafaSpawnInterval;

    [Tooltip("Máximo de Rafas activos en pantalla al mismo tiempo.")]
    public int maxActiveRafas;

    [Tooltip("Health del Rafa. 0 = usar valor del prefab.")]
    public float rafaHealth;

    [Space(6)]
    [Header("——— Tumulto ———")]
    [Tooltip("Mínimo de grupos juntos para considerar que hay tumulto. 0 = usar valor del EnemySpawner.")]
    public int tumultoMinGroups;

    [Tooltip("Segundos que espera antes de volver a chequear si hay tumulto, después de que terminó un evento.")]
    public float tumultoCheckCooldown;

    [Tooltip("Cuántos segundos dura el evento de tumulto spawneando enemigos.")]
    public float tumultoActiveDuration;

    [Tooltip("Cada cuántos segundos spawnea un enemigo durante el evento de tumulto.")]
    public float tumultoSpawnInterval;

    [Tooltip("Tipo de enemigo que spawnea durante el tumulto.")]
    public TumultoSpawnType tumultoSpawnType;

    [Space(6)]
    [Header("——— Monedas ———")]
    [Tooltip("Cuántas monedas necesita el jugador para llenar la barra en esta fase. 0 = no modificar.")]
    public int coinsToFill;

    [Tooltip("Cuántas monedas dropea cada enemigo al morir. 0 = usar valor del inspector.")]
    public int coinsPerKill;
}

public class PhaseManager : MonoBehaviour
{
    [SerializeField] private SpawnPhase[] _phases;

    public event Action<SpawnPhase> OnPhaseChanged;

    public SpawnPhase CurrentPhase => _phases[_currentPhaseIndex];

    private int _currentPhaseIndex = 0;
    private float _sessionTime = 0f;
    private bool _isRunning = false;

    public void StartSession()
    {
        _currentPhaseIndex = 0;
        _sessionTime = 0f;
        _isRunning = true;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    public void StopSession()
    {
        _isRunning = false;
    }

    private void Update()
    {
        if (!_isRunning) return;
        if (_phases == null || _phases.Length == 0) return;

        _sessionTime += Time.deltaTime;

        int next = _currentPhaseIndex + 1;
        if (next < _phases.Length && _sessionTime >= _phases[next].startAtSecond)
        {
            _currentPhaseIndex = next;
            Debug.Log($"[PhaseManager] Fase {_currentPhaseIndex}: {CurrentPhase.name}");
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}