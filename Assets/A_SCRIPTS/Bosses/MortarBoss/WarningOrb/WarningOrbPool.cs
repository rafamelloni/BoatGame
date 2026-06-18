using UnityEngine;

public class WarningOrbPool : MonoBehaviour
{
    [SerializeField] private WarningOrb _prefab;
    [SerializeField] private int _initialSize = 20;

    public static WarningOrbPool Instance { get; private set; }

    private ObjectPool<WarningOrb> _pool;

    private void Awake()
    {
        Instance = this;
        _pool = new ObjectPool<WarningOrb>(
            () => Instantiate(_prefab, transform),
            orb => orb.gameObject.SetActive(true),
            orb => orb.gameObject.SetActive(false),
            _initialSize
        );
    }

    public WarningOrb Get() => _pool.Get();
    public void Return(WarningOrb orb) => _pool.Return(orb);
}