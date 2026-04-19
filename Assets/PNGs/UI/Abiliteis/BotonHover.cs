using UnityEngine;
using UnityEngine.EventSystems;

public class BotonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject target;

    public void OnPointerEnter(PointerEventData eventData)
    {
        target.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target.SetActive(false);
    }
}
