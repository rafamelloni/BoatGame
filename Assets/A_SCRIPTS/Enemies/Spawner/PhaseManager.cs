using System;
using UnityEngine;

public enum TumultoSpawnType { Group, Ship, Rafa }

[Serializable]
public struct SpawnPhase
{
    public string name;
    public float startAtSecond;

    [Header("Groups")]
    public float groupSpawnInterval;
    public int maxActiveGroups;
    public float groupHealth;

    [Header("Ships")]
    public float shipSpawnInterval;
    public int maxActiveShips;
    public float shipHealth;

    [Header("Rafas")]
    public float rafaSpawnInterval;
    public int maxActiveRafas;
    public float rafaHealth;

    [Header("Tumulto")]
    [Tooltip("Cuántos grupos tienen que estar juntos para considerarse tumulto.")]
    public int tumultoMinGroups;
    [Tooltip("Cuánto espera antes de volver a chequear si hay tumulto.")]
    public float tumultoCheckCooldown;
    [Tooltip("Cuántos segundos dura spawneando cuando detecta tumulto.")]
    public float tumultoActiveDuration;
    [Tooltip("Cada cuántos segundos spawnea un enemigo durante el evento de tumulto.")]
    public float tumultoSpawnInterval;
    [Tooltip("Qué tipo de enemigo spawnea durante el tumulto.")]
    public TumultoSpawnType tumultoSpawnType;

    [Header("Coins")]
    public int coinsToFill;
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