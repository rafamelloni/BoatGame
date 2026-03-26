using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Data")]
public class SO_EnemyData : ScriptableObject
{
    public float maxHealth;
    public float fireRate;
    public GameObject woodExplosionVFX;
}
