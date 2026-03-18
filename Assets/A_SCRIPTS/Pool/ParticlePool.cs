using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    [SerializeField] private List<GameObject> particlePrefabs; // Lista con los 3 prefabs
    [SerializeField] private int poolSizePerType = 10; // Cantidad de cada tipo en la pool

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Start()
    {
        // Crear una pool para cada tipo de partícula
        foreach (GameObject prefab in particlePrefabs)
        {
            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < poolSizePerType; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            poolDictionary.Add(prefab, pool);
        }
    }

    public GameObject GetParticle(GameObject prefab, Vector3 position)
    {
        if (poolDictionary.ContainsKey(prefab) && poolDictionary[prefab].Count > 0)
        {
            GameObject particle = poolDictionary[prefab].Dequeue();
            particle.transform.position = position;
            particle.SetActive(true);

            // Desactivar después de que termine la animación
            ParticleSystem ps = particle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                StartCoroutine(DeactivateAfterTime(particle, prefab, ps.main.duration));
            }

            return particle;
        }
        else
        {
            return null;
        }
    }

    private IEnumerator DeactivateAfterTime(GameObject obj, GameObject prefab, float time)
    {
        yield return new WaitForSeconds(time);
        obj.SetActive(false);
        poolDictionary[prefab].Enqueue(obj);
    }
}
