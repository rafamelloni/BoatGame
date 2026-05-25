using UnityEngine;
using UnityEngine.UI;

public class FlaskLiquid : MonoBehaviour
{
    public Material flaskMaterial;
    public Movement movement;

    [Header("Tilt")]
    public float tiltMultiplier = 0.3f;
    public float tiltSmoothing = 5f;

    private float _currentTilt = 0f;

    void Update()
    {
        float targetTilt = movement.GetCurrentBankZ() * tiltMultiplier;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * tiltSmoothing);
        flaskMaterial.SetFloat("_Tilt", _currentTilt);

        
    }
}