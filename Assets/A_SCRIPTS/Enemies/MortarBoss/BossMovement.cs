using UnityEngine;

public class BossMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;
    [SerializeField] private Transform _player;
    [SerializeField] private float _rotationOffset = 0f;
    public bool Paused { get; set; } = false;

    private void Update()
    {
        if (Paused || _player == null) return;

        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.position += dir.normalized * _speed * Time.deltaTime;

        // rota para que -X mire al player
        Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = lookRot * Quaternion.Euler(0f, _rotationOffset, 0f);
    }
}