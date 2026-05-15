using System.Collections;
using UnityEngine;

public class LifeBox : MonoBehaviour
{
    [SerializeField] ParticleSystem _particleSystem;
    [SerializeField] float _ammount = 25f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") )
        {
            other.gameObject.GetComponent<RT_PlayerHealth>().Heal(_ammount);
            if(_particleSystem != null)
                _particleSystem.Play();
            

            gameObject.SetActive(false);
        }
    }
}
