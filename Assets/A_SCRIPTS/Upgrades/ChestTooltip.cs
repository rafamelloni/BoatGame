using UnityEngine;
using UnityEngine.EventSystems;

public class ChestTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UpgradePathMap pathMap;

    public void OnPointerEnter(PointerEventData eventData)
    {
        pathMap.ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pathMap.HideTooltip();
    }
}