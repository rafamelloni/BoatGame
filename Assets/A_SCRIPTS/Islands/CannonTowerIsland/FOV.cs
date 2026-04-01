using UnityEngine;

public class FOV : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _viewDistance = 20f;
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Canvas")]
    [SerializeField] private GameObject _uiHealth;

    private void Update()
    {
        if(CanSeeTarget())
        {
            //acttiva el canvas de vida, feedback de que te puede atacar
            _uiHealth.SetActive(true);
            print(CanSeeTarget());
        }
        else
        {
            _uiHealth.SetActive(false);

        }

    }
    public bool CanSeeTarget()
    {
        if (_target == null) return false;

        Vector3 dirToTarget = _target.position - transform.position;
        dirToTarget.y = 0f;

        float distance = dirToTarget.magnitude;
        if (distance > _viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
        if (angle > _viewAngle * 0.5f) return false;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, dirToTarget.normalized, distance, _obstacleMask))
            return false;

        return true;
    }

    public float DistanceToTarget()
    {
        if (_target == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, _target.position);
    }

    public Transform GetTarget()
    {
        return _target;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Vector3 leftDir = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, _viewAngle * 0.5f, 0) * forward;

        Gizmos.DrawLine(origin, origin + forward * _viewDistance);
        Gizmos.DrawLine(origin, origin + leftDir * _viewDistance);
        Gizmos.DrawLine(origin, origin + rightDir * _viewDistance);
    }
}