using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IslandProfile", menuName = "Islands/Decoration Profile")]
public class IslandDecorationProfile : ScriptableObject
{
    [System.Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Range(0, 100)] public int weight = 10;
    }

    [Header("Vegetación")]
    public List<WeightedPrefab> vegetationPrefabs;

    [Header("Decoración")]
    public List<WeightedPrefab> decorationPrefabs;

    [Header("Defensas")]
    public List<WeightedPrefab> defensePrefabs;

    [Header("Reglas globales")]
    [Range(0f, 1f)] public float globalSpawnChance = 0.85f;

    public GameObject GetRandom(SpawnPointType type)
    {
        var list = type switch
        {
            SpawnPointType.Vegetation => vegetationPrefabs,
            SpawnPointType.Decoration => decorationPrefabs,
            SpawnPointType.Defense => defensePrefabs,
            _ => null
        };

        if (list == null || list.Count == 0) return null;
        return PickWeighted(list);
    }

    private GameObject PickWeighted(List<WeightedPrefab> list)
    {
        int totalWeight = 0;
        foreach (var item in list) totalWeight += item.weight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var item in list)
        {
            cumulative += item.weight;
            if (roll < cumulative) return item.prefab;
        }

        return list[^1].prefab; // fallback
    }
}