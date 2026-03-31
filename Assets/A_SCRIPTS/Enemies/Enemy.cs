using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    protected SO_EnemyData _baseData;

    [Header("Components From Enemy BaseClass")]
    public SO_EnemyData baseData;
    [SerializeField] protected EnemyHealth _enemyHealth;

    protected virtual void Awake()
    {
        Init(baseData);
    }

    public virtual void Init(SO_EnemyData data)
    {
        _baseData = data;

        if (_enemyHealth != null)
        {
            print("enemt init");
            _enemyHealth.InitializeComponent(_baseData);
        }

        OnInit();
    }

    protected virtual void OnInit()
    {
    }
}
