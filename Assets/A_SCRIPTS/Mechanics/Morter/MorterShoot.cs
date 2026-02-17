using UnityEngine;
using System.Collections;


public class MorterShoot : MonoBehaviour
{

    [Header("Visual Spawn Settings")]
    [SerializeField] GameObject _projectilePrefab;
    [SerializeField] Transform _shootPoint;
    [SerializeField] float _shootForce = 15f;
    GameObject projectile;

    [Header("Real Spawn Settings")]
    [SerializeField] GameObject _realProjectilePrefab;
    [SerializeField] float _backDistance = 8f;
    [SerializeField] float _height = 12f;

    [Header("VFX")]
    [SerializeField] GameObject explosionVfx;
    [SerializeField] GameObject circle;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ViasualShoot();
            SpawnProjectile();
        }
    }

    void ViasualShoot()
    {
        GameObject proj = Instantiate(
            _projectilePrefab,
            _shootPoint.position,
            Quaternion.identity
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.up * _shootForce;

        StartCoroutine(DespawnBarrels(proj));
    }

    IEnumerator DespawnBarrels(GameObject proj)
    {
        yield return new WaitForSeconds(1.2f);

        if (proj != null)
            proj.SetActive(false);
    }
 
    void SpawnProjectile()
    {
        // Dirección atrás del jugador (plano XZ)
        Vector3 backDir = -transform.forward;
        backDir.y = 0f;
        backDir.Normalize();

        // Posición final de spawn
        Vector3 spawnPos =
            transform.position +
            backDir * _backDistance +
            Vector3.up * _height;

        float randomRadius = 2f;
        Vector2 rand = Random.insideUnitCircle * randomRadius;

        spawnPos += new Vector3(rand.x, 0f, rand.y);

        _realProjectilePrefab.GetComponent<BarrelExplosion>().Setup(explosionVfx, circle);
        Instantiate(_realProjectilePrefab, spawnPos, Quaternion.identity);
    }
}
