using UnityEngine;

public class CustomCursorManager : MonoBehaviour
{
    [Header("Cursor personalizado")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("Dejalo en el centro si es un crosshair")]
    [SerializeField] private Vector2 hotSpot = new Vector2(32, 32);

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyCursor();
    }

    private void Start()
    {
        ApplyCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursor();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
            ApplyCursor();
    }

    private void ApplyCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.ForceSoftware);
        }
    }
}