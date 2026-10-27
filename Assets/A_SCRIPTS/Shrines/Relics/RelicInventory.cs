using System;
using UnityEngine;

// Lleva la cuenta de los fragmentos de cada reliquia.
// Ponelo en la escena y cargá el array con tus SO_RelicData.
// Cada templo completado da un fragmento de una reliquia al azar entre las que todavía no completaste.
public class RelicInventory : MonoBehaviour
{
    public const int FragmentsToComplete = 3;

    public static RelicInventory Instance { get; private set; }

    [SerializeField] private SO_RelicData[] _relics;

    private int[] _counts;

    // (reliquia, fragmentos que tenés ahora)
    public event Action<SO_RelicData, int> OnFragmentAdded;
    // se completó la reliquia (acá va a ir "lo importante")
    public event Action<SO_RelicData> OnRelicCompleted;
    // se reinició la run: se pierden fragmentos y mejoras
    public event Action OnReset;

    public SO_RelicData[] Relics => _relics;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _counts = new int[_relics != null ? _relics.Length : 0];
        // las mejoras de las reliquias viven en RelicEffects (mismo objeto)
        if (GetComponent<RelicEffects>() == null) gameObject.AddComponent<RelicEffects>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Llamarlo al reiniciar la run (lo hace SceneResetter): saca fragmentos y deshace las mejoras
    public void ResetAll()
    {
        _counts = new int[_relics != null ? _relics.Length : 0];
        OnReset?.Invoke();
    }

    public int GetCount(SO_RelicData relic)
    {
        int i = Array.IndexOf(_relics, relic);
        return i >= 0 ? _counts[i] : 0;
    }

    public bool HasIncomplete()
    {
        if (_relics == null) return false;
        for (int i = 0; i < _relics.Length; i++)
            if (_relics[i] != null && _counts[i] < FragmentsToComplete) return true;
        return false;
    }

    // Da un fragmento de una reliquia al azar entre las no completadas.
    public bool TryAddRandomFragment(out SO_RelicData relic, out int count)
    {
        relic = null;
        count = 0;
        if (_relics == null || _relics.Length == 0) return false;
        if (_counts == null || _counts.Length != _relics.Length) ResetAll();

        // elegir al azar entre las incompletas (sin armar listas)
        int candidates = 0, chosen = -1;
        for (int i = 0; i < _relics.Length; i++)
        {
            if (_relics[i] == null || _counts[i] >= FragmentsToComplete) continue;
            candidates++;
            if (UnityEngine.Random.Range(0, candidates) == 0) chosen = i;
        }
        if (chosen < 0) return false;

        _counts[chosen]++;
        relic = _relics[chosen];
        count = _counts[chosen];

        OnFragmentAdded?.Invoke(relic, count);
        if (count >= FragmentsToComplete) OnRelicCompleted?.Invoke(relic);
        return true;
    }
}
