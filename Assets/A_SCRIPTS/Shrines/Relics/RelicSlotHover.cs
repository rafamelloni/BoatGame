using UnityEngine;
using UnityEngine.EventSystems;

// Detecta el mouse sobre un slot del HUD de reliquias (lo agrega RelicHUD solo).
public class RelicSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public RelicHUD hud;
    [HideInInspector] public int index;

    public void OnPointerEnter(PointerEventData e) { if (hud != null) hud.OnSlotHover(index, true); }
    public void OnPointerExit(PointerEventData e) { if (hud != null) hud.OnSlotHover(index, false); }
}
