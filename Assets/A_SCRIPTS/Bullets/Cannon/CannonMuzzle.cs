using UnityEngine;

public class CannonMuzzle : MonoBehaviour
{
    [SerializeField] private ParticleSystem _smoke;
    [SerializeField] private CannonRecoil _recoil;

    public void Fire(float recoilTime)
    {
        if (_smoke != null) _smoke.Play();
        if (_recoil != null) _recoil.Fire(recoilTime);
    }
}