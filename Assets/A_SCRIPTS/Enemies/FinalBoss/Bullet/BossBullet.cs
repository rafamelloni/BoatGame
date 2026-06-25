using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 _direction;

    public void Launch(Vector3 direction)
    {
        _direction = direction.normalized;
        _direction.y = 0f;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;
    }
}