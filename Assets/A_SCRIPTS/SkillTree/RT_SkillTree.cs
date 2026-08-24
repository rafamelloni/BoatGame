using System;
using System.Collections.Generic;
using UnityEngine;

// Estado en runtime del arbol de mejoras del canon (progreso de la run actual).
// Solo maneja niveles/desbloqueos: que hace cada nodo en el juego se conecta
// despues, desde afuera (UpgradeSystem-style), escuchando estos eventos.
public class RT_SkillTree : MonoBehaviour
{
    public event Action<SO_NormalNode, int> OnNormalNodeLeveled;
    public event Action<SO_SpecialNode> OnSpecialNodeUnlocked;
    public event Action<int> OnCurrencyChanged;

    private readonly Dictionary<SO_NormalNode, int> _normalLevels = new();
    private readonly HashSet<SO_SpecialNode> _unlockedSpecials = new();
    private int _currency;

    public int Currency => _currency;

    public int GetLevel(SO_NormalNode node) =>
        node != null && _normalLevels.TryGetValue(node, out int lvl) ? lvl : 0;

    public bool CanLevelUp(SO_NormalNode node) =>
        node != null && GetLevel(node) < node.maxLevel;

    public bool CanAfford(SO_NormalNode node) =>
        node != null && _currency >= node.cost;

    // Sube nivel sin cobrar plata. Lo sigue usando SkillTreeDebug (atajo por teclado).
    public bool LevelUp(SO_NormalNode node)
    {
        if (!CanLevelUp(node)) return false;

        int newLevel = GetLevel(node) + 1;
        _normalLevels[node] = newLevel;
        OnNormalNodeLeveled?.Invoke(node, newLevel);
        return true;
    }

    // Version "real": cobra el costo del nodo. La usa la UI con botones.
    public bool TryLevelUp(SO_NormalNode node)
    {
        if (!CanLevelUp(node)) return false;
        if (!CanAfford(node)) return false;

        _currency -= node.cost;
        OnCurrencyChanged?.Invoke(_currency);

        int newLevel = GetLevel(node) + 1;
        _normalLevels[node] = newLevel;
        OnNormalNodeLeveled?.Invoke(node, newLevel);
        return true;
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;
        _currency += amount;
        OnCurrencyChanged?.Invoke(_currency);
    }

    // Para testear con "plata infinita" mientras no este la economia real conectada.
    public void SetCurrency(int amount)
    {
        _currency = amount;
        OnCurrencyChanged?.Invoke(_currency);
    }

    public bool IsSpecialUnlocked(SO_SpecialNode special) =>
        special != null && _unlockedSpecials.Contains(special);

    public bool IsSpecialUnlockable(SO_SpecialNode special)
    {
        if (special == null || special.requirements == null || special.requirements.Length == 0)
            return false;

        foreach (var req in special.requirements)
        {
            if (req.node == null) return false;
            if (GetLevel(req.node) < req.requiredLevel) return false;
        }
        return true;
    }

    // Desbloquea el nodo rojo si ya cumple sus requisitos.
    public bool UnlockSpecial(SO_SpecialNode special)
    {
        if (IsSpecialUnlocked(special)) return false;
        if (!IsSpecialUnlockable(special)) return false;

        _unlockedSpecials.Add(special);
        OnSpecialNodeUnlocked?.Invoke(special);
        return true;
    }

    public void ResetAll()
    {
        _normalLevels.Clear();
        _unlockedSpecials.Clear();
    }
}
