using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Menú principal en su propia escena. Play: la UI se va a la izquierda, el barco escapa hacia la
// derecha bajo un bombardeo (MenuPlaySequence) y cuando sale de pantalla se carga el juego,
// que arranca directo en la preselección de barco. La carga recién empieza con la pantalla
// en negro (pantalla de carga), para que la animación no pierda frames.
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Nombre de la escena del juego (tiene que estar en Build Settings)")]
    [SerializeField] private string _gameScene = "TestScene";

    [Header("Fundido (opcional)")]
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private float _fadeDuration = 0.4f;

    [Tooltip("Texto de carga (opcional). Vacío = se crea uno abajo a la derecha dentro del Fade.")]
    [SerializeField] private TMP_Text _loadingLabel;

    [Tooltip("Secuencia de salida. Vacío = se busca o se agrega sola en este objeto.")]
    [SerializeField] private MenuPlaySequence _sequence;

    private bool _loading;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (_fade != null)
        {
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
        }
    }

    public void Play()
    {
        if (_loading) return;

        if (!Application.CanStreamedLevelBeLoaded(_gameScene))
        {
            Debug.LogError($"[MainMenu] La escena '{_gameScene}' no está en Build Settings.");
            return;
        }

        StartCoroutine(LoadGame());
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadGame()
    {
        _loading = true;

        // 1. Secuencia del menú (UI afuera, barco escapando, bombardeo). Sin cargar nada todavía,
        //    así la animación va fluida.
        if (_sequence == null) _sequence = FindFirstObjectByType<MenuPlaySequence>();
        if (_sequence == null) _sequence = gameObject.AddComponent<MenuPlaySequence>();

        bool sequenceDone = false;
        _sequence.Play(() => sequenceDone = true);
        while (!sequenceDone) yield return null;

        // 2. Pantalla de carga: fundido a negro + "Cargando"
        TMP_Text label = null;
        if (_fade != null)
        {
            _fade.transform.SetAsLastSibling();
            _fade.gameObject.SetActive(true);
            _fade.blocksRaycasts = true;
            label = GetOrCreateLoadingLabel();

            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _fade.alpha = Mathf.Clamp01(t / _fadeDuration);
                yield return null;
            }
            _fade.alpha = 1f;
        }

        // 3. Recién ahora carga el juego, con la pantalla tapada
        yield return null;
        Application.backgroundLoadingPriority = ThreadPriority.High;
        AsyncOperation op = SceneManager.LoadSceneAsync(_gameScene);
        float dots = 0f;
        while (!op.isDone)
        {
            if (label != null)
            {
                dots += Time.unscaledDeltaTime * 3f;
                label.text = "CARGANDO" + new string('.', (int)dots % 4);
            }
            yield return null;
        }
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
    }

    private TMP_Text GetOrCreateLoadingLabel()
    {
        if (_loadingLabel != null) return _loadingLabel;

        var go = new GameObject("LoadingText", typeof(RectTransform));
        go.transform.SetParent(_fade.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);   // abajo a la derecha
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-60f, 50f);
        rt.sizeDelta = new Vector2(500f, 80f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = "CARGANDO";
        text.fontSize = 42;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.raycastTarget = false;
        _loadingLabel = text;
        return text;
    }
}
