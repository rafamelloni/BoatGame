using System;
using UnityEngine;

public interface IcooldownAbilities 
{
    event Action<float> OnCooldownStarted;
    float CooldownDuration { get; }
    float RemainingCooldown { get; }
    bool IsOnCooldown { get; }
}
