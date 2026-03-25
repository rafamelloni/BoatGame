using UnityEngine;

public class BasicEnemy : Enemy
{
    [Header("BasicEnemy Class Temporal")]
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

        if (leader != null && !leader.gameObject.activeInHierarchy)
        {
            leader = null;
        }

        Vector3 targetPos;

        if (leader != null)
            targetPos = leader.position + formationOffset;
        else
            targetPos = player.position;

        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(dir);

            // Como el frente de tu modelo es el eje rojo (X+) y no el azul (Z+),
            // corregimos con un offset de 90 grados.
            targetRot *= Quaternion.Euler(0f, -90f, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}