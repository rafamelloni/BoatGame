using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos")]
    [SerializeField] private string skillName;
    [TextArea(2, 5)]
    [SerializeField] private string skillDescription;
    [SerializeField] private SelectedAbility ability;

    [Header("GameObjects")]
    [SerializeField] private GameObject panelHoverObject;
    [SerializeField] private GameObject previewModel;

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
        PreselectionData.SetAbility(ability, panelHoverObject);
        SkillPreselectionManager.Instance?.OnAbilitySelected(ability);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!SkillPreselectionManager.IsUnlocked) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        SkillHoverPanel.Instance.Show(skillName, skillDescription, rectTransform, panelHoverObject, previewModel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!SkillPreselectionManager.IsUnlocked) return;
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

    public SelectedAbility Ability => ability;
}