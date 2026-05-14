using UnityEngine;
using System;
using System.Collections;

public class IslandManager : MonoBehaviour
{
    private bool _wasDefeated = false;
    private int _destroyedDefenses;

    [Header("Ammount of defenses Island has")]
    [SerializeField] public int _totalDefenses;

    [Header("Canvas Example")]
    [SerializeField] private GameObject _canvasExample;

    [Header("Sink")]
    [SerializeField] private float _sinkDepth = 5f;
    [SerializeField] private float _shakeIntensity = 0.1f;

    public event Action OnIslandDefeated;
    public event Action<GameObject> OnReadyToReturn;

    public static event Action OnAnyIslandDefeated;

    [SerializeField] float _tiempoDesactivarIslas = 5f;
    IslandIndicator _indicator;
    ChestAnimation _chestAnim;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        _totalDefenses = GetComponentsInChildren<IslandDefense>().Length;
        _chestAnim = GetComponentInChildren<ChestAnimation>();
        _wasDefeated = false;
        _destroyedDefenses = 0;
        _indicator?.Show();

        //if (_canvasExample != null)
        //    _canvasExample.SetActive(false);
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
        _chestAnim.StopAnimation();
        _chestAnim.OpenChest();
        _canvasExample.SetActive(true);
        OnIslandDefeated?.Invoke();
        OnAnyIslandDefeated.Invoke();
        StartCoroutine(DeactivateIsland());
    }
    public void SetIndicator(IslandIndicator indicator)
    {
        _indicator = indicator;
    }
    public void ResetIsland()
    {
        Init();
    }

    public void SetCanvas(GameObject canvas)
    {
        _canvasExample = canvas;
    }

    private IEnumerator DeactivateIsland()
    {

        yield return StartCoroutine(SinkIsland());
        _indicator?.Hide();
        OnReadyToReturn?.Invoke(gameObject);
    }

    private IEnumerator SinkIsland()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, startPos.y - _sinkDepth, startPos.z);
        float t = 0f;

        while (t < _tiempoDesactivarIslas)
        {
            t += Time.deltaTime;
            float progress = t / _tiempoDesactivarIslas;

            float sinkY = Mathf.Lerp(startPos.y, targetPos.y, progress);
            float shakeX = Mathf.Sin(t * 20f) * _shakeIntensity * (1f - progress);
            float shakeZ = Mathf.Cos(t * 17f) * _shakeIntensity * (1f - progress);

            transform.position = new Vector3(startPos.x + shakeX, sinkY, startPos.z + shakeZ);
            yield return null;
        }

        transform.position = targetPos;
    }
}