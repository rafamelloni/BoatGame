using UnityEngine;
using System;

public class IslandManager : MonoBehaviour
{
    private bool _wasDefeated = false;
    private int _destroyedDefenses;

    [Header("Ammount of defenses Island has")]
    [SerializeField] public int _totalDefenses;

    [Header("Canvas Example")]
    [SerializeField] private GameObject _canvasExample;

    public event Action OnIslandDefeated;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        _totalDefenses = GetComponentsInChildren<IslandDefense>().Length;
        _wasDefeated = false;
        _destroyedDefenses = 0;

        if (_canvasExample != null)
            _canvasExample.SetActive(false);
    }

    public void RegisterDefenseDestroyed()
    {
        if (_wasDefeated) return;

        _destroyedDefenses++;

        if (_destroyedDefenses >= _totalDefenses)
        {
            _wasDefeated = true;
            IslandDefeated();
        }
    }

    public void IslandDefeated()
    {
        _canvasExample.SetActive(true);
        OnIslandDefeated?.Invoke();
    }

    public void ResetIsland()
    {
        Init();
    }

    public void SetCanvas(GameObject canvas)
    {
        _canvasExample = canvas;
    }
}