// Que stat del canon toca un nodo normal. Sumá casos aca cuando quieras
// que un nodo controle otro campo de RT_CannonData (y agregalo tambien
// al switch de SkillTreeEffectApplier.ApplyStat).
public enum CannonStatType
{
    None,
    Damage,
    Cooldown,
    BulletSpeed,
    LaunchSpeed,
    ShotsPerBurst,
    TimeBetweenShots,
}

// Como se aplica statValue sobre el stat actual.
public enum StatModifierMode
{
    PercentAdditive, // multiplica: currentValue * (1 +/- statValue/100)
    FlatAdd,         // suma/resta: currentValue +/- statValue
}
