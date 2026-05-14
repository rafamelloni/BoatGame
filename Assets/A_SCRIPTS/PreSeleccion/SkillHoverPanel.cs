using UnityEngine;
using TMPro;

public class SkillHoverPanel : MonoBehaviour
{
    public static SkillHoverPanel Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private CanvasGroup canvasGroup;
    private GameObject currentHoverObject;
    private GameObject currentPreviewModel;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string title, string description, GameObject hoverObject)
    {
        titleText.text = title;
        descriptionText.text = description;

        if (currentHoverObject != null && currentHoverObject != PreselectionData.SelectedHoverObject)
            currentHoverObject.SetActive(false);

        SetActive(currentPreviewModel, false);

        currentHoverObject = hoverObject;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        SetActive(currentHoverObject, true);
        SetActive(currentPreviewModel, true);
    }

    public void Hide()
    {
        if (currentHoverObject != null && currentHoverObject != PreselectionData.SelectedHoverObject)
            currentHoverObject.SetActive(false);

        SetActive(currentPreviewModel, false);

        currentHoverObject = null;
        currentPreviewModel = null;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnPanelExit() => Hide();

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}