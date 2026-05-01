using UnityEngine;
using static UnityEngine.ParticleSystem;

public class IslandDefense : MonoBehaviour
{
    //cuando muere una defensa script//
    [SerializeField] private float thresholdPercent = 0.5f;
    [SerializeField]IslandManager _islandManager;

    [Header("Objects Mesh/Paricles")]
    [SerializeField]GameObject _normalMesh;
    [SerializeField]GameObject _normalMeshCatapult;
    [SerializeField] GameObject _lowMesh;
    [SerializeField] GameObject _lowMeshCatapult;
    [SerializeField] ParticleSystem _particlesRocks;
    [SerializeField] ParticleSystem _particlesSmoke;
    [SerializeField] ParticleSystem _particlesFire;

    EnemyHealth _islandHealth;


    private bool _thresholdTriggered = false;

    private void Awake()
    {
        _islandHealth = GetComponent<EnemyHealth>();
        _islandManager = GetComponentInParent<IslandManager>();
    }

    //se subscribe a cuando muere 
    private void OnEnable()
    {
        if (_islandHealth != null) 
        {
            _islandHealth.OnDeath += HandleDeath;
            _islandHealth.OnDamage += HandleDamage;

        }
    }

    //se desubscribe a cuando muere 
    private void OnDisable()
    {
        if (_islandHealth != null)
        {
            _islandHealth.OnDeath -= HandleDeath;
            _islandHealth.OnDamage += HandleDamage;

            _normalMesh.SetActive(true);
            _lowMesh.SetActive(false);

            //Catapult
            _lowMeshCatapult.SetActive(false);
            _normalMeshCatapult.SetActive(true);
        }
        _thresholdTriggered = false;
    }

    //Cuando muere llama al manager de su isla y le registra la muerte
    private void HandleDeath()
    {
        if (_islandManager != null)
            _islandManager.RegisterDefenseDestroyed();
        _particlesRocks.Play();


    }

    private void HandleDamage(float currentHealth)
    {
        _particlesRocks.Play();
        if (_thresholdTriggered) return;
        

        if (_islandHealth.GetHealthNormalized() <= thresholdPercent)
        {
            _thresholdTriggered = true;
            // lo que quieras que pase acá
            
            _particlesRocks.Play();
            _particlesSmoke.Play();
            _particlesFire.Play();

            //Catapult
            

        }
    }


}
