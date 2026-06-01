using UnityEngine;
using static Unity.Collections.Unicode;

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

    [Header("Molotov")]
    [SerializeField] private SO_MolotovData _molotovData;
    [SerializeField] private Transform _molotovLaunchPoint;
    [SerializeField] private LayerMask _molotovEnemyLayers;
    public MolotovStrategy MolotovAbility => _molotovStrategy;

    [Header("Blades")]
    [SerializeField] private SO_BladesData _bladesData;
    public BladesStrategy BladesAbility => _abilityBlades;

    [Header("Cannon UI")]
    public GameObject cannonMesh;
    public GameObject cannonUI;
    public GameObject cannonTecla;
    public GameObject cooldownC;
    public bool _wasUCannon = false;

    [Header("Mortar UI")]
    public GameObject mortarGo;
    public GameObject mortargoAbilityUI;
    public GameObject mortargoAbilityNumber;
    public GameObject mortargoAbilityStats;
    public GameObject mortargoAbilityKey;
    public GameObject barra;
    public GameObject TEXTDamage;
    public GameObject cooldownM;
    public bool _wasUMortar = false;

    [Header("Ship Upgrade Positions")]
    [SerializeField] private GameObject _cannonR;
    [SerializeField] private GameObject _cannonL;
    [SerializeField] private GameObject _mortar;
    [SerializeField] private Transform _newCannonPosR;
    [SerializeField] private Transform _newCannonPosL;
    [SerializeField] private Transform _newMortarPos;

    [Header("Mortar VFX")]
    [SerializeField] private ParticleSystem _mortarE;

    [Header("Factories")]
    [SerializeField] private BulletFactory _bulletFactory;
    [SerializeField] private BulletFactory _barrelFactory;

    private CannonStrategy _abilityE;
    private MorterStrategy _abilityQ;
    private MolotovStrategy _molotovStrategy;
    private BladesStrategy _abilityBlades;

    public RT_PlayerUpgrades _playerUpgrades;

    public CannonStrategy CannonAbility => _abilityE;

    private void Awake()
    {
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();
        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        SetupCannon(hardpoints, runner);
        SetupMortar(runner);
        SetupMolotov(runner);
        SetupBlades(runner);
    }

    private void SetupCannon(ShipHardpoints hardpoints, CoroutineRunner runner)
    {
        _abilityE = new CannonStrategy(_cannonsData, hardpoints, runner, _bulletFactory,
            _camera, _recoilCannonR, _recoilCannonL, _playerUpgrades);
        _abilityE.OnCooldownStarted += _cannonCooldownUI.PlayCooldown;
    }

    private void SetupMortar(CoroutineRunner runner)
    {
        _abilityQ = new MorterStrategy(_morterData, _mortarShootPointReal, _mortarShootPoint,
            runner, _barrelFactory, _mortarE);
        _mortarChargeUI.Init(_morterData.cooldown, () => _abilityQ.RestoreCharge());
        _abilityQ.OnChargeConsumed += _ => _mortarChargeUI.OnShot();
    }

    private void SetupMolotov(CoroutineRunner runner)
    {
        _molotovStrategy = new MolotovStrategy(_molotovData, _molotovLaunchPoint, runner,
            _molotovEnemyLayers, _playerUpgrades, GetComponent<Collider>());
    }

    private void SetupBlades(CoroutineRunner runner)
    {
        _abilityBlades = new BladesStrategy(_bladesData, transform, runner, _playerUpgrades);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && _wasUCannon)
            _abilityE.TryExecute();

        if (Input.GetKeyDown(KeyCode.Q) && _wasUMortar)
            _abilityQ.TryExecute();

        _abilityBlades?.Tick();
    }

    public void BladesAvailable()
    {
        _abilityBlades.SetUnlocked(true);

        _abilityBlades.EnableBlades();
    }

    public void CannonAveilable()
    {
        _wasUCannon = true;
        cannonMesh.SetActive(true);
        cannonUI.SetActive(true);
        cooldownC.SetActive(true);
        _abilityE.SetUnlocked(true);
    }

    public void MortarAveilable()
    {
        _wasUMortar = true;
        mortarGo.SetActive(true);
        mortargoAbilityUI.SetActive(true);
        mortargoAbilityNumber.SetActive(true);
        mortargoAbilityStats.SetActive(true);
        mortargoAbilityKey.SetActive(true);
        cooldownM.SetActive(true);
        barra.SetActive(true);
        TEXTDamage.SetActive(true);
        _abilityQ.SetUnlocked(true);
    }

    public void ResetAbilities()
    {
        _wasUCannon = false;
        _wasUMortar = false;

        _cannonCooldownUI.TurnOff();
        _mortarChargeUI.TurnOff();

        _abilityE.ResetUpgrades();
        _abilityQ.ResetUpgrades();

        _abilityBlades.ResetUpgrades();

        cannonMesh.SetActive(false);
        cannonUI.SetActive(false);
     //   cannonTecla.SetActive(false);
        cooldownC.SetActive(false);

        mortarGo.SetActive(false);
        mortargoAbilityUI.SetActive(false);
        mortargoAbilityNumber.SetActive(false);
       // mortargoAbilityStats.SetActive(false);
        mortargoAbilityKey.SetActive(false);
       // barra.SetActive(false);
      //  TEXTDamage.SetActive(false);
        cooldownM.SetActive(false);
    }

    public void ShipUpgraded()
    {
        _cannonR.transform.position = _newCannonPosR.position;
        _cannonL.transform.position = _newCannonPosL.position;
        _mortar.transform.position = _newMortarPos.position;

        _recoilCannonR.UpdateLocalOrgin();
        _recoilCannonL.UpdateLocalOrgin();
    }
}