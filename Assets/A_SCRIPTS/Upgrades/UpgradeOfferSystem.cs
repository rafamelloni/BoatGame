using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeOfferSystem : MonoBehaviour
{
    [SerializeField] private SO_UpgradePath[] allPaths;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;

    public List<SO_UpgradePath> GetOffer(int count = 3)
    {
        List<SO_UpgradePath> available = allPaths
            .Where(p => playerUpgrades.CanUpgrade(p))
            .ToList();

        // Fisher-Yates shuffle
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        return available.Take(count).ToList();
    }
}