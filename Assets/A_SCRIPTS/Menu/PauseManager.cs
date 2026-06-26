using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _ExitPanel;
    [SerializeField] private GameObject[] _objectsToDeactivate;

    private bool _paused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && Time.timeScale !=0f || Input.GetKeyDown(KeyCode.Escape) && _pausePanel.activeSelf)
            SetPause(!_paused);
    }

    public void SetPause(bool pause)
    {
        _paused = pause;
        Time.timeScale = _paused ? 0f : 1f;

        _pausePanel.SetActive(_paused);
        _ExitPanel.SetActive(_paused);

        foreach (var go in _objectsToDeactivate) 
            go.SetActive(!_paused);
    }
}   