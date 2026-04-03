using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

public class EnemyUpgradeDisplay : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private float _duration = 1.5f;
    // valor entre 0 y 1, ajustás en inspector. (0.85, 0.85) = esquina superior derecha
    [SerializeField] private Vector2 _screenTarget = new Vector2(0.85f, 0.85f);

    public void Play(Sprite icon, Vector3 worldStartPosition)
    {
        _iconImage.sprite = icon;
        StartCoroutine(FlyToCorner(worldStartPosition));
    }
    private void Update()
    {
        transform.LookAt(Camera.main.transform);
    }

    private IEnumerator FlyToCorner(Vector3 worldStartPosition)
    {
        // inicio: posicion del enemigo en pantalla, capturada UNA vez
        Vector2 startScreen = Camera.main.WorldToScreenPoint(worldStartPosition);

        // destino: punto fijo en pantalla, nunca cambia
        Vector2 endScreen = new Vector2(
            Screen.width * _screenTarget.x,
            Screen.height * _screenTarget.y
        );

        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Pow(elapsed / _duration, 2f);

            // mover en screen space
            Vector2 currentScreen = Vector2.Lerp(startScreen, endScreen, t);
            transform.position = Camera.main.ScreenToWorldPoint(
                new Vector3(currentScreen.x, currentScreen.y,
                Camera.main.WorldToScreenPoint(worldStartPosition).z));

            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}