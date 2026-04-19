using UnityEngine;
using static UnityEngine.ParticleSystem;

public class BasicEnemy : Enemy
{
    [Header("BasicEnemy Class Temporal")]
    [SerializeField] private Transform leader;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private ParticleSystem _trailP;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleRadius = 15f;
    [SerializeField] private float obstacleForce = 3f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 4f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private LayerMask enemyLayer;

    private Vector3 formationOffset;
    private bool _stopped;

    public void SetPlayer(Transform player)
    {
        this.player = player;
        if (leader != null)
            formationOffset = transform.position - leader.position;
    }

    public void SetStopped(bool stopped)
    {
        _stopped = stopped;
    }

    private void Update()
    {
        if (player == null) return;

        if (leader != null && !leader.gameObject.activeInHierarchy)
            leader = null;

        Vector3 targetPos = leader != null
            ? leader.position + formationOffset
            : player.position;

        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        _trailP.Play();

        if (!_stopped && dir.sqrMagnitude > 0.01f)
        {
            // Avoidance islas
            Vector3 avoidance = Vector3.zero;
            Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleRadius, obstacleLayer);
            foreach (var col in obstacles)
            {
                Vector3 away = transform.position - col.ClosestPoint(transform.position);
                away.y = 0f;
                float dist = away.magnitude;
                if (dist > 0.001f)
                    avoidance += away.normalized / dist;
            }

            // Separacion entre enemies
            Vector3 separation = Vector3.zero;
            Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);
            foreach (var col in neighbors)
            {
                if (col.gameObject == gameObject) continue;
                Vector3 away = transform.position - col.transform.position;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist > 0.001f)
                    separation += away.normalized / dist;
            }

            Vector3 move = dir.normalized + avoidance * obstacleForce + separation * separationForce;
            move.y = 0f;

            transform.position += move.normalized * speed * Time.deltaTime;

            // Rotacion sigue la direccion real de movimiento
            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move.normalized);
                targetRot *= Quaternion.Euler(0f, -90f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
        else if (_stopped)
        {
            _trailP.Stop();
            // Parado, rota hacia el player
            Vector3 rotDir = player.position - transform.position;
            rotDir.y = 0f;
            if (rotDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(rotDir);
                targetRot *= Quaternion.Euler(0f, -90f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}