using UnityEngine;

public class CannonBullet : MonoBehaviour
{
    [SerializeField] GameObject explosionVfx;

    public void Setup(GameObject Vfx)
    {
        explosionVfx = Vfx;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            ContactPoint contact = collision.contacts[0];

            Instantiate(
                explosionVfx,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );


            Destroy(gameObject);
        }
    }

}
