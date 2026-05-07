using UnityEngine;

public class ShipPreselectionManager : MonoBehaviour
{
    public static ShipPreselectionManager Instance { get; private set; }

    [SerializeField] private ShipButton[] shipButtons;

    private void Awake()
    {
        Instance = this;
    }

    public void OnShipSelected(ShipButton selected)
    {
        foreach (var btn in shipButtons)
            btn.SetSelectedIndicator(btn == selected);
    }

    public void Reset()
    {
        foreach (var btn in shipButtons)
            btn.SetSelectedIndicator(false);
    }
}