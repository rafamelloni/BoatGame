using UnityEngine;
using System.Collections;

public class DashMovement : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashSpeedMultiplier = 3.5f;
    [SerializeField] private float chargeRechargeTime = 4f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem trailSprint;
    [SerializeField] private ParticleSystem trailSprint1;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Image sprintBarImage;

    private int _currentCharges;
    private bool _isDashing = false;
    private bool _isUnlocked = false;
    private Movement _movement;

    public bool IsDashing => _isDashing;
    public bool IsInvincible => _isDashing;
    public float DashSpeedMultiplier => dashSpeedMultiplier;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        _currentCharges = maxCharges;

    }

    private void Update()
    {
        if (!_isUnlocked) return;

        UpdateUI();

        bool shiftPressed = Input.GetKeyDown(KeyCode.LeftShift);
        float vertical = Input.GetAxisRaw("Vertical");
        Debug.Log($"[DashMovement] charges:{_currentCharges} shift:{shiftPressed} vertical:{vertical} dashing:{_isDashing}");

        if (shiftPressed && _currentCharges > 0 && vertical > 0f && !_isDashing)
            StartCoroutine(DashRoutine());
    }

    public void Unlock()
    {
        _isUnlocked = true;
        _movement.SetSprintEnabled(false);
        Debug.Log($"[DashMovement] Unlock llamado. Movement encontrado: {_movement != null}");

    }

    private IEnumerator DashRoutine()
    {
        _currentCharges--;
        _isDashing = true;

        trailSprint?.Play();
        trailSprint1?.Play();
        _movement.playerWave.ApplyForwardTilt(0.4f);

        yield return new WaitForSeconds(dashDuration);

        _isDashing = false;

        trailSprint?.Stop();
        trailSprint1?.Stop();

        StartCoroutine(RechargeRoutine());
    }

    private IEnumerator RechargeRoutine()
    {
        yield return new WaitForSeconds(chargeRechargeTime);
        if (_currentCharges < maxCharges)
            _currentCharges++;
    }

    private void UpdateUI()
    {
        if (sprintBarImage != null)
            sprintBarImage.fillAmount = (float)_currentCharges / maxCharges;
    }
}