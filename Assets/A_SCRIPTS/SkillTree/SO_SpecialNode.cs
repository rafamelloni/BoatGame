using UnityEngine;

// Requisito individual para desbloquear un nodo especial: un nodo normal
// y el nivel minimo que tiene que tener.
[System.Serializable]
public struct NodeRequirement
{
    public SO_NormalNode node;
    [Min(1)] public int requiredLevel;
}

// Nodo rojo: no mejora stats, solo entrega la habilidad especial.
// Depende de 1 o 2 nodos normales (ver "requirements").
[CreateAssetMenu(menuName = "SkillTree/Special Node", fileName = "SO_SpecialNode")]
public class SO_SpecialNode : SO_SkillNode
{
    [Header("Requisitos (1 o 2 nodos normales)")]
    public NodeRequirement[] requirements;

    [Header("Habilidad especial que otorga")]
    public SpecialAbilityType specialAbility = SpecialAbilityType.None;
}
