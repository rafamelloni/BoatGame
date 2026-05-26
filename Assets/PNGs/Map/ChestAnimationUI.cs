using UnityEngine;

public class ChestAnimationUI : MonoBehaviour
{
    [SerializeField] private float rotationAmount = 8f;
    [SerializeField] private float speed = 2f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        float z = Mathf.Sin(Time.time * speed) * rotationAmount;
        _rect.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}
