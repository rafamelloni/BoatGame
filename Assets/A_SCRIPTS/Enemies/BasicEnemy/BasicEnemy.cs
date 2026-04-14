using UnityEngine;

public class BasicEnemy : Enemy
{
    [Header("BasicEnemy Class Temporal")]
    [SerializeField] private Transform leader;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

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

        // Siempre rotar hacia el player cuando esta parado
        Vector3 rotDir = _stopped
            ? player.position - transform.position
            : dir;

        rotDir.y = 0f;

        if (rotDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rotDir);
            targetRot *= Quaternion.Euler(0f, -90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (!_stopped && dir.sqrMagnitude > 0.01f)
            transform.position += dir.normalized * speed * Time.deltaTime;
    }
}