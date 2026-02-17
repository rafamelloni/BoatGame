using UnityEngine;

public class BarrelExplosion : MonoBehaviour
{
    [SerializeField] GameObject explosionVfx;

    [SerializeField] GameObject circle;
    [SerializeField] float rayDistance = 5f;
    [SerializeField] LayerMask floorLayer;

    bool _hasSpawned = false;
    GameObject _circleInstance;


    public void Setup(GameObject Vfx, GameObject circleVfx)
    {
        explosionVfx = Vfx;
        circle = circleVfx;
        print("set");

    }

    private void Update()
    {
        if (_hasSpawned) return;

        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorLayer))
        {
            _circleInstance =  Instantiate(
                circle,
                hit.point,
                Quaternion.identity
            );
            _hasSpawned = true;
        }
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


            if (_circleInstance != null)
                Destroy(_circleInstance);
            Destroy(gameObject);
        }
    }

}
