using UnityEngine;

public class BulletFactory : Factory<BulletsBase>
{
    public BulletsBase prefab;
    ObjectPool<BulletsBase> _pool;
    public void Awake()
    {
        _pool = new ObjectPool<BulletsBase>(InstatiatiePrebaf, TurnOn, TurnOff, 16);
    }

    void TurnOn(BulletsBase bullet)
    {
        bullet.TurnOn();
    }
    void TurnOff(BulletsBase bullet)
    {
        bullet.TurnOff();
    }
    BulletsBase InstatiatiePrebaf()
    {
        return Instantiate(prefab);
    }
    public override BulletsBase Create()
    {
        var b = _pool.Get();
        b.Pool = _pool;
        return b;
    }
}
