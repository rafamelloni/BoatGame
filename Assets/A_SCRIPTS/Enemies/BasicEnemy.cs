using UnityEngine;

public class BasicEnemy : Enemy
{
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    private void Update()
    {
        if (player == null) return;

        // Dirección hacia el player (sin Y para que no mire arriba/abajo)
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;

        // Movimiento
        transform.position += dir.normalized * speed * Time.deltaTime;

        // Rotación suave hacia el player
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
