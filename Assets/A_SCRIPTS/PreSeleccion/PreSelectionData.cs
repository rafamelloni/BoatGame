public enum SelectedAbility
{
    None,
    Cannon,
    Mortar
}

public static class PreselectionData
{
    // Habilidad
    public static SelectedAbility Ability { get; private set; } = SelectedAbility.None;
    public static UnityEngine.GameObject SelectedHoverObject { get; private set; } = null;

    // Barco
    public static SO_BASESTATS SelectedShipStats { get; private set; } = null;
    public static UnityEngine.GameObject SelectedShipMesh { get; private set; } = null;

    public static void SetAbility(SelectedAbility ability, UnityEngine.GameObject hoverObject)
    {
        if (Ability == ability && SelectedHoverObject == hoverObject) return;

        if (SelectedHoverObject != null && SelectedHoverObject != hoverObject)
            SelectedHoverObject.SetActive(false);

        Ability = ability;
        SelectedHoverObject = hoverObject;

        if (SelectedHoverObject != null)
            SelectedHoverObject.SetActive(true);
    }

    public static void SetShip(SO_BASESTATS stats, UnityEngine.GameObject mesh)
    {
        if (SelectedShipMesh != null && SelectedShipMesh != mesh)
            SelectedShipMesh.SetActive(false);

        SelectedShipStats = stats;
        SelectedShipMesh = mesh;

        if (SelectedShipMesh != null)
            SelectedShipMesh.SetActive(true);
    }

    public static void Reset()
    {
        if (SelectedHoverObject != null)
            SelectedHoverObject.SetActive(false);

        if (SelectedShipMesh != null)
            SelectedShipMesh.SetActive(false);

        Ability = SelectedAbility.None;
        SelectedHoverObject = null;
        SelectedShipStats = null;
        SelectedShipMesh = null;
    }
}