using UnityEngine;
using System.Collections;

public class UpgradeShipBOSS : MonoBehaviour
{
    [SerializeField] Movement _movement;
    [SerializeField] AbilityController _abilityController;

    [Header("Animación")]
    public float spinDuration = 1f;
    public float spinSpeed = 360f;
    public float scaleUpAmount = 1.3f;
    public float scaleDownDuration = 0.4f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] GameObject _barcoViejoTest;
    [SerializeField] GameObject _barcoNew;

    [SerializeField] GameObject _particulasPuff;
    [SerializeField] GameObject _canvasShipUpgrades;
    [SerializeField] EnemySpawner _enemySpawner;

    [SerializeField] RT_PlayerStats _playerHealth;
    [SerializeField] RT_PlayerHealth _playerHealthRT;

    public void Onclick()
    {
        AnimateShip();
        _canvasShipUpgrades.SetActive(false);
        _playerHealth.maxHealth = 250;

        _playerHealth.currentHealth = _playerHealth.maxHealth;
        _playerHealthRT.TakeDamage(1f);
    }

    public void OnclickSpeed()
    {
        AnimateShip();
        _canvasShipUpgrades.SetActive(false);
        _playerHealth.moveSpeed = 11;

    }

    private void AnimateShip()
    {
        _movement.SetMovementEnabled(false);
        StartCoroutine(ShipUpgradeAnim());
    }

    private IEnumerator ShipUpgradeAnim()
    {
        
        
        Transform ship = _movement.transform;
        Quaternion originalRot = ship.rotation;
        Vector3 originalScale = ship.localScale;
        Vector3 bigScale = originalScale * scaleUpAmount;

        // Fase 1: girar y agrandarse
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinDuration;

            ship.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
            ship.localScale = Vector3.Lerp(originalScale, bigScale, scaleCurve.Evaluate(t));
            
            yield return null;
        }
        _particulasPuff.SetActive(true);
        StartCoroutine(resetP());
        _abilityController.ShipUpgraded();
        _barcoViejoTest.SetActive(false);


        
        // Fase 2: dejar de girar, achicarse y volver a rotación original
        elapsed = 0f;
        while (elapsed < scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleDownDuration;

            ship.localScale = Vector3.Lerp(bigScale, originalScale, scaleCurve.Evaluate(t));
            ship.rotation = Quaternion.Slerp(ship.rotation, originalRot, t);

            yield return null;
            _barcoNew.SetActive(true);
        }
        
        ship.localScale = originalScale;
        ship.rotation = originalRot;
        _movement.SetMovementEnabled(true);
    }

    public void ResetShip()
    {
        StopAllCoroutines();
        _movement.SetMovementEnabled(true);
        _barcoViejoTest.SetActive(true);
        _barcoNew.SetActive(false);
        _particulasPuff.SetActive(false);
        _canvasShipUpgrades.SetActive(false);
    }

    IEnumerator resetP()
    {
        yield return new WaitForSeconds(2f);
        _particulasPuff.SetActive(false);

    }
}