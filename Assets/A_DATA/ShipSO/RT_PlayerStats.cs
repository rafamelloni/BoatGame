using UnityEngine;

public class RT_PlayerStats : MonoBehaviour
{
    [SerializeField] private SO_BASESTATS baseStats;

    [Header("Runtime Stats ")]
    [Tooltip("Esta es la data del barco en todo momento, al inicio lee la data del SO y luego este script va a ser modificado por todos ya que va a ser los datos reales del barco")]

    public float moveSpeed;
    public float turnSpeed;
    public float acceleration;
    public float deceleration;
    public float sprintMultiplier;
    public float maxHealth;
    public float currentHealth;

    private void Awake()
    {
        ResetToBase();
    }

    public void ResetToBase()
    {
        if (baseStats == null)
        {
            Debug.LogError("ShipStats: falta asignar SO_BASESTATS.");
            return;
        }

        moveSpeed = baseStats.moveSpeed;
        turnSpeed = baseStats.turnSpeed;
        acceleration = baseStats.acceleration;
        deceleration = baseStats.deceleration;
        sprintMultiplier = baseStats.sprintMultiplier;

        maxHealth = baseStats.maxHealth;
        currentHealth = baseStats.maxHealth;
    }

    public void SetBaseStats(SO_BASESTATS stats)
    {
        moveSpeed = stats.moveSpeed;
        turnSpeed = stats.turnSpeed;
        acceleration = stats.acceleration;
        deceleration = stats.deceleration;
        sprintMultiplier = stats.sprintMultiplier;
        maxHealth = stats.maxHealth;
        currentHealth = stats.maxHealth;
    }
}
