using UnityEngine;

// Un objeto que se arma juntando fragmentos (3) que dan los templos.
// Crear: click derecho > Create > Botes > Relic
[CreateAssetMenu(menuName = "Botes/Relic", fileName = "SO_Relic")]
public class SO_RelicData : ScriptableObject
{
    public string displayName = "Reliquia";
    [Tooltip("Qué mejoras da (los valores se ajustan en RelicEffects)")]
    public RelicType effectType = RelicType.None;
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("Color con el que se muestra el nombre en el panel")]
    public Color color = new Color(1f, 0.8f, 0.25f);
}
