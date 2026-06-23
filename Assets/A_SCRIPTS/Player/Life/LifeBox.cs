using UnityEngine;

public class LifeBox : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private ParticleSystem _lifeParticlesPrefab;
    [SerializeField] private float _amount = 25f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<RT_PlayerHealth>().Heal(_amount);

            if (_particleSystem != null)
                _particleSystem.Play();

            if (_lifeParticlesPrefab != null)
            {
                ParticleSystem particles = Instantiate(
                    _lifeParticlesPrefab,
                    other.transform.position,
                    Quaternion.identity
                );

                particles.Play();

                Destroy(
                    particles.gameObject,
                    particles.main.duration + particles.main.startLifetime.constantMax
                );
            }

            gameObject.SetActive(false);
        }
    }
}