using UnityEngine;

public class HeavyIslandBullet : BulletsBase
{
    [Header("Data")]
    [SerializeField] private float _speed;
    [SerializeField] private float _lifeTime = 5f;
    private float _currentTime;

    private void OnEnable()
    {
        _currentTime = _lifeTime;
    }

    private void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;
        _currentTime -= Time.deltaTime;
    }
 }
