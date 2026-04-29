public interface IUpgradeable
{
    string AbilityId { get; }
    bool IsUnlocked { get; }
    StatType[] ValidStats { get; } // <- nuevo
    void ApplyUpgrade(StatType stat, float value);
    void ResetUpgrades();
}