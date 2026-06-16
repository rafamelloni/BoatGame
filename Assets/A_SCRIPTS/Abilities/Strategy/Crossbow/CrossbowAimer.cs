using UnityEngine;

public class CrossbowAimer : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;

    private Transform _target;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void Update()
    {
        if (_target == null) return;

        Vector3 dir = (_target.position - transform.position).normalized;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }
}