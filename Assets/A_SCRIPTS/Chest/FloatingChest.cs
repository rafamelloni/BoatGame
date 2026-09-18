using UnityEngine;

public class FloatingChest : MonoBehaviour, IDamageable
{
    [Header("Cómo se rompe")]
    [SerializeField] private string playerTag = "Player";

    [Header("Monedas")]
    [SerializeField] private CoinSpawner coinSpawner; // arrastrá el mismo CoinSpawner de la escena
    [SerializeField] private int minCoins = 5;
    [SerializeField] private int maxCoins = 10;
    [SerializeField] private float minHorizontalSpeed = 2.5f;
    [SerializeField] private float maxHorizontalSpeed = 5f;
    [SerializeField] private float minVerticalSpeed = 6f;
    [SerializeField] private float maxVerticalSpeed = 9f;
    [SerializeField] private float launchPower = 1.3f;

    [Header("Visual")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject brokenVisual; // opcional
    [SerializeField] private float destroyDelay = 2f;

    private bool _broken;

    public void TakeDamage(float damage)
    {
        Break();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag)) Break();
    }

    private void Break()
    {
        if (_broken) return;
        _broken = true;

        int count = Random.Range(minCoins, maxCoins + 1);
        coinSpawner.SpawnFromChest(transform.position, count, minHorizontalSpeed, maxHorizontalSpeed, minVerticalSpeed, maxVerticalSpeed, launchPower);

        if (closedVisual != null) closedVisual.SetActive(false);
        if (brokenVisual != null) brokenVisual.SetActive(true);

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, destroyDelay);
    }
}