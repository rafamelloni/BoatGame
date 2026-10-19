using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class UpgradeShipBOSS : MonoBehaviour
{
    [Serializable]
    public class ShipTier
    {
        public string name;
        public GameObject model;

        [Header("Posiciones de cañones originales y cofre")]
        [Tooltip("En el tier 1 se pueden dejar vacías (usa las originales).")]
        public Transform cannonPosR;
        public Transform cannonPosL;
        public Transform chestPos;

        [Header("Shoot points de este tier")]
        [Tooltip("Lista COMPLETA de shoot points del lado derecho (originales + extra). Vacío = mantiene los del tier anterior.")]
        public Transform[] rightShootPoints;
        [Tooltip("Lista COMPLETA de shoot points del lado izquierdo (originales + extra). Vacío = mantiene los del tier anterior.")]
        public Transform[] leftShootPoints;

        [Header("Cañones extra")]
        [Tooltip("Cañones extra que se prenden en este tier. Lista completa: los que no estén acá se apagan.")]
        public GameObject[] extraCannons;
        [Tooltip("Posición de cada cañón extra en ESTE tier, en el mismo orden que extraCannons. Si un cañón no tiene posición, queda donde lo dejaste en la escena.")]
        public Transform[] extraCannonPositions;
    }

    [SerializeField] Movement _movement;
    [SerializeField] AbilityController _abilityController;

    [Header("Tiers (el primero = barco inicial)")]
    [SerializeField] ShipTier[] _tiers;

    [Header("Meshes de cañones por tier")]
    [Tooltip("Cañones que cambian de mesh al subir de tier.")]
    [SerializeField] CannonTierMeshes[] _cannonMeshes;

    [Header("Animación")]
    public float spinDuration = 1f;
    public float spinSpeed = 360f;
    public float scaleUpAmount = 1.3f;
    public float scaleDownDuration = 0.4f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] GameObject _particulasPuff;
    [SerializeField] GameObject _canvasShipUpgrades;
    [SerializeField] EnemySpawner _enemySpawner;

    [Header("Stats")]
    [SerializeField] RT_PlayerStats _playerHealth;
    [SerializeField] RT_PlayerHealth _playerHealthRT;
    [SerializeField] float _healthBonusPerUpgrade = 100f;
    [SerializeField] float _speedBonusPerUpgrade = 5f;

    private int _currentTier = 0;
    private ShipHardpoints _hardpoints;
    private Transform[] _originalRightPoints;
    private Transform[] _originalLeftPoints;
    private readonly Dictionary<GameObject, Vector3> _extraOriginalPos = new();

    public bool HasNextTier => _currentTier + 1 < _tiers.Length;
    public int CurrentTier => _currentTier;
    public int TierCount => _tiers.Length;

    private void Start()
    {
        _hardpoints = _abilityController.GetComponent<ShipHardpoints>();
        _originalRightPoints = _hardpoints.rightShootPoints;
        _originalLeftPoints = _hardpoints.leftShootPoints;

        foreach (var t in _tiers)
            if (t.extraCannons != null)
                foreach (var c in t.extraCannons)
                    if (c != null && !_extraOriginalPos.ContainsKey(c))
                        _extraOriginalPos[c] = c.transform.localPosition;

        ApplyModel(0);
    }

    public void Onclick()
    {
        if (!TryAnimateShip()) return;
        _playerHealth.maxHealth += _healthBonusPerUpgrade;
        _playerHealth.currentHealth = _playerHealth.maxHealth;
        _playerHealthRT.TakeDamage(1f);
    }

    public void OnclickSpeed()
    {
        if (!TryAnimateShip()) return;
        _playerHealth.moveSpeed += _speedBonusPerUpgrade;
    }

    private bool TryAnimateShip()
    {
        _canvasShipUpgrades.SetActive(false);
        if (!HasNextTier)
        {
            Debug.Log("[UpgradeShipBOSS] No hay más tiers de barco.");
            return false;
        }
        _movement.SetMovementEnabled(false);
        StartCoroutine(ShipUpgradeAnim(_currentTier + 1));
        return true;
    }

    private IEnumerator ShipUpgradeAnim(int nextTier)
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

        // Swap de modelo + cañones en el pico de la animación
        _particulasPuff.SetActive(true);
        StartCoroutine(resetP());
        ApplyTier(nextTier);

        // Fase 2: achicarse y volver a la rotación original
        elapsed = 0f;
        while (elapsed < scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleDownDuration;
            ship.localScale = Vector3.Lerp(bigScale, originalScale, scaleCurve.Evaluate(t));
            ship.rotation = Quaternion.Slerp(ship.rotation, originalRot, t);
            yield return null;
        }

        ship.localScale = originalScale;
        ship.rotation = originalRot;
        _movement.SetMovementEnabled(true);
    }

    // Aplica todo lo de un tier: modelo, cañones extra, posiciones, meshes y shoot points.
    private void ApplyTier(int tierIndex)
    {
        _currentTier = tierIndex;
        ApplyModel(tierIndex);
        var tier = _tiers[tierIndex];
        _abilityController.ShipUpgraded(tier.cannonPosR, tier.cannonPosL, tier.chestPos);
        ApplyShootPoints(tier);
    }

    private void ApplyModel(int tierIndex)
    {
        // Cambiar el mesh de los cañones al tier actual (tierIndex 0 = tier 1)
        if (_cannonMeshes != null)
            foreach (var cm in _cannonMeshes)
                if (cm != null) cm.SetTier(tierIndex + 1);

        // Apagar todo (modelos y cañones extra de todos los tiers)
        for (int i = 0; i < _tiers.Length; i++)
        {
            if (_tiers[i].model != null)
                _tiers[i].model.SetActive(false);
            if (_tiers[i].extraCannons != null)
                foreach (var c in _tiers[i].extraCannons)
                    if (c != null) c.SetActive(false);
        }

        // Prender y posicionar lo del tier actual
        var tier = _tiers[tierIndex];
        if (tier.model != null)
            tier.model.SetActive(true);

        if (tier.extraCannons == null) return;
        for (int i = 0; i < tier.extraCannons.Length; i++)
        {
            var cannon = tier.extraCannons[i];
            if (cannon == null) continue;

            Transform target = (tier.extraCannonPositions != null && i < tier.extraCannonPositions.Length)
                ? tier.extraCannonPositions[i]
                : null;

            if (target != null)
                cannon.transform.localPosition = target.localPosition;
            else if (_extraOriginalPos.TryGetValue(cannon, out var original))
                cannon.transform.localPosition = original;

            cannon.SetActive(true);

            // El recoil guarda su posición de descanso en Start(), que solo corre la
            // primera vez que se prende. Si el cañón ya se había prendido en un tier
            // anterior, hay que actualizarle la posición a mano.
            foreach (var recoil in cannon.GetComponentsInChildren<CannonRecoil>())
                recoil.UpdateLocalOrgin();
        }
    }

    private void ApplyShootPoints(ShipTier tier)
    {
        if (tier.rightShootPoints != null && tier.rightShootPoints.Length > 0)
            _hardpoints.rightShootPoints = tier.rightShootPoints;
        if (tier.leftShootPoints != null && tier.leftShootPoints.Length > 0)
            _hardpoints.leftShootPoints = tier.leftShootPoints;
    }

    public void ResetShip()
    {
        StopAllCoroutines();
        _currentTier = 0;
        ApplyModel(0);
        if (_hardpoints != null)
        {
            _hardpoints.rightShootPoints = _originalRightPoints;
            _hardpoints.leftShootPoints = _originalLeftPoints;
        }
        _movement.SetMovementEnabled(true);
        _particulasPuff.SetActive(false);
        _canvasShipUpgrades.SetActive(false);
    }

    IEnumerator resetP()
    {
        yield return new WaitForSeconds(2f);
        _particulasPuff.SetActive(false);
    }

    // ——— DEBUG ———
    public void DebugSetTier(int index)
    {
        if (index < 0 || index >= _tiers.Length) return;
        StopAllCoroutines();
        _movement.SetMovementEnabled(true);

        if (index == 0)
        {
            ResetShip();
            return;
        }

        ApplyTier(index);
        Debug.Log($"[UpgradeShipBOSS] DEBUG → tier {index + 1} ({_tiers[index].name})");
    }
}