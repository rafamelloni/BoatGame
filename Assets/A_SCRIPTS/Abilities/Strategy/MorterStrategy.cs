using UnityEngine;
using System.Collections;


public class MorterStrategy : IAbilityStrategy
{
    private readonly SO_MorterData data;
    private readonly Transform ownerTransform;
    private readonly Transform shootPoint;
    private readonly CoroutineRunner runner;

    private float nextFireTime = 0f;

    public MorterStrategy(SO_MorterData data, Transform ownerTransform, Transform shootPoint, CoroutineRunner runner)
    {
        this.data = data;
        this.ownerTransform = ownerTransform;
        this.shootPoint = shootPoint;
        this.runner = runner;
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + data.cooldown;

        VisualShoot();
        SpawnRealProjectile();
    }

    private void VisualShoot()
    {
        if (data.visualProjectilePrefab == null || shootPoint == null) return;

        GameObject proj = Object.Instantiate(
            data.visualProjectilePrefab,
            shootPoint.position,
            Quaternion.identity
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // OJO: tu c�digo usaba linearVelocity; en Unity normal es velocity
            rb.linearVelocity = Vector3.up * data.visualShootForce;
        }

        runner.StartCoroutine(DisableAfterSeconds(proj, data.visualLifetime));
    }

    private IEnumerator DisableAfterSeconds(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private void SpawnRealProjectile()
    {
        if (data.realProjectilePrefab == null) return;

        // Direcci�n atr�s del jugador (plano XZ)
        Vector3 backDir = -ownerTransform.forward;
        backDir.y = 0f;
        backDir.Normalize();

        // Posici�n final de spawn
        Vector3 spawnPos =
            ownerTransform.position +
            backDir * data.backDistance +
            Vector3.up * data.height;

        // Random offset
        Vector2 rand = Random.insideUnitCircle * data.randomRadius;
        spawnPos += new Vector3(rand.x, 0f, rand.y);

        // Instanciamos
        GameObject real = Object.Instantiate(data.realProjectilePrefab, spawnPos, Quaternion.identity);

        // Setup en la instancia (importante)
        var barrel = real.GetComponent<BarrelExplosion>();
        if (barrel != null)
        {
            barrel.Setup(data.explosionVfx, data.circleVfx);
        }
    }
}
