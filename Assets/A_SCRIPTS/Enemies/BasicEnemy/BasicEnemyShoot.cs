using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicEnemyShoot : MonoBehaviour
{

    public BulletFactory enemyBullet;

    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireRate = 2f;

    [Header("FOV")]
    [SerializeField] private Transform target;
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    private float _nextFireTime;

    void Update()
    {
        if (Time.time >= _nextFireTime && CanSeeTarget())
        {
            Shoot();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        var bullet = enemyBullet.Create();

        bullet.transform.position = shootPoint.position;
        bullet.transform.rotation = shootPoint.rotation;
    }
    private bool CanSeeTarget()
    {
        if (target == null) return false;

        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;

        float distance = dir.magnitude;
        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(transform.right, dir.normalized);
        if (angle > viewAngle * 0.5f) return false;

        // Line of sight (opcional pero recomendado)
        if (Physics.Raycast(transform.position, dir.normalized, distance, obstacleMask))
            return false;

        return true;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // Radio de visión
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        if (target == null) return;

        Vector3 forward = transform.right;
        float halfAngle = viewAngle * 0.5f;

        // Direcciones del cono
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;

        // Líneas del cono
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewDistance);

        // Línea al target
        if (CanSeeTarget())
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawLine(transform.position, target.position);
    }
}
