using UnityEngine;

public class BasicEnemyBullet : BulletsBase
{
    [Header("Data")]
    [SerializeField] private float _speed;
    [SerializeField] private float _lifeTime = 5f;

    private float _currentTime;

    private void OnEnable()
    {
        _currentTime = _lifeTime;
    }
    void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;
        _currentTime -= Time.deltaTime;

        if (_currentTime <= 0f)
        {
            Pool.Return(this);
        }

    }
}
