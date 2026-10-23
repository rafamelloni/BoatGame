using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Lógica del Overworld: detecta en qué océano está el barquito, muestra el cartel
// y al confirmar carga la escena del nivel (salteando el menú principal).
public class OverworldManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private OverworldMap _map;
    [SerializeField] private OverworldBoat _boat;

    [Header("Cartel")]
    [SerializeField] private RectTransform _promptPanel;
    [SerializeField] private TMP_Text _promptTitle;
    [SerializeField] private TMP_Text _promptDescription;
    [SerializeField] private Button _sailButton;
    [SerializeField] private TMP_Text _sailButtonLabel;
    [SerializeField] private string _sailText = "Zarpar  [Espacio]";
    [SerializeField] private string _lockedText = "Próximamente";

    [Header("Controles")]
    [SerializeField] private KeyCode _sailKey = KeyCode.Space;
    [SerializeField] private KeyCode _sailKeyAlt = KeyCode.E;
    [SerializeField] private KeyCode _backKey = KeyCode.Escape;
    [Tooltip("Escena a la que vuelve con Escape (la del menú principal)")]
    [SerializeField] private string _mainMenuScene = "TestScene";

    [Header("Fade")]
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private float _fadeDuration = 0.6f;

    private OceanRegion _current;
    private bool _loading;
    private Coroutine _promptAnim;

    private void Start()
    {
        Time.timeScale = 1f;
        if (_sailButton != null) _sailButton.onClick.AddListener(() => { if (_current != null) TrySail(_current); });
        if (_promptPanel != null) _promptPanel.gameObject.SetActive(false);
        if (_fade != null) { _fade.alpha = 1f; StartCoroutine(Fade(1f, 0f)); }
    }

    private void Update()
    {
        if (_loading || _map == null || _boat == null) return;

        OceanRegion region = _map.GetRegionAt(_boat.transform.position);
        if (region != _current)
        {
            _current = region;
            _map.SetHighlighted(region);
            RefreshPrompt();
        }

        if (_current != null && (Input.GetKeyDown(_sailKey) || Input.GetKeyDown(_sailKeyAlt)))
            TrySail(_current);

        if (Input.GetKeyDown(_backKey) && !string.IsNullOrEmpty(_mainMenuScene))
        {
            OverworldSession.SkipMainMenu = false;
            StartCoroutine(FadeAndLoad(_mainMenuScene));
        }
    }

    private void RefreshPrompt()
    {
        if (_promptPanel == null) return;

        if (_current == null)
        {
            _promptPanel.gameObject.SetActive(false);
            return;
        }

        _promptPanel.gameObject.SetActive(true);
        if (_promptTitle != null) _promptTitle.text = _current.displayName;
        if (_promptDescription != null)
            _promptDescription.text = _current.unlocked ? _current.description : "Este mar todavía no se puede navegar.";
        if (_sailButton != null) _sailButton.interactable = _current.CanEnter;
        if (_sailButtonLabel != null) _sailButtonLabel.text = _current.CanEnter ? _sailText : _lockedText;

        if (_promptAnim != null) StopCoroutine(_promptAnim);
        _promptAnim = StartCoroutine(PopIn(_promptPanel));
    }

    private void TrySail(OceanRegion region)
    {
        if (_loading || !region.CanEnter) return;
        if (!Application.CanStreamedLevelBeLoaded(region.sceneName))
        {
            Debug.LogError($"[Overworld] La escena '{region.sceneName}' no está en Build Settings.");
            return;
        }

        OverworldSession.SkipMainMenu = true;
        OverworldSession.CurrentOcean = region.displayName;
        StartCoroutine(FadeAndLoad(region.sceneName));
    }

    private IEnumerator FadeAndLoad(string scene)
    {
        _loading = true;
        _boat.InputEnabled = false;
        if (_fade != null) yield return Fade(0f, 1f);
        SceneManager.LoadScene(scene);
    }

    private IEnumerator Fade(float from, float to)
    {
        _fade.gameObject.SetActive(true);
        _fade.blocksRaycasts = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / _fadeDuration;
            _fade.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        _fade.alpha = to;
        _fade.blocksRaycasts = to > 0.5f;
        if (to <= 0f) _fade.gameObject.SetActive(false);
    }

    private IEnumerator PopIn(RectTransform rt)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.22f;
            float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.08f;
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(t)) * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
        _promptAnim = null;
    }
}
