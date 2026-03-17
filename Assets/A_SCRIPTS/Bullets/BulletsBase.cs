using UnityEngine;

public class BulletsBase : MonoBehaviour
{
   
    public ObjectPool<BulletsBase> Pool { protected get; set; }
    public virtual void TurnOn()
    {
        gameObject.SetActive(true);
    }
    public virtual void TurnOff()
    {
        gameObject.SetActive(false);
    }
}
