using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IslandEntry
{
    public GameObject islandPrefab;
    public IslandDecorationProfile profile;
    [Range(0, 100)] public int weight = 10;
}

public class IslandSpawner : MonoBehaviour
{
    public List<IslandEntry> islands;
    public List<Transform> spawnLocations;
    [SerializeField] private GameObject _canvasExample;
    private void Start()
    {
        SpawnAllIslands();
    }

    private void SpawnAllIslands()
    {
        foreach (var location in spawnLocations)
        {
            IslandEntry entry = PickWeighted();
            if (entry == null) continue;

            GameObject instance = Instantiate(entry.islandPrefab, location.position, location.rotation);

            //temporal solo de pruieba
            IslandManager manager = instance.GetComponent<IslandManager>();
            if (manager != null)
            {
                manager.SetCanvas(_canvasExample);
                print("no null");
            }
            //temporal solo de pruieba

            // Busca los spawn points y asigna prefabs random
            var points = instance.GetComponentsInChildren<IslandSpawnPoint>();
            foreach (var point in points)
            {
                GameObject prefab = entry.profile.GetRandom(point.pointType);
                if (prefab == null) continue;

                GameObject spawned = Instantiate(prefab, point.transform.position, point.transform.rotation, point.transform);
                spawned.transform.localScale = prefab.transform.localScale * 75; // <- esto
            }
        }
    }

    private IslandEntry PickWeighted()
    {
        int total = 0;
        foreach (var i in islands) total += i.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;

        foreach (var i in islands)
        {
            cumulative += i.weight;
            if (roll < cumulative) return i;
        }

        return null;
    }
}