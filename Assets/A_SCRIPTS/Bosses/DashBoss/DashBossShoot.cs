using UnityEngine;

public class DashBossShoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _SP0;
    [SerializeField] private Transform _SP1;
    [SerializeField] private Transform _SP2;
    [SerializeField] private Transform _SP0l;
    [SerializeField] private Transform _SP1l;
    [SerializeField] private Transform _SP2l;
    [SerializeField] private ParticleSystem _particle0;
    [SerializeField] private ParticleSystem _particle1;
    [SerializeField] private ParticleSystem _particle2;

    [Header("Data")]
    [SerializeField] private SO_CannonData _soCannonData;
    [SerializeField] private float _fireRate = 2f;

    private RT_CannonData _rtCannonData;
    private BulletFactory _bullets;
    private float _nextFireTime;
    private bool _canShoot;

    private Transform _player;

    public void SetPlayer(Transform player) => _player = player;

    private void Awake()
    {
        _rtCannonData = new RT_CannonData(_soCannonData);
        _bullets = GameObject.FindWithTag("IslandBulletFactory").GetComponent<BulletFactory>();
    }

    public void SetCanShoot(bool value)
    {
        _canShoot = value;
        if (value) _nextFireTime = Time.time + _fireRate;
    }

    public void TryShoot()
    {

        if (!_canShoot) return;
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + _fireRate;
        Shoot();
    }

    private void Shoot()
    {
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dot = Vector3.Dot(transform.right, toPlayer.normalized);

        if (dot >= 0)
        {
            Fire(_SP0, _particle0);
            Fire(_SP1, _particle1);
            Fire(_SP2, _particle2);
        }
        else
        {
            Fire(_SP0l, _particle0);
            Fire(_SP1l, _particle1);
            Fire(_SP2l, _particle2);
        }
    }

    private void Fire(Transform sp, ParticleSystem particle)
    {
        var b = _bullets.Create();
        b.GetComponent<CannonBulletIsland>().Setup(sp, _rtCannonData, -1);
        if (particle != null) particle.Play();
    }
}