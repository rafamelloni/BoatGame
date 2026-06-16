using UnityEngine;

[CreateAssetMenu(fileName = "SO_BASESTATS", menuName = "Scriptable Objects/SO_BASESTATS")]
public class SO_BASESTATS : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float turnSpeed = 90f;

    [Header("Inertia")]
    public float acceleration = 5f;
    public float deceleration = 4f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Ability")]
    public SelectedAbility ability;

    [Header("Crit")]
    public float critChance = 0f;     
    public float critMultiplier = 2f;

    [Header("Collisions")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.2f;
    public float damageWhenBumpingEnemies = 15f;
    public float damageWhenBumpingBoss = 25f;

    [Header("Coins")]
    public float pickUpRange = 10f;

    [Header("Block")]
    public float blockChance = 0f;
}
