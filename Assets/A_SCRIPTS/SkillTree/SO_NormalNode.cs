using UnityEngine;

// Nodo normal: mejora un stat y se puede subir de nivel varias veces.
// El umbral para desbloquear el nodo rojo que cuelga de este nodo se define
// en el propio SO_SpecialNode (NodeRequirement.requiredLevel), no aca.
[CreateAssetMenu(menuName = "SkillTree/Normal Node", fileName = "SO_NormalNode")]
public class SO_NormalNode : SO_SkillNode
{
    [Header("Niveles")]
    [Min(1)] public int maxLevel = 5;

    [Header("Costo (testing: se paga con plata infinita)")]
    [Min(0)] public int cost = 1;

    [Header("Efecto en el canon (se aplica una vez por cada nivel que subís)")]
    public CannonStatType statType = CannonStatType.None;
    public StatModifierMode modifierMode = StatModifierMode.PercentAdditive;
    public float statValue;
}
