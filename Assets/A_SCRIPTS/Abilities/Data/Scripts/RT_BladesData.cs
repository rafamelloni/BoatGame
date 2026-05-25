public class RT_BladesData
{
    public float damage;
    public int bladeCount;
    public float orbitRadius;
    public float orbitSpeed;
    public float damageCooldownPerEnemy;
    public float erraticAmount;
    public float radiusVariation;
    public float noiseSpeed;
    public int burstCount;
    public float burstSpeed;
    public float burstMaxDistance;
    public float burstDamage;
    public float burstInterval;

    public RT_BladesData(SO_BladesData data)
    {
        damage = data.damage;
        bladeCount = data.bladeCount;
        orbitRadius = data.orbitRadius;
        orbitSpeed = data.orbitSpeed;
        damageCooldownPerEnemy = data.damageCooldownPerEnemy;
        erraticAmount = data.erraticAmount;
        radiusVariation = data.radiusVariation;
        noiseSpeed = data.noiseSpeed;
        burstCount = data.burstCount;
        burstSpeed = data.burstSpeed;
        burstMaxDistance = data.burstMaxDistance;
        burstDamage = data.burstDamage;
        burstInterval = data.burstInterval;
    }
}