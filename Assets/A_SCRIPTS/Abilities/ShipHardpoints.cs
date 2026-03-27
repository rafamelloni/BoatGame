using UnityEngine;

public class ShipHardpoints : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform[] rightShootPoints;
    public Transform[] leftShootPoints;

    [Header("VFX Cannon Shoot")]
    public ParticleSystem _cannonSmokeShootR;
    public ParticleSystem _cannonSmokeShootL;
}
