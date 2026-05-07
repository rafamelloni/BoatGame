using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShipButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos")]
    [SerializeField] private string shipName;
    [TextArea(2, 5)]
    [SerializeField] private string shipDescription;
    [SerializeField] private SO_BASESTATS shipStats;

    [Header("GameObjects")]
    [SerializeField] private GameObject previewModel; // RawImage
    [SerializeField] private GameObject shipMesh;     // visual del juego, persiste al click

    [Header("Selección visual")]
    [SerializeField] private GameObject selectedIndicator;

    private RectTransform rectTransform;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnClick()
    {
        PreselectionData.SetShip(shipStats, shipMesh);
        ShipPreselectionManager.Instance?.OnShipSelected(this);
        SkillPreselectionManager.Instance?.UnlockSkillButtons();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        // previewModel va al Show — se activa/desactiva con el hover
        SkillHoverPanel.Instance.Show(shipName, shipDescription, rectTransform, null, previewModel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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
        var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var result in results)
            if (result.gameObject.GetComponent<SkillHoverPanel>() != null) return true;
        return false;
    }

    public void SetSelectedIndicator(bool active)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(active);
    }
}