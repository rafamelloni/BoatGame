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


    [Header("Mortar Data To Activate")]
    public GameObject mortarGo;
    public GameObject mortargoAbilityUI;
    public GameObject mortargoAbilityNumber;
    public GameObject mortargoAbilityStats;
    public GameObject mortargoAbilityKey;
    public GameObject barra;
    public GameObject TEXTDamage;
    public bool _wasUMortar = false;

    [Header("Cannon Data To Activate")]
    public GameObject cannonMesh;
    public GameObject cannonUI;
    public GameObject cannonTecla;
    public GameObject cannonBarra;
    public GameObject cannonText;
    public bool _wasUCannon = false;


    [Header("hardcodeado insta")]
    [SerializeField] private GameObject _mortarPhoto;
    [SerializeField] private GameObject _mortarText;
    [SerializeField] private GameObject _mortarMesh;
    [SerializeField] private ParticleSystem _mortarE;

    [Header("Factories")]
    [SerializeField] private BulletFactory _bulletFactory;
    [SerializeField] private BulletFactory _barrelFactory;

    [Header("Systems")]
    [SerializeField] private AbilityUpgradeSystem _upgradeSystem;
    [SerializeField] private UpgradeStatsUI _statsUI;

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

        _statsUI.RegisterBase("Cannon", StatType.Damage, _cannonsData.damage);
        _statsUI.RegisterBase("Cannon", StatType.Cooldown, _cannonsData.cooldown);
        _statsUI.RegisterBase("Cannon", StatType.FireRate, _cannonsData.timeBetweenShots);
    }

    private void SetupMortar(CoroutineRunner runner)
    {
        _abilityQ = new MorterStrategy(_morterData, _mortarShootPointReal, _mortarShootPoint, runner, _barrelFactory, _mortarE);
        _mortarChargeUI.Init(_morterData.cooldown, () => _abilityQ.RestoreCharge());
        _abilityQ.OnChargeConsumed += _ => _mortarChargeUI.OnShot();
        _upgradeSystem.RegisterAbility(_abilityQ);

        _statsUI.RegisterBase("Mortar", StatType.Damage, _morterData.damage);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && _wasUCannon)
            _abilityE.TryExecute();

        if (Input.GetKeyDown(KeyCode.Q) && _wasUMortar)
            _abilityQ.TryExecute();
    }

    private void OnDestroy()
    {
        _upgradeSystem.UnregisterAbility(_abilityE);
        _upgradeSystem.UnregisterAbility(_abilityQ);
    }
    public void Upgrade()
    {
        _abilityE._rtData.shotsPerBurst = 4;
    }

    public void MortarAveilable()
    {
        _wasUMortar = true;
        mortarGo.SetActive(true);


        mortargoAbilityUI.SetActive(true);
        mortargoAbilityNumber.SetActive(true);
        mortargoAbilityStats.SetActive(true);
        mortargoAbilityKey.SetActive(true);
       
        
        barra.SetActive(true);
        TEXTDamage.SetActive(true);
        LetMortarBeUpgraded();
    }

    public void CannonAveilable()
    {
        _wasUCannon = true;
        cannonMesh.SetActive(true);
        cannonUI.SetActive(true);
        cannonTecla.SetActive(true);
        cannonBarra.SetActive(true);
        cannonText.SetActive(true);

        LetCannonBeUpgraded();

}
    public void LetCannonBeUpgraded()
    {
        _abilityE.SetUnlocked(true);
        Debug.Log($"Mortar unlocked: {_abilityQ.IsUnlocked}");
    }
    public void LetMortarBeUpgraded()
    {
        _abilityQ.SetUnlocked(true);
        Debug.Log($"Mortar unlocked: {_abilityQ.IsUnlocked}");
    }

    public void ResetAbilities()
    {
        _wasUMortar = false;
        _mortarPhoto.SetActive(false);
        _mortarText.SetActive(false);
        _mortarMesh.SetActive(false);
    }
}