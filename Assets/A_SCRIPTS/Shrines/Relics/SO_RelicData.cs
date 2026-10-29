using UnityEngine;

public enum RelicRarity { Common, Rare, SuperRare }

// Una reliquia que te da un templo al completarlo (entera, ya no por fragmentos).
// Crear: click derecho > Create > Botes > Relic
[CreateAssetMenu(menuName = "Botes/Relic", fileName = "SO_Relic")]
public class SO_RelicData : ScriptableObject
{
    public string displayName = "Reliquia";
    [Tooltip("Qué hace (los valores se ajustan en RelicEffects)")]
    public RelicType effectType = RelicType.None;
    public RelicRarity rarity = RelicRarity.Common;
    [Tooltip("Frase de ambiente que se muestra en el cartel (lo que hace lo escribe RelicEffects)")]
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("Color del nombre. Si lo dejás en blanco se usa el color de la rareza")]
    public Color color = Color.white;

    public Color NameColor => color == Color.white ? RarityColor(rarity) : color;

    public static Color RarityColor(RelicRarity r)
    {
        switch (r)
        {
            case RelicRarity.Rare: return new Color(0.30f, 0.65f, 1f);
            case RelicRarity.SuperRare: return new Color(1f, 0.76f, 0.24f);
            default: return new Color(0.80f, 0.82f, 0.86f);
        }
    }

    public static string RarityLabel(RelicRarity r)
    {
        switch (r)
        {
            case RelicRarity.Rare: return "Rare";
            case RelicRarity.SuperRare: return "Super Rare";
            default: return "Common";
        }
    }
}
