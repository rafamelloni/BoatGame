using UnityEngine;

public class BasicEnemy : Enemy
{
    [SerializeField] private Transform leader;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    private Vector3 formationOffset;

    private void Start()
    {
        if (leader != null)
        {
            formationOffset = transform.position - leader.position;
        }
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 targetPos;

        // Si este enemigo tiene líder, mantiene formación
        if (leader != null)
        {
            // El líder apunta al player, pero este mantiene su lugar relativo
            Vector3 leaderToPlayer = (player.position - leader.position);
            leaderToPlayer.y = 0f;

            Vector3 leaderMoveDir = leaderToPlayer.normalized;
            targetPos = leader.position + formationOffset;

            // opcional: que la formación mire hacia adelante del líder
            // targetPos += leaderMoveDir * 0f;
        }
        else
        {
            // Si no tiene líder, este enemigo es el líder y persigue al player
            targetPos = player.position;
        }

        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        transform.position += dir.normalized * speed * Time.deltaTime;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

}
