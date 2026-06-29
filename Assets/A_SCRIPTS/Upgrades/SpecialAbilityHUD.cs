using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class SpecialAbilityHUD : MonoBehaviour
{
    [SerializeField] private Image[] abilitySlots;
    [SerializeField] private GameObject[] cooldownContainers;
    [SerializeField] private Image[] cooldownBars;

    private int _unlockedCount = 0;
    private List<Func<float>> _progressGetters = new();

    public void UnlockNext(Sprite sprite, Func<float> progressGetter = null)
    {
        if (_unlockedCount >= abilitySlots.Length) return;
        if (sprite == null) return;

        abilitySlots[_unlockedCount].sprite = sprite;
        abilitySlots[_unlockedCount].gameObject.SetActive(true);

        if (_unlockedCount < cooldownContainers.Length && cooldownContainers[_unlockedCount] != null)
            cooldownContainers[_unlockedCount].SetActive(progressGetter != null);

        _progressGetters.Add(progressGetter);
        _unlockedCount++;
    }

    public int GetLastUnlockedIndex() => _unlockedCount - 1;

    public void AddProgressToIndex(int index, Func<float> progressGetter)
    {
        Debug.Log($"ENTRO AddProgressToIndex index={index} count={_progressGetters.Count}");


        if (index < 0 || index >= _progressGetters.Count) return;

        _progressGetters[index] = progressGetter;

        if (index < cooldownContainers.Length && cooldownContainers[index] != null)
        {
            cooldownContainers[index].SetActive(true);
           // Debug.Log($"[HUD] AddProgressToIndex {index} | container: {cooldownContainers[index].name} | active: {cooldownContainers[index].activeSelf}");

        }
        
    }

    public void ResetAll()
    {
        _unlockedCount = 0;
        _progressGetters.Clear();

        foreach (var slot in abilitySlots)
            slot.gameObject.SetActive(false);

        foreach (var container in cooldownContainers)
            if (container != null) container.SetActive(false);

        foreach (var bar in cooldownBars)
            if (bar != null) bar.fillAmount = 0f;
    }

    private void Update()
    {
        for (int i = 0; i < _progressGetters.Count; i++)
        {
            if (_progressGetters[i] == null) continue;
            if (i >= cooldownBars.Length || cooldownBars[i] == null) continue;
            float val = _progressGetters[i].Invoke();
            Debug.Log($"[HUD] slot {i} progress: {val} | container active: {cooldownContainers[i].activeSelf}");
            cooldownBars[i].fillAmount = val;
        }
    }
}