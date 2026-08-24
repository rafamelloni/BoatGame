using UnityEngine;
using UnityEngine.EventSystems;

// Panel de pan + zoom para el arbol, tipo editor de nodos.
// Poné este componente en el "Viewport" (el panel visible, recortado con
// un RectMask2D, con una Image encima -aunque sea transparente- para que
// pueda recibir el click-and-drag y el scroll en cualquier parte vacia,
// no solo sobre los botones).
//
// Jerarquia esperada:
//   Viewport (RectMask2D + Image raycastable + este script)
//     Content (RectTransform, anchors y pivot en el centro 0.5/0.5)
//       -> aca van todos tus botones del arbol, posicionados como quieras
[RequireComponent(typeof(RectTransform))]
public class SkillTreePanZoom : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Referencias")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private Canvas _canvas;

    [Header("Pan")]
    [Tooltip("Que tan rapido se mueve el arbol respecto al movimiento del mouse al arrastrar. 1 = sigue al mouse 1 a 1.")]
    [SerializeField] private float _panSpeed = 1f;
    [SerializeField] private bool _clampPan = true;
    [Tooltip("Cuanto te podés alejar del centro del arbol, en pixels de UI (eje X, eje Y). Bajalo para acortar el rango de drag.")]
    [SerializeField] private Vector2 _maxPanDistance = new Vector2(250f, 200f);

    [Header("Zoom")]
    [Tooltip("Que tan rapido zoomea cada 'click' de la rueda del mouse. Bajalo para que sea mas lento.")]
    [SerializeField] private float _zoomSpeed = 0.05f;
    [SerializeField] private float _minZoom = 0.5f;
    [SerializeField] private float _maxZoom = 2f;

    private RectTransform _viewport;

    private void Awake()
    {
        _viewport = (RectTransform)transform;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_content == null) return;

        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        _content.anchoredPosition += (eventData.delta / scaleFactor) * _panSpeed;

        ClampContentPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_content == null) return;

        float oldScale = _content.localScale.x;
        float newScale = Mathf.Clamp(oldScale + eventData.scrollDelta.y * _zoomSpeed, _minZoom, _maxZoom);
        if (Mathf.Approximately(oldScale, newScale)) return;

        Camera eventCamera = GetEventCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _viewport, eventData.position, eventCamera, out Vector2 localPointInViewport);

        // Punto bajo el cursor, en espacio local del content sin escala.
        Vector2 contentLocalPoint = (localPointInViewport - _content.anchoredPosition) / oldScale;

        _content.localScale = Vector3.one * newScale;

        // Reacomoda el content para que ese mismo punto siga bajo el cursor
        // (asi el zoom "entra" hacia donde apunta el mouse, no hacia el centro).
        _content.anchoredPosition = localPointInViewport - contentLocalPoint * newScale;

        ClampContentPosition();
    }

    private void ClampContentPosition()
    {
        if (!_clampPan || _content == null) return;

        Vector2 pos = _content.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -_maxPanDistance.x, _maxPanDistance.x);
        pos.y = Mathf.Clamp(pos.y, -_maxPanDistance.y, _maxPanDistance.y);
        _content.anchoredPosition = pos;
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null) return null;
        return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
    }
}
