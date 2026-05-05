using UnityEngine;

public class ModelPreviewRotator : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private void OnEnable()
    {
        // Resetear rotación al activarse (opcional, comentar si no querés)
        // transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.World);
    }
}