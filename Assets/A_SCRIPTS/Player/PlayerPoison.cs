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
            if (tickTimer >= tickRate)
            {
                tickTimer = 0f;
                _playerHealth.TakeDamage(damage);
            }
            if (zonesOverlapping <= 0)
            {
                enterAccum = 0f;
                poisonTimer -= Time.deltaTime;
                if (poisonTimer <= 0f)
                    SetPoisonActive(false);
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
        zonesOverlapping++;
    }
    public void ExitZone()
    {
        zonesOverlapping = Mathf.Max(0, zonesOverlapping - 1);
    }
}