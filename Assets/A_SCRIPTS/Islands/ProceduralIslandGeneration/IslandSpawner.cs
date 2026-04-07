using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

            // temporal
            IslandManager manager = instance.GetComponent<IslandManager>();
            if (manager != null)
            {
                manager.SetCanvas(_canvasExample);
                print("no null");
            }
            // temporal

            var allPoints = instance.GetComponentsInChildren<IslandSpawnPoint>();

            // Puntos que NO son defensa, se spawnean todos
            var normalPoints = allPoints.Where(p => p.pointType != SpawnPointType.Defense);

            // Puntos de defensa, solo 2 random
            var defensePoints = allPoints
                .Where(p => p.pointType == SpawnPointType.Defense)
                .OrderBy(_ => Random.value)
                .Take(2);

            foreach (var point in normalPoints.Concat(defensePoints))
            {
                GameObject prefab = entry.profile.GetRandom(point.pointType);
                if (prefab == null) continue;

                GameObject spawned = Instantiate(prefab, point.transform.position,
                                                 point.transform.rotation, instance.transform);
                float parentScale = instance.transform.lossyScale.x;
                float prefabScale = prefab.transform.localScale.x;
                spawned.transform.localScale = Vector3.one * (prefabScale / parentScale);
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