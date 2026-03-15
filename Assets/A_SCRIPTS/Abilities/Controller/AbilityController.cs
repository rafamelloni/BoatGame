using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [Header("Equip")]
    [SerializeField] private SO_CannonData _cannonsData;
    [SerializeField] private SO_MorterData _morterData;
    [SerializeField] private ParticleSystem[] _smokeParticle; //Bad done Particle sistem, not supposed to be here

    [Header("Mortar Shoot Point")]
    [SerializeField] private Transform _mortarShootPoint;

    private IAbilityStrategy _abilityE;
    private IAbilityStrategy _abilityQ;

    private void Awake()
    {
        // referencias del barco
        var hardpoints = GetComponent<ShipHardpoints>();
        var runner = GetComponent<CoroutineRunner>();

        if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();

        // crear estrategia
        _abilityE = new CannonStrategy(_cannonsData, hardpoints, runner);
        _abilityQ = new MorterStrategy(_morterData, transform, _mortarShootPoint, runner);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            _abilityE.TryExecute();
            foreach(var smokeParticle in _smokeParticle)
            {
                smokeParticle.Play();
            }
            
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _abilityQ.TryExecute();
        }
    }
}
