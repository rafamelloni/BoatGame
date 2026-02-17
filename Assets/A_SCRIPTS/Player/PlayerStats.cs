using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private SO_BASESTATS baseStats;

    [Header("Runtime Stats ")]
    [Tooltip("Esta es la data del barco en todo momento, al inicio lee la data del SO y luego este script va a ser modificado por todos ya que va a ser los datos reales del barco")]

    public float moveSpeed;
    public float turnSpeed;
    public float acceleration;
    public float deceleration;

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
    }
}
