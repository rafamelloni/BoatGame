using UnityEngine;
using TMPro;

public class SkillHoverPanel : MonoBehaviour
{
    public static SkillHoverPanel Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Canvas raíz")]
    [SerializeField] private Canvas rootCanvas;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject currentHoverObject;
    private GameObject currentPreviewModel;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string title, string description, RectTransform buttonRect, GameObject hoverObject, GameObject previewModel = null)
    {

        Debug.Log($"[Show] previewModel: {(previewModel != null ? previewModel.name : "NULL")}");
        titleText.text = title;
        descriptionText.text = description;

        // Ocultar hoverObject anterior solo si no está seleccionado
        if (currentHoverObject != null && currentHoverObject != PreselectionData.SelectedHoverObject)
            currentHoverObject.SetActive(false);

        // Ocultar previewModel anterior siempre (no persiste)
        SetActive(currentPreviewModel, false);

        currentHoverObject = hoverObject;
        currentPreviewModel = previewModel;

        Vector3[] corners = new Vector3[4];
        buttonRect.GetWorldCorners(corners);
        float worldCenterY = (corners[0].y + corners[1].y) / 2f;

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        RectTransform panelParent = rectTransform.parent as RectTransform;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(corners[0].x, worldCenterY, corners[0].z));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelParent, screenPoint, cam, out Vector2 localPoint);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, localPoint.y);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        SetActive(currentHoverObject, true);
        SetActive(currentPreviewModel, true);
    }

    public void Hide()
    {
        // Solo desactivar hoverObject si no está seleccionado
        if (currentHoverObject != null && currentHoverObject != PreselectionData.SelectedHoverObject)
            currentHoverObject.SetActive(false);

        // previewModel siempre se desactiva
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