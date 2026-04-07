using UnityEngine;
using System;

public class IslandManager : MonoBehaviour
{

    //scropot que se usa para detectar si la isla murioo y activar canvas
    private bool _wasDefeated = false;
    private int _destroyedDefenses;

    [Header("Ammount of defenses Island has")]
    [SerializeField] public int _totalDefenses;

    [Header("Canvas Example")]
    [SerializeField] private GameObject _canvasExample;

    public event Action OnIslandDefeated;

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
        print("activate canvas, sfx etc");
        _canvasExample.SetActive(true);
        OnIslandDefeated?.Invoke();


    }

    public void SetCanvas(GameObject canvas)
    {
        _canvasExample = canvas;
    }
}
