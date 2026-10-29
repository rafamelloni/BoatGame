using System;
using System.Collections.Generic;
using UnityEngine;

// Las reliquias que tenés en la run.
// Cada templo completado da UNA reliquia entera, elegida por rareza:
//   1) se tira la rareza según los pesos (Common / Rare / Super Rare)
//   2) se elige una reliquia al azar de esa rareza entre las que todavía podés recibir
// Las Common se pueden repetir y se apilan (dos Pólvora fina = +20%).
// Las Rare y Super Rare salen una sola vez por run.
// Ponelo en la escena y cargá el array con Tools > Botes > Cargar reliquias en el inventario.
public class RelicInventory : MonoBehaviour
{
    public static RelicInventory Instance { get; private set; }

    [SerializeField] private SO_RelicData[] _relics;

    [Header("Probabilidad de cada rareza (pesos, no hace falta que sumen 100)")]
    [SerializeField] private float _commonWeight = 65f;
    [SerializeField] private float _rareWeight = 28f;
    [SerializeField] private float _superRareWeight = 7f;

    [Header("Mala racha")]
    [Tooltip("Después de esta cantidad de Common seguidas, la próxima es Rare o mejor (0 = desactivado)")]
    [SerializeField] private int _pityAfterCommons = 3;

    [Header("Repetidas")]
    [Tooltip("Máximo de copias de una misma Common (0 = sin límite)")]
    [SerializeField] private int _maxCommonStacks = 5;

    [Header("Debug")]
    [Tooltip("F6: abre el panel del templo. F7: te da una reliquia al azar sin panel.")]
    [SerializeField] private bool _debugKeys = true;

    private readonly Dictionary<SO_RelicData, int> _counts = new Dictionary<SO_RelicData, int>();
    private readonly List<SO_RelicData> _owned = new List<SO_RelicData>(); // en el orden en que las conseguiste
    private int _commonStreak;

    // (reliquia, cuántas copias tenés ahora)
    public event Action<SO_RelicData, int> OnRelicAdded;
    // se reinició la run: se pierden las reliquias y sus mejoras
    public event Action OnReset;

    public SO_RelicData[] Relics => _relics;
    public IReadOnlyList<SO_RelicData> Owned => _owned;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        // los efectos de las reliquias viven en RelicEffects (mismo objeto)
        if (GetComponent<RelicEffects>() == null) gameObject.AddComponent<RelicEffects>();

#if UNITY_EDITOR
        if (!HasAnyValid())
        {
            _relics = LoadAllFromProject();
            Debug.LogWarning("[RelicInventory] El array de reliquias no tenía reliquias nuevas; las cargué del proyecto solo para este Play. " +
                             "Para que quede guardado (y funcione en el build) usá Tools > Botes > Cargar reliquias en el inventario.");
        }
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!_debugKeys) return;
        if (Input.GetKeyDown(KeyCode.F6)) ShrineRewardUI.ShowFor(null);
        if (Input.GetKeyDown(KeyCode.F7)) TryGiveRandom(out _, out _);
    }

    // Llamarlo al reiniciar la run (lo hace SceneResetter): saca las reliquias y deshace las mejoras
    public void ResetAll()
    {
        _counts.Clear();
        _owned.Clear();
        _commonStreak = 0;
        OnReset?.Invoke();
    }

    public int GetCount(SO_RelicData relic) =>
        relic != null && _counts.TryGetValue(relic, out int c) ? c : 0;

    public bool CanReceive(SO_RelicData relic)
    {
        if (relic == null || RelicEffects.IsLegacy(relic.effectType)) return false;
        int count = GetCount(relic);
        if (relic.rarity == RelicRarity.Common)
            return _maxCommonStacks <= 0 || count < _maxCommonStacks;
        return count == 0;
    }

    public bool HasAvailable()
    {
        if (_relics == null) return false;
        foreach (var r in _relics) if (CanReceive(r)) return true;
        return false;
    }

    // Lo que usa el templo: tira la rareza y da una reliquia.
    public bool TryGiveRandom(out SO_RelicData relic, out int count)
    {
        relic = null;
        count = 0;
        if (_relics == null || _relics.Length == 0) return false;

        if (!RollRarity(out RelicRarity rarity)) return false;

        // elegir al azar entre las de esa rareza que todavía podés recibir
        int candidates = 0;
        foreach (var r in _relics)
        {
            if (r == null || r.rarity != rarity || !CanReceive(r)) continue;
            candidates++;
            if (UnityEngine.Random.Range(0, candidates) == 0) relic = r;
        }
        if (relic == null) return false;

        count = Give(relic);
        return true;
    }

    // Da una reliquia puntual (también sirve para debug). Devuelve cuántas copias tenés.
    public int Give(SO_RelicData relic)
    {
        if (relic == null) return 0;
        int count = GetCount(relic) + 1;
        _counts[relic] = count;
        if (count == 1) _owned.Add(relic);
        _commonStreak = relic.rarity == RelicRarity.Common ? _commonStreak + 1 : 0;
        OnRelicAdded?.Invoke(relic, count);
        return count;
    }

    private bool RollRarity(out RelicRarity rarity)
    {
        float wc = AnyOf(RelicRarity.Common) ? _commonWeight : 0f;
        float wr = AnyOf(RelicRarity.Rare) ? _rareWeight : 0f;
        float ws = AnyOf(RelicRarity.SuperRare) ? _superRareWeight : 0f;

        // mala racha: si venís de varias Common seguidas, la próxima no puede ser Common
        if (_pityAfterCommons > 0 && _commonStreak >= _pityAfterCommons && wr + ws > 0f)
            wc = 0f;

        float total = wc + wr + ws;
        rarity = RelicRarity.Common;
        if (total <= 0f) return false;

        float roll = UnityEngine.Random.value * total;
        if (roll < wc) rarity = RelicRarity.Common;
        else if (roll < wc + wr) rarity = RelicRarity.Rare;
        else rarity = RelicRarity.SuperRare;
        return true;
    }

    private bool AnyOf(RelicRarity rarity)
    {
        foreach (var r in _relics)
            if (r != null && r.rarity == rarity && CanReceive(r)) return true;
        return false;
    }

    private bool HasAnyValid()
    {
        if (_relics == null) return false;
        foreach (var r in _relics)
            if (r != null && !RelicEffects.IsLegacy(r.effectType)) return true;
        return false;
    }

    // ---- compatibilidad con el sistema viejo de fragmentos (por si algún script lo usa)
    [Obsolete("Las reliquias ya no vienen por fragmentos")] public const int FragmentsToComplete = 1;
    [Obsolete("Usá TryGiveRandom")]
    public bool TryAddRandomFragment(out SO_RelicData relic, out int count) => TryGiveRandom(out relic, out count);

#if UNITY_EDITOR
    public const string RelicsFolder = "Assets/A_DATA/Relics";

    public static SO_RelicData[] LoadAllFromProject()
    {
        var list = new List<SO_RelicData>();
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:SO_RelicData", new[] { RelicsFolder }))
        {
            var r = UnityEditor.AssetDatabase.LoadAssetAtPath<SO_RelicData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (r != null && !RelicEffects.IsLegacy(r.effectType)) list.Add(r);
        }
        list.Sort((a, b) => a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : a.effectType.CompareTo(b.effectType));
        return list.ToArray();
    }

    public void EditorSetRelics(SO_RelicData[] relics) => _relics = relics;
#endif
}
