using UnityEngine;

public class BasicEnemyBullet : BulletsBase
{
    [SerializeField] private float _speed;
    void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;

    }
}
