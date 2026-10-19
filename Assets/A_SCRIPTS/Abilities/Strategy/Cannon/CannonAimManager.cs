using UnityEngine;
public class CannonAimManager : MonoBehaviour
{
    [SerializeField] private Transform _cannonRight;
    [SerializeField] private Transform _cannonLeft;

    [Header("Cañones extra (tier 2+)")]
    [Tooltip("Rotan igual que _cannonRight. Si están apagados no pasa nada.")]
    [SerializeField] private Transform[] _extraRight;
    [Tooltip("Rotan igual que _cannonLeft. Si están apagados no pasa nada.")]
    [SerializeField] private Transform[] _extraLeft;

    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private Camera cam;
    [SerializeField] private float _offsetRight = -90f;
    [SerializeField] private float _offsetLeft = -90f;

    private void Update()
    {
        Vector3? worldTarget = GetCursorWorldPos();
        if (worldTarget == null) return;
        Vector3 dirToTarget = worldTarget.Value - transform.position;
        dirToTarget.y = 0f;
        if (dirToTarget == Vector3.zero) return;
        float side = Vector3.Dot(dirToTarget, transform.right);
        Vector3 leaderDir = -dirToTarget.normalized;
        Vector3 localDir = transform.InverseTransformDirection(leaderDir);
        localDir.x = -localDir.x;
        Vector3 mirrorDir = transform.TransformDirection(localDir);

        Vector3 rightDir = side >= 0f ? leaderDir : mirrorDir;
        Vector3 leftDir = side >= 0f ? mirrorDir : leaderDir;

        ApplyRotation(_cannonRight, rightDir, _offsetRight);
        ApplyRotation(_cannonLeft, leftDir, _offsetLeft);

        if (_extraRight != null)
            foreach (var c in _extraRight)
                if (c != null && c.gameObject.activeInHierarchy)
                    ApplyRotation(c, rightDir, _offsetRight);

        if (_extraLeft != null)
            foreach (var c in _extraLeft)
                if (c != null && c.gameObject.activeInHierarchy)
                    ApplyRotation(c, leftDir, _offsetLeft);
    }

    private void ApplyRotation(Transform cannon, Vector3 dir, float offset)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, offset, 0f);
        cannon.rotation = Quaternion.Lerp(cannon.rotation, targetRot, Time.deltaTime * _rotationSpeed);
    }

    private Vector3? GetCursorWorldPos()
    {
        if (cam.gameObject.activeSelf)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);
            return null;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (_cannonLeft == null || _cannonRight == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_cannonRight.position, transform.right * 3f);
        for (int i = -90; i < 90; i += 10)
        {
            Vector3 from = _cannonRight.position + Quaternion.Euler(0f, i, 0f) * transform.right * 3f;
            Vector3 to = _cannonRight.position + Quaternion.Euler(0f, (i + 10), 0f) * transform.right * 3f;
            Gizmos.DrawLine(from, to);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_cannonLeft.position, -transform.right * 3f);
        for (int i = -90; i < 90; i += 10)
        {
            Vector3 from = _cannonLeft.position + Quaternion.Euler(0f, i, 0f) * (-transform.right) * 3f;
            Vector3 to = _cannonLeft.position + Quaternion.Euler(0f, (i + 10), 0f) * (-transform.right) * 3f;
            Gizmos.DrawLine(from, to);
        }
    }
}