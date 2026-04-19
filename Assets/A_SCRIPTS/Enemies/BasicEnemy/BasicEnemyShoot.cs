using UnityEngine;

public class BasicEnemyShoot : MonoBehaviour
{
    public BulletFactory enemyBullet;
    [SerializeField] private Transform shootPoint;

    [Header("FOV")]
    [SerializeField] private Transform target;
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float _aimRadius = 2f;

    [Header("Data")]
    public SO_EnemyData SO_EnemyData;
    private RT_EnemyStats RT_EnemyData;
    private ParticleSystem _smokeParticle;

    private void Awake()
    {
        RT_EnemyData = new RT_EnemyStats(SO_EnemyData);
        _smokeParticle = GetComponentInChildren<ParticleSystem>();
    }
    public void SetTarget(Transform target, BulletFactory enemyBullet)
    {
        this.target = target;
        this.enemyBullet = enemyBullet;
    }

    public void ForceShoot()
    {
        if (!CanSeeTarget()) return;
        Vector3 randomOffset = new Vector3(
            Random.Range(-_aimRadius, _aimRadius),
            0f,
            Random.Range(-_aimRadius, _aimRadius)
        );
        Vector3 aimPoint = target.position + randomOffset;
        var bullet = enemyBullet.Create();
        bullet.GetComponent<BasicEnemyBullet>().SetTarget(aimPoint, RT_EnemyData, shootPoint);

        if (_smokeParticle != null) 
        {
            _smokeParticle.Play();

        }

    }

    public bool CanSeeTarget()
    {
        if (target == null) return false;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        float distance = dir.magnitude;
        if (distance > viewDistance) return false;
        float angle = Vector3.Angle(transform.right, dir.normalized);
        if (angle > viewAngle * 0.5f) return false;
        if (Physics.Raycast(transform.position, dir.normalized, distance, obstacleMask))
            return false;
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        if (target == null) return;
        Vector3 forward = transform.right;
        float halfAngle = viewAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewDistance);
        Gizmos.color = CanSeeTarget() ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, target.position);
    }
}