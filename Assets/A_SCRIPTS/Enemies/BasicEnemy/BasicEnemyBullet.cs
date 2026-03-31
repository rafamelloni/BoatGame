using UnityEngine;

public class BasicEnemyBullet : BulletsBase
{
    [SerializeField] private float _speed;
    void Update()
    {
        transform.position += transform.right * _speed * Time.deltaTime;

    }
}
