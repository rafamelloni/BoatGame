using UnityEngine;

// Bala recta y lenta de la TorreEspiral. Va a la altura con la que sale, en línea recta.
// Daña al jugador al tocarlo. La torre la reusa (pool propio), no hace falta BulletFactory.
// Necesita un Collider (se pone en Trigger solo). Si la torre no tiene prefab, arma una esfera sola.
public class TowerProjectile : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("Se rompe al tocar una isla")]
    [SerializeField] private bool _destroyOnIslands = true;
    [SerializeField] private string _islandTag = "Islas";
    [Tooltip("Partícula al pegarle al jugador (tiene que estar en el ParticlePool). Opcional.")]
    [SerializeField] private GameObject _hitVfx;

    private Vector3 _dir;
    private float _speed;
    private float _damage;
    private float _life;
    private TrailRenderer _trail;

    public bool IsFree => !gameObject.activeSelf;

    private void Awake()
    {
        _trail = GetComponentInChildren<TrailRenderer>(true);

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // con Rigidbody kinematic los triggers andan bien aunque se mueva por transform
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, float damage, float lifetime, float size)
    {
        direction.y = 0f;
        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _speed = speed;
        _damage = damage;
        _life = lifetime;

        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(_dir, Vector3.up));
        transform.localScale = Vector3.one * size;
        gameObject.SetActive(true);
        if (_trail != null) { _trail.Clear(); _trail.emitting = true; }
    }

    private void Update()
    {
        transform.position += _dir * (_speed * Time.deltaTime);
        _life -= Time.deltaTime;
        if (_life <= 0f) Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag))
        {
            var health = other.GetComponentInParent<RT_PlayerHealth>();
            if (health != null) health.TakeDamage(_damage);
            if (_hitVfx != null && ParticlePool.Instance != null)
                ParticlePool.Instance.GetParticle(_hitVfx, transform.position);
            Despawn();
        }
        else if (_destroyOnIslands && other.CompareTag(_islandTag))
        {
            Despawn();
        }
    }

    public void Despawn()
    {
        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
        gameObject.SetActive(false);
    }
}
