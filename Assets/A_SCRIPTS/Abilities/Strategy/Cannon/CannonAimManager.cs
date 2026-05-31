using UnityEngine;
public class CannonAimManager : MonoBehaviour
{
    [SerializeField] private Transform _cannonRight;
    [SerializeField] private Transform _cannonLeft;
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
        if (side >= 0f)
        {
            ApplyRotation(_cannonRight, leaderDir);
            ApplyRotation(_cannonLeft, mirrorDir);
        }
        else
        {
            ApplyRotation(_cannonLeft, leaderDir);
            ApplyRotation(_cannonRight, mirrorDir);
        }
    }
    private void ApplyRotation(Transform cannon, Vector3 dir)
    {
        bool isRightCannon = cannon == _cannonRight;
        float offset = isRightCannon ? _offsetRight : _offsetLeft;
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
        UnityEditor.Handles.Label(_cannonRight.position + Vector3.up * 0.5f,
            $"R: {_cannonRight.localEulerAngles.y:F1}°");
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_cannonLeft.position, -transform.right * 3f);
        for (int i = -90; i < 90; i += 10)
        {
            Vector3 from = _cannonLeft.position + Quaternion.Euler(0f, i, 0f) * (-transform.right) * 3f;
            Vector3 to = _cannonLeft.position + Quaternion.Euler(0f, (i + 10), 0f) * (-transform.right) * 3f;
            Gizmos.DrawLine(from, to);
        }
        UnityEditor.Handles.Label(_cannonLeft.position + Vector3.up * 0.5f,
            $"L: {_cannonLeft.localEulerAngles.y:F1}°");
    }
}