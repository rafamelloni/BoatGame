using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [Header("Equip")]
    [SerializeField] private SO_CannonData cannonsData;
    [SerializeField] private SO_MorterData morterData;

    [Header("Mortar Shoot Point")]
    [SerializeField] private Transform mortarShootPoint;

    private IAbilityStrategy abilityE;
    private IAbilityStrategy abilityQ;

    private void Awake()
    {
        // referencias del barco
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();

        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        // crear estrategia
        abilityE = new CannonStrategy(cannonsData, hardpoints, runner);
        abilityQ = new MorterStrategy(morterData, transform, mortarShootPoint, runner);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            abilityE.TryExecute();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            abilityQ.TryExecute();
        }
    }
}
