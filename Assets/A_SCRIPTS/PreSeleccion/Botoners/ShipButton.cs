using UnityEngine;
using UnityEngine.EventSystems;

public class ShipButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos")]
    [SerializeField] private string shipName;
    [TextArea(2, 5)]
    [SerializeField] private string shipDescription;
    [SerializeField] private SO_BASESTATS shipStats;

    [Header("GameObjects")]
    [SerializeField] private GameObject shipMesh;
    [SerializeField] private GameObject ImageShip;

    [Header("Selección visual")]
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private AbilityController abilityController;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (ImageShip != null)
            ImageShip.SetActive(false);
    }

    public void OnClick()
    {
        PreselectionData.SetShip(shipStats, shipMesh);
        PreselectionData.SetAbility(shipStats.ability, shipMesh);
        ShipPreselectionManager.Instance?.OnShipSelected(this);
        if (ImageShip != null)
            ImageShip.SetActive(true);
        SkillHoverPanel.Instance.Lock(shipName, shipDescription);
        switch (shipStats.ability)
        {
            case SelectedAbility.Cannon:
                abilityController.CannonAveilable();
                break;
            case SelectedAbility.Mortar:
                abilityController.MortarAveilable();
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        if (ImageShip != null)
            ImageShip.SetActive(true);
        SkillHoverPanel.Instance.Show(shipName, shipDescription, null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ImageShip != null)
            ImageShip.SetActive(false);
        hideCoroutine = StartCoroutine(HideDelayed());
    }

    private System.Collections.IEnumerator HideDelayed()
    {
        yield return null;
        yield return null;
        if (EventSystem.current != null && !IsPointerOverPanel())
            SkillHoverPanel.Instance.Hide();
        hideCoroutine = null;
    }

    private bool IsPointerOverPanel()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<SkillHoverPanel>() != null)
                return true;
        }
        return false;
    }

    public void SetSelectedIndicator(bool active)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(active);
    }
}