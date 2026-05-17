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
    private bool _locked;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string title, string description, GameObject hoverObject)
    {
        if (_locked) return;
        titleText.text = title;
        descriptionText.text = description;
        SetActive(currentPreviewModel, false);
        currentHoverObject = hoverObject;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        SetActive(currentHoverObject, true);
        SetActive(currentPreviewModel, true);
    }

    public void Hide()
    {
        if (_locked) return;
        if (currentHoverObject != null && currentHoverObject != PreselectionData.SelectedHoverObject)
            currentHoverObject.SetActive(false);
        SetActive(currentPreviewModel, false);
        currentHoverObject = null;
        currentPreviewModel = null;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void Lock(string title, string description)
    {
        _locked = true;
        titleText.text = title;
        descriptionText.text = description;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void Unlock()
    {
        _locked = false;
    }

    public void OnPanelExit() => Hide();

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}