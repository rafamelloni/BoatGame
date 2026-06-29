using UnityEngine;

public class PlayerPoison : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float damage = 7f;
    [SerializeField] float tickRate = 0.5f;
    [SerializeField] float poisonDuration = 3f;
    [SerializeField] float cooldownTime = 5f;
    [SerializeField] float timeToPoison = 1.5f;
    [SerializeField] RT_PlayerHealth _playerHealth;

    [Header("VFX")]
    [SerializeField] GameObject poisonParticles;
    [SerializeField] GameObject poisonIcon;
    [SerializeField] float maxPoisonDuration = 5f;

    float totalPoisonTime = 0f;
    float poisonTimer;
    float cooldownTimer;
    float tickTimer;
    float enterAccum;
    int zonesOverlapping;

    bool IsPoisoned => poisonTimer > 0f;
    bool InCooldown => cooldownTimer > 0f;

    void Start()
    {
        SetPoisonActive(false);
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (IsPoisoned)
        {
            totalPoisonTime += Time.deltaTime;

            if (tickTimer >= tickRate)
            {
                tickTimer = 0f;
                _playerHealth.TakeDamage(damage);
            }

            if (zonesOverlapping <= 0 || totalPoisonTime >= maxPoisonDuration)
            {
                enterAccum = 0f;
                poisonTimer -= Time.deltaTime;

                if (poisonTimer <= 0f)
                {
                    poisonTimer = 0f;
                    totalPoisonTime = 0f;
                    cooldownTimer = cooldownTime;
                    zonesOverlapping = 0;
                    enterAccum = 0f;
                    SetPoisonActive(false);
                }
            }
        }
        else if (InCooldown)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
                cooldownTimer = 0f;
        }
        else
        {
            if (zonesOverlapping > 0)
            {
                enterAccum += Time.deltaTime;

                if (enterAccum >= timeToPoison)
                {
                    enterAccum = 0f;
                    poisonTimer = poisonDuration;
                    tickTimer = 0f;
                    SetPoisonActive(true);
                }
            }
            else
            {
                enterAccum = 0f;
            }
        }
    }

    void SetPoisonActive(bool active)
    {
        if (poisonParticles != null) poisonParticles.SetActive(active);
        if (poisonIcon != null) poisonIcon.SetActive(active);
    }

    public void EnterZone()
    {
        if (InCooldown) return;
        zonesOverlapping++;
    }

    public void ExitZone()
    {
        if (InCooldown) return;
        zonesOverlapping = Mathf.Max(0, zonesOverlapping - 1);
    }

    public void ResetPoison()
    {
        poisonTimer = 0f;
        cooldownTimer = 0f;
        tickTimer = 0f;
        enterAccum = 0f;
        totalPoisonTime = 0f;
        zonesOverlapping = 0;
        SetPoisonActive(false);

        // destruir todas las zonas activas en la escena
        var zones = FindObjectsByType<PoisonZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var zone in zones)
            Object.Destroy(zone.gameObject);
    }
}