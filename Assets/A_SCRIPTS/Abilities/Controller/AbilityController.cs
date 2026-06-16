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

    [Header("Molotov")]
    [SerializeField] private SO_MolotovData _molotovData;
    [SerializeField] private Transform _molotovLaunchPoint;
    [SerializeField] private LayerMask _molotovEnemyLayers;
    public MolotovStrategy MolotovAbility => _molotovStrategy;

    [Header("Crossbow")]
    [SerializeField] private SO_CrossbowData _crossbowData;
    [SerializeField] private Transform _crossbowLaunchPoint;
    [SerializeField] private LayerMask _crossbowEnemyLayers;
    [SerializeField] private GameObject _crossbowGO;
    public CrossbowStrategy CrossbowAbility => _crossbowStrategy;

    [Header("Blades")]
    [SerializeField] private SO_BladesData _bladesData;
    public BladesStrategy BladesAbility => _abilityBlades;

    [Header("Cannon UI")]
    public GameObject cannonMesh;
    public GameObject cannonTecla;
    public GameObject cooldownC;
    public bool _wasUCannon = false;

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
    private MolotovStrategy _molotovStrategy;
    private CrossbowStrategy _crossbowStrategy;
    private BladesStrategy _abilityBlades;

    public RT_PlayerUpgrades _playerUpgrades;
    public PlayerUpgrades _islandUpgrades;
    public CannonStrategy CannonAbility => _abilityE;

    private void Awake()
    {
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();
        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        SetupCannon(hardpoints, runner);
        SetupMolotov(runner);
        SetupCrossbow(runner);
        SetupBlades(runner);
    }

    private void SetupCannon(ShipHardpoints hardpoints, CoroutineRunner runner)
    {
        _abilityE = new CannonStrategy(_cannonsData, hardpoints, runner, _bulletFactory,
            _camera, _recoilCannonR, _recoilCannonL, _playerUpgrades, _islandUpgrades);
        _abilityE.OnCooldownStarted += _cannonCooldownUI.PlayCooldown;
    }

    private void SetupMolotov(CoroutineRunner runner)
    {
        _molotovStrategy = new MolotovStrategy(_molotovData, _molotovLaunchPoint, runner,
            _molotovEnemyLayers, _playerUpgrades, GetComponent<Collider>());
    }

    private void SetupCrossbow(CoroutineRunner runner)
    {
        _crossbowStrategy = new CrossbowStrategy(_crossbowData, _crossbowLaunchPoint,
            runner, _crossbowEnemyLayers, _playerUpgrades, _crossbowGO);
    }

    private void SetupBlades(CoroutineRunner runner)
    {
        _abilityBlades = new BladesStrategy(_bladesData, transform, runner, _playerUpgrades);
    }

    private void Update()
    {
        if (Input.GetMouseButton(1) && _wasUCannon)
            _abilityE.TryExecute();

        _abilityBlades?.Tick();
    }

    public void BladesAvailable()
    {
        _abilityBlades.SetUnlocked(true);
        _abilityBlades.EnableBlades();
    }

    public void CrossbowAvailable()
    {
        _crossbowStrategy.SetUnlocked(true);
    }

    public void CannonAveilable()
    {
        _wasUCannon = true;
        cannonMesh.SetActive(true);
        cooldownC.SetActive(true);
        _abilityE.SetUnlocked(true);
    }

    public void ResetAbilities()
    {
        _wasUCannon = false;
        _cannonCooldownUI.TurnOff();
        _abilityE.ResetUpgrades();
        _molotovStrategy.ResetUpgrades();
        _crossbowStrategy.ResetUpgrades();
        _abilityBlades.ResetUpgrades();
        cannonMesh.SetActive(false);
        cooldownC.SetActive(false);
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