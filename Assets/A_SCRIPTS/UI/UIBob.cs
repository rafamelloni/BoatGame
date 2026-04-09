using System.Collections;
using UnityEngine;

public class UIBob : MonoBehaviour
{
    [SerializeField] private float _bobHeight = 5f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _bobWidth = 2f;
    [SerializeField] private float _bobWidthSpeed = 1.5f;

    private RectTransform _rectTransform;
    private Vector2 _originalPosition;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalPosition = _rectTransform.anchoredPosition;
        StartCoroutine(BobRoutine());
    }

    private IEnumerator BobRoutine()
    {
        while (true)
        {
            float y = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            float x = Mathf.Sin(Time.time * _bobWidthSpeed) * _bobWidth;
            _rectTransform.anchoredPosition = _originalPosition + new Vector2(x, y);
            yield return null;
        }
    }
}