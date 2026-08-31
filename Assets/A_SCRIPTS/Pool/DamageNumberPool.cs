using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pool para los numeros de daño flotante. Hermano de ParticlePool: mismo
// patron (singleton + Queue + coroutine que devuelve el objeto al pool),
// pero como el prefab es un popup de texto (no tiene un ParticleSystem con
// duracion propia), el tiempo de vida se define a mano con _lifetime.
//
// Se suscribe directo a EnemyHealth.OnAnyEnemyDamaged (el mismo patron que
// usa CoinSpawner con OnAnyEnemyDied) asi que funciona automaticamente para
// cualquier enemigo/boss/tier, sin tocar sus prefabs.
public class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance { get; private set; }

    [Header("Prefab (Canvas World Space + Text + DamageNumberPopup)")]
    [SerializeField] private GameObject _damageNumberPrefab;
    [SerializeField] private int _poolSize = 20;

    [Header("Timing")]
    [Tooltip("Cuanto tiempo (segundos) vive cada numero antes de volver al pool.")]
    [SerializeField] private float _lifetime = 0.8f;

    [Header("Posicion")]
    [Tooltip("Desplazamiento desde la posicion del enemigo donde aparece el numero.")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Jitter horizontal random para que golpes seguidos no se pisen encima.")]
    [SerializeField] private float _randomJitter = 0.3f;

    private readonly Queue<GameObject> _pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = Instantiate(_damageNumberPrefab, transform);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDamaged += HandleEnemyDamaged;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDamaged -= HandleEnemyDamaged;
    }

    private void HandleEnemyDamaged(Vector3 position, float damage)
    {
        if (_damageNumberPrefab == null) return;

        GameObject obj = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_damageNumberPrefab, transform);

        Vector3 jitter = new Vector3(Random.Range(-_randomJitter, _randomJitter), 0f, 0f);
        obj.transform.position = position + _spawnOffset + jitter;
        obj.SetActive(true);

        DamageNumberPopup popup = obj.GetComponent<DamageNumberPopup>();
        popup?.Show(damage);

        StartCoroutine(ReturnAfterTime(obj));
    }

    private IEnumerator ReturnAfterTime(GameObject obj)
    {
        yield return new WaitForSeconds(_lifetime);
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
