using UnityEngine;

// El "cable" entre el arbol nuevo y el juego real.
// - Nodo normal subido de nivel -> aplica su modificador a RT_CannonData.
// - Nodo especial desbloqueado -> prende el flag en RT_PlayerUpgrades,
//   el MISMO que ya usa CannonBullet para chequear Ricochet/DoubleShot.
//   Por eso Ricochet y Split andan reales apenas los desbloqueás, sin
//   escribir nada mas.
//
// Poné este componente en el mismo GameObject donde tengas el RT_SkillTree
// (o en cualquiera, mientras le asignes las referencias).
public class SkillTreeEffectApplier : MonoBehaviour
{
    [Header("Arbol")]
    [SerializeField] private RT_SkillTree _tree;

    [Header("Canon")]
    [SerializeField] private AbilityController _abilityController;

    [Header("Habilidades especiales (mismo flag que usa CannonBullet)")]
    [SerializeField] private RT_PlayerUpgrades _playerUpgrades;

    private void OnEnable()
    {
        _tree.OnNormalNodeLeveled += HandleNormalLeveled;
        _tree.OnSpecialNodeUnlocked += HandleSpecialUnlocked;
    }

    private void OnDisable()
    {
        _tree.OnNormalNodeLeveled -= HandleNormalLeveled;
        _tree.OnSpecialNodeUnlocked -= HandleSpecialUnlocked;
    }

    private void HandleNormalLeveled(SO_NormalNode node, int level)
    {
        ApplyStat(node);
    }

    private void HandleSpecialUnlocked(SO_SpecialNode special)
    {
        if (special.specialAbility == SpecialAbilityType.None) return;

        _playerUpgrades.UnlockAbility(special.specialAbility);
        Debug.Log($"[SkillTreeEffectApplier] Habilidad desbloqueada: {special.specialAbility}");
    }

    private void ApplyStat(SO_NormalNode node)
    {
        if (node.statType == CannonStatType.None) return;

        RT_CannonData data = _abilityController.CannonAbility.RuntimeData;

        switch (node.statType)
        {
            case CannonStatType.Damage:
                data.damage = Apply(data.damage, node);
                break;
            case CannonStatType.Cooldown:
                // "mejorar" cadencia = bajar el cooldown
                data.cooldown = Apply(data.cooldown, node, invert: true);
                break;
            case CannonStatType.BulletSpeed:
                data.bulletSpeed = Apply(data.bulletSpeed, node);
                break;
            case CannonStatType.LaunchSpeed:
                data.launchSpeed = Apply(data.launchSpeed, node);
                break;
            case CannonStatType.ShotsPerBurst:
                data.shotsPerBurst = Mathf.RoundToInt(Apply(data.shotsPerBurst, node));
                break;
            case CannonStatType.TimeBetweenShots:
                // "mejorar" = disparos de la rafaga mas rapidos entre si
                data.timeBetweenShots = Apply(data.timeBetweenShots, node, invert: true);
                break;
        }

        Debug.Log($"[SkillTreeEffectApplier] {node.displayName} -> nivel {_tree.GetLevel(node)} ({node.statType})");
    }

    // invert = true para stats donde "mejorar" significa BAJAR el valor (cooldown, timeBetweenShots)
    private float Apply(float currentValue, SO_NormalNode node, bool invert = false)
    {
        if (node.modifierMode == StatModifierMode.PercentAdditive)
        {
            float factor = invert ? 1f - node.statValue / 100f : 1f + node.statValue / 100f;
            return currentValue * factor;
        }

        return invert ? currentValue - node.statValue : currentValue + node.statValue;
    }
}
