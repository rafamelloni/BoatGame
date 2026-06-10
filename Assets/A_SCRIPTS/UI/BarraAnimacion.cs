using UnityEngine;

public class UIFloatingIdle : MonoBehaviour
{
    [Header("Objeto a flotar")]
    [SerializeField] private RectTransform target;

    [Header("Movimiento")]
    [SerializeField] private float moveAmountY = 3f;
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Rotación")]
    [SerializeField] private float rotationAmount = 1.2f;
    [SerializeField] private float rotationSpeed = 1f;

    private Vector2 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        startPos = target.anchoredPosition;
        startRot = target.localRotation;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * moveSpeed) * moveAmountY;
        float rot = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;

        target.anchoredPosition = startPos + new Vector2(0f, y);
        target.localRotation = startRot * Quaternion.Euler(0f, 0f, rot);
    }
}