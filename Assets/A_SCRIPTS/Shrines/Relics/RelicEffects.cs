using System.Collections.Generic;
using UnityEngine;

public enum RelicType { None, Ancla, Timon, Ron }

// Las mejoras que dan las reliquias (solo duran la run).
//  - Cada fragmento da una mejora chica de stats
//  - Al juntar las 3 piezas se desbloquea un efecto especial
// Todo lo que suma lo anota para poder deshacerlo exacto al reiniciar la run.
// Va en el mismo objeto que RelicInventory (si no está, RelicInventory lo agrega solo).
public class RelicEffects : MonoBehaviour
{
    public static RelicEffects Instance { get; private set; }

    [Header("Ancla — por fragmento")]
    [SerializeField, Range(0f, 1f)] private float _anclaMaxHealthPct = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _anclaBlockChance = 0.04f;
    [Header("Ancla — completa: Anclado")]
    [Tooltip("Por debajo de esta fracción de tu velocidad máxima contás como 'anclado'")]
    [SerializeField, Range(0f, 1f)] private float _anclaSlowThreshold = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _anclaDamageReduction = 0.4f;

    [Header("Timón — por fragmento")]
    [SerializeField, Range(0f, 1f)] private float _timonMoveSpeedPct = 0.10f;
    [SerializeField, Range(0f, 1f)] private float _timonTurnSpeedPct = 0.15f;
    [Header("Timón — completa: Viento a favor")]
    [SerializeField] private float _timonSprintMultiplierAdd = 0.3f;
    [Tooltip("El sprint gasta esta fracción de lo normal (0.5 = la mitad)")]
    [SerializeField, Range(0.1f, 1f)] private float _timonSprintDrainMul = 0.5f;

    [Header("Ron — por fragmento")]
    [SerializeField, Range(0f, 1f)] private float _ronDamagePct = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float _ronReloadPct = 0.08f;
    [Header("Ron — completa: Barril sin fondo")]
    [Tooltip("Probabilidad de que un disparo no gaste munición")]
    [SerializeField, Range(0f, 1f)] private float _ronFreeShotChance = 0.3f;

    // referencias (se buscan solas)
    private RT_PlayerStats _stats;
    private RT_PlayerHealth _health;
    private Movement _move;
    private CannonStrategy _cannon;

    private RelicInventory _inventory;

    // deshacer: cada cambio aplicado guarda cómo revertirlo
    private readonly List<System.Action> _undo = new List<System.Action>();
    private int _lastAmmo = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        _inventory = GetComponent<RelicInventory>();
        if (_inventory == null) _inventory = FindFirstObjectByType<RelicInventory>();
        if (_inventory != null)
        {
            _inventory.OnFragmentAdded += OnFragmentAdded;
            _inventory.OnReset += RevertAll;
        }
    }

    // ------------------------------------------------------------ aplicar

    private void OnFragmentAdded(SO_RelicData relic, int count)
    {
        if (relic == null) return;
        FindRefs();
        bool complete = count >= RelicInventory.FragmentsToComplete;

        switch (relic.effectType)
        {
            case RelicType.Ancla:
                AddMaxHealthPct(_anclaMaxHealthPct);
                AddBlockChance(_anclaBlockChance);
                if (complete) EnableAnclado();
                break;

            case RelicType.Timon:
                AddMovePct(_timonMoveSpeedPct, _timonTurnSpeedPct);
                if (complete) EnableVientoAFavor();
                break;

            case RelicType.Ron:
                AddCannonPct(_ronDamagePct, _ronReloadPct);
                if (complete) EnableBarrilSinFondo();
                break;
        }
    }

    private void AddMaxHealthPct(float pct)
    {
        if (_stats == null) return;
        float add = _stats.maxHealth * pct;
        _stats.maxHealth += add;
        _stats.currentHealth = Mathf.Min(_stats.currentHealth + add, _stats.maxHealth); // te cura lo que sumó
        if (_health != null) _health.RefreshUI();
        var st = _stats;
        _undo.Add(() => { st.maxHealth -= add; st.currentHealth = Mathf.Min(st.currentHealth, st.maxHealth); });
    }

    private void AddBlockChance(float add)
    {
        if (_stats == null) return;
        _stats.blockChance += add;
        var st = _stats;
        _undo.Add(() => st.blockChance -= add);
    }

    private void AddMovePct(float movePct, float turnPct)
    {
        if (_stats == null) return;
        float m = _stats.moveSpeed * movePct;
        float t = _stats.turnSpeed * turnPct;
        _stats.moveSpeed += m;
        _stats.turnSpeed += t;
        var st = _stats;
        _undo.Add(() => { st.moveSpeed -= m; st.turnSpeed -= t; });
    }

    private void AddCannonPct(float dmgPct, float reloadPct)
    {
        if (_cannon == null) return;
        var data = _cannon._rtData;
        float d = data.damage * dmgPct;
        float c = data.cooldown * reloadPct;
        data.damage += d;
        data.cooldown -= c;
        _undo.Add(() => { data.damage -= d; data.cooldown += c; });
    }

    // Ancla completa: si vas despacio recibís menos daño
    private void EnableAnclado()
    {
        if (_health == null) return;
        var health = _health;
        System.Func<float, float> mod = amount =>
        {
            if (_move != null && _move.SpeedRatio < _anclaSlowThreshold)
                return amount * (1f - _anclaDamageReduction);
            return amount;
        };
        health.IncomingDamageModifier += mod;
        _undo.Add(() => health.IncomingDamageModifier -= mod);
    }

    // Timón completo: sprint más rápido y que gasta menos
    private void EnableVientoAFavor()
    {
        if (_move == null) return;
        var mv = _move;
        float originalDrain = mv.sprintDrainRate;
        mv.sprintSpeedMultiplier += _timonSprintMultiplierAdd;
        mv.sprintDrainRate *= _timonSprintDrainMul;
        float add = _timonSprintMultiplierAdd;
        _undo.Add(() => { mv.sprintSpeedMultiplier -= add; mv.sprintDrainRate = originalDrain; });
    }

    // Ron completo: chance de que un disparo no gaste bala
    private void EnableBarrilSinFondo()
    {
        if (_cannon == null) return;
        var cannon = _cannon;
        _lastAmmo = cannon.CurrentAmmo;
        System.Action<int, int> handler = (current, max) =>
        {
            bool shot = !cannon.IsReloading && current == _lastAmmo - 1;
            _lastAmmo = current;
            if (shot && Random.value < _ronFreeShotChance)
            {
                cannon.RefundShot();
                _lastAmmo = cannon.CurrentAmmo;
            }
        };
        cannon.OnAmmoChanged += handler;
        _undo.Add(() => cannon.OnAmmoChanged -= handler);
    }

    // ------------------------------------------------------------ reset

    private void RevertAll()
    {
        for (int i = _undo.Count - 1; i >= 0; i--)
        {
            try { _undo[i](); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
        _undo.Clear();
        _lastAmmo = -1;
        // la run nueva puede crear otro cañón/jugador: volver a buscarlos
        _cannon = null; _stats = null; _health = null; _move = null;
    }

    private void FindRefs()
    {
        if (_move == null) _move = FindFirstObjectByType<Movement>();
        if (_stats == null && _move != null) _stats = _move.GetComponent<RT_PlayerStats>();
        if (_stats == null) _stats = FindFirstObjectByType<RT_PlayerStats>();
        if (_health == null && _stats != null) _health = _stats.GetComponent<RT_PlayerHealth>();
        if (_health == null) _health = FindFirstObjectByType<RT_PlayerHealth>();
        if (_cannon == null)
        {
            var ac = FindFirstObjectByType<AbilityController>();
            if (ac != null) _cannon = ac.CannonAbility;
        }
    }

    // ------------------------------------------------------------ textos

    // Qué te dio ESTE fragmento (para el panel del cofre)
    public string DescribeGain(SO_RelicData relic, int count)
    {
        if (relic == null) return "";
        bool complete = count >= RelicInventory.FragmentsToComplete;
        string step = FragmentText(relic.effectType);
        if (complete) step += $"\n<b>¡{SpecialName(relic.effectType)} desbloqueado!</b>";
        return step;
    }

    // Todo lo que hace la reliquia (para el cartel del HUD), marcando lo que ya tenés
    public string DescribeAll(SO_RelicData relic, int count)
    {
        if (relic == null) return "";
        int need = RelicInventory.FragmentsToComplete;
        string on = "#FFD966", off = "#8A8FA8";
        string perFrag = $"<color={(count > 0 ? on : off)}>Cada fragmento: {FragmentText(relic.effectType)}</color>";
        if (count > 0) perFrag += $"  <color={on}>(x{Mathf.Min(count, need)})</color>";
        string special = $"<color={(count >= need ? on : off)}>Completa — <b>{SpecialName(relic.effectType)}</b>: {SpecialText(relic.effectType)}</color>";
        return perFrag + "\n" + special;
    }

    private string FragmentText(RelicType t)
    {
        switch (t)
        {
            case RelicType.Ancla: return $"+{Pct(_anclaMaxHealthPct)} vida máxima, +{Pct(_anclaBlockChance)} de bloquear golpes";
            case RelicType.Timon: return $"+{Pct(_timonMoveSpeedPct)} velocidad, +{Pct(_timonTurnSpeedPct)} giro";
            case RelicType.Ron: return $"+{Pct(_ronDamagePct)} daño del cañón, -{Pct(_ronReloadPct)} tiempo de recarga";
            default: return "";
        }
    }

    private string SpecialName(RelicType t)
    {
        switch (t)
        {
            case RelicType.Ancla: return "Anclado";
            case RelicType.Timon: return "Viento a favor";
            case RelicType.Ron: return "Barril sin fondo";
            default: return "";
        }
    }

    private string SpecialText(RelicType t)
    {
        switch (t)
        {
            case RelicType.Ancla: return $"si vas despacio recibís {Pct(_anclaDamageReduction)} menos daño";
            case RelicType.Timon: return $"el sprint es más rápido y gasta {Pct(1f - _timonSprintDrainMul)} menos";
            case RelicType.Ron: return $"{Pct(_ronFreeShotChance)} de chance de que un disparo no gaste bala";
            default: return "";
        }
    }

    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
}
