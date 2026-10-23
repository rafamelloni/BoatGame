using UnityEngine;

// Cámara del Overworld: sigue al barquito desde arriba con un ángulo fijo.
public class OverworldCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 22f, -16f);
    [SerializeField] private float _followSpeed = 4f;

    private void Start()
    {
        if (_target == null) return;
        transform.position = _target.position + _offset;
        transform.rotation = Quaternion.LookRotation(-_offset.normalized, Vector3.up);
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        Vector3 desired = _target.position + _offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(-_offset.normalized, Vector3.up);
    }
}
