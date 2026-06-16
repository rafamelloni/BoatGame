using UnityEngine;

public class RT_CrossbowData
{
    public float damage;
    public float cooldown;
    public float arrowSpeed;
    public float detectionRadius;
    public int burstArrowCount;
    public float burstSpreadAngle;
    public float rotationSpeed;
    public GameObject explosionVfx;
    public GameObject waterVfx;

    public RT_CrossbowData(SO_CrossbowData data)
    {
        damage = data.damage;
        cooldown = data.cooldown;
        arrowSpeed = data.arrowSpeed;
        detectionRadius = data.detectionRadius;
        burstArrowCount = data.burstArrowCount;
        burstSpreadAngle = data.burstSpreadAngle;
        rotationSpeed = data.rotationSpeed;
        explosionVfx = data.explosionVfx;
        waterVfx = data.waterVfx;
    }
}