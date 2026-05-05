public enum SelectedAbility
{
    None,
    Cannon,
    Mortar
}

public static class PreselectionData
{
    public static SelectedAbility Ability { get; private set; } = SelectedAbility.None;
    public static UnityEngine.GameObject SelectedHoverObject { get; private set; } = null;

    public static void SetAbility(SelectedAbility ability, UnityEngine.GameObject hoverObject)
    {
        if (Ability == ability && SelectedHoverObject == hoverObject) return;

        // Desactivar el hoverObject anterior
        if (SelectedHoverObject != null && SelectedHoverObject != hoverObject)
            SelectedHoverObject.SetActive(false);

        Ability = ability;
        SelectedHoverObject = hoverObject;

        if (SelectedHoverObject != null)
            SelectedHoverObject.SetActive(true);
    }

    public static void Reset()
    {
        if (SelectedHoverObject != null)
            SelectedHoverObject.SetActive(false);

        Ability = SelectedAbility.None;
        SelectedHoverObject = null;
    }
}