using UnityEngine;

// Base comun a los dos tipos de nodo del arbol de mejoras del canon.
public abstract class SO_SkillNode : ScriptableObject
{
    [Header("Info")]
    public string nodeId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
}
