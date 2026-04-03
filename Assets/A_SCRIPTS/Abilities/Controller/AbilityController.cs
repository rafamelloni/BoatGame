using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [Header("Equip")]
    [SerializeField] private SO_CannonData _cannonsData;
    [SerializeField] private SO_MorterData _morterData;
    [SerializeField] private CooldownRadialUI cannonCooldownUI;
    [SerializeField] private AbilityUpgradeSystem _upgradeSystem;



    [Header("Mortar Shoot Point")]
    [SerializeField] private Transform _mortarShootPoint;
    [SerializeField] private Transform _mortarShootPointReal;

    [Header("Bullet Factory/Partivle pool")]
    [SerializeField] private BulletFactory _bulletFactory;
    [SerializeField] private BulletFactory _barrelFactory;




    private CannonStrategy _abilityE;
    private MorterStrategy _abilityQ;

    private void Awake()
    {
        // referencias del barco
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();

        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        // crear estrategia
        _abilityE = new CannonStrategy(_cannonsData, hardpoints, runner, _bulletFactory);
        _upgradeSystem.RegisterAbility(_abilityE);
        _abilityE.OnCooldownStarted += cannonCooldownUI.PlayCooldown;

        _abilityQ = new MorterStrategy(_morterData, _mortarShootPointReal, _mortarShootPoint, runner, _barrelFactory);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            _abilityE.TryExecute();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _abilityQ.TryExecute();
        }
    }

    private void OnDestroy()
    {
        _upgradeSystem.UnregisterAbility(_abilityE);
    }

}
