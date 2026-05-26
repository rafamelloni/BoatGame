using UnityEngine;
using UnityEngine.UI;

public class SpecialAbilityHUD : MonoBehaviour
{
    [SerializeField] private Image[] abilitySlots; // de abajo para arriba

    private int _unlockedCount = 0;

    public void UnlockNext(Sprite sprite)
    {
        if (_unlockedCount >= abilitySlots.Length) return;
        abilitySlots[_unlockedCount].sprite = sprite;
        abilitySlots[_unlockedCount].gameObject.SetActive(true);
        _unlockedCount++;
    }

    public void ResetAll()
    {
        _unlockedCount = 0;
        foreach (var slot in abilitySlots)
            slot.gameObject.SetActive(false);
    }
}