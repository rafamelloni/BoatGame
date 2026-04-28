using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [Header("Cannon")]
    [SerializeField] private SO_CannonData _cannonsData;
    [SerializeField] private CooldownRadialUI _cannonCooldownUI;
    [SerializeField] private followPlayer _camera;
    [SerializeField] private CannonRecoil _recoilCannonR;
    [SerializeField] private CannonRecoil _recoilCannonL;

    [Header("Mortar")]
    [SerializeField] private SO_MorterData _morterData;
    [SerializeField] private Transform _mortarShootPoint;
    [SerializeField] private Transform _mortarShootPointReal;
    [SerializeField] private MortarChargeUI _mortarChargeUI;

    [Header("Factories")]
    [SerializeField] private BulletFactory _bulletFactory;
    [SerializeField] private BulletFactory _barrelFactory;

    [Header("Systems")]
    [SerializeField] private AbilityUpgradeSystem _upgradeSystem;

    private CannonStrategy _abilityE;
    private MorterStrategy _abilityQ;

    private void Awake()
    {
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();
        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        SetupCannon(hardpoints, runner);
        SetupMortar(runner);
    }

    private void SetupCannon(ShipHardpoints hardpoints, CoroutineRunner runner)
    {
        _abilityE = new CannonStrategy(_cannonsData, hardpoints, runner, _bulletFactory, _camera, _recoilCannonR, _recoilCannonL);
        _upgradeSystem.RegisterAbility(_abilityE);
        _abilityE.OnCooldownStarted += _cannonCooldownUI.PlayCooldown;
    }

    private void SetupMortar(CoroutineRunner runner)
    {
        _abilityQ = new MorterStrategy(_morterData, _mortarShootPointReal, _mortarShootPoint, runner, _barrelFactory);
        _mortarChargeUI.Init(_morterData.cooldown, () => _abilityQ.RestoreCharge());
        _abilityQ.OnChargeConsumed += _ => _mortarChargeUI.OnShot();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            _abilityE.TryExecute();

        if (Input.GetKeyDown(KeyCode.Q))
            _abilityQ.TryExecute();
    }

    private void OnDestroy()
    {
        _upgradeSystem.UnregisterAbility(_abilityE);
        //_upgradeSystem.UnregisterAbility(_abilityQ);
    }

    public void Upgrade()
    {
        _abilityE._rtData.shotsPerBurst = 4;
    }
}