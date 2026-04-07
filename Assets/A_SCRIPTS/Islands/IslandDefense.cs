using UnityEngine;

public class IslandDefense : MonoBehaviour
{
    //cuando muere una defensa script//

    [SerializeField]IslandManager _islandManager;
    EnemyHealth _islandHealth;

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
            _islandManager._totalDefenses++;
        }
    }

    //se desubscribe a cuando muere 
    private void OnDisable()
    {
        if (_islandHealth != null)
            _islandHealth.OnDeath -= HandleDeath;
    }

    //Cuando muere llama al manager de su isla y le registra la muerte
    private void HandleDeath()
    {
        if (_islandManager != null)
            _islandManager.RegisterDefenseDestroyed();
    }


}
