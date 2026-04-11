using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyUpgradeDisplay : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private float _duration = 1.5f;
    [SerializeField] private Vector2 _screenTarget = new Vector2(0.50f, 0.1f);
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    public void Play(Sprite icon, Vector3 worldStartPosition)
    {
        _iconImage.sprite = icon;
        StartCoroutine(FlyToCorner(worldStartPosition + _spawnOffset));
        Debug.Log("play");
    }

    private void Update()
    {
        transform.LookAt(Camera.main.transform);
    }

    private IEnumerator FlyToCorner(Vector3 worldStartPosition)
    {
        Vector2 startScreen = Camera.main.WorldToScreenPoint(worldStartPosition);
        Vector2 endScreen = new Vector2(
            Screen.width * _screenTarget.x,
            Screen.height * _screenTarget.y
        );

        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.zero;
        float elapsed = 0f;
        float fixedY = worldStartPosition.y;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Pow(elapsed / _duration, 2f);

            Vector2 currentScreen = Vector2.Lerp(startScreen, endScreen, t);
            transform.position = Camera.main.ScreenToWorldPoint(
                new Vector3(currentScreen.x, currentScreen.y,
                Camera.main.WorldToScreenPoint(worldStartPosition).z));

            transform.position = new Vector3(transform.position.x, fixedY, transform.position.z);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}