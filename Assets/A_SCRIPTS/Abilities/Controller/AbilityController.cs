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
    public GameObject cooldownM;

    public bool _wasUMortar = false;

    [Header("Cannon Data To Activate")]
    public GameObject cannonMesh;
    public GameObject cannonUI;
    public GameObject cannonTecla;
    public GameObject cannonBarra;
    public GameObject cannonText;
    public GameObject cooldownC;
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

    [Header("Data para cuando matemos al Boss")]
    [SerializeField] private GameObject _cannonR;
    [SerializeField] private GameObject _cannonL;
    [SerializeField] private GameObject _mortar;
    [SerializeField] private Transform _newCannonPosR;
    [SerializeField] private Transform _newCannonPosL;
    [SerializeField] private Transform _newMortarPos;





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
        cooldownM.SetActive(true);
       
        
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
        cooldownC.SetActive(true);

        LetCannonBeUpgraded();

}


    public void ResetAbilities()
    {
        // Flags
        _wasUCannon = false;
        _wasUMortar = false;

        _cannonCooldownUI.TurnOff();
        _mortarChargeUI.TurnOff();

        // Cannon
        cannonMesh.SetActive(false);
        cannonUI.SetActive(false);
        cannonTecla.SetActive(false);
        cannonBarra.SetActive(false);
        cannonText.SetActive(false);
        cooldownC.SetActive(false);


        // Mortar
        mortarGo.SetActive(false);
        mortargoAbilityUI.SetActive(false);
        mortargoAbilityNumber.SetActive(false);
        mortargoAbilityStats.SetActive(false);
        mortargoAbilityKey.SetActive(false);
        barra.SetActive(false);
        TEXTDamage.SetActive(false);
        cooldownM.SetActive(false);

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

    public void ShipUpgraded()
    {
        _cannonR.transform.position = _newCannonPosR.transform.position;
        _cannonL.transform.position = _newCannonPosL.transform.position;
        _mortar.transform.position = _newMortarPos.transform.position;


        _recoilCannonR.UpdateLocalOrgin();
        _recoilCannonL.UpdateLocalOrgin();
    }
}