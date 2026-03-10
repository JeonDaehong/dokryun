using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Systems;

/// <summary>
/// 우클릭 카운터/패링 시스템 (Q/E 스킬 제거됨)
/// </summary>
public class SkillSystem
{
    // Counter state
    public float CounterCooldown { get; set; } = 5f;
    public float CounterCooldownTimer { get; set; }
    public float CounterDuration { get; set; } = 0.5f;
    public float CounterActiveTimer { get; set; }
    public float CounterDamageMultiplier { get; set; } = 3f;
    public float CounterKiCost { get; set; } = 15f;

    private const float ParryWindow = 0.3f;

    public bool IsCounterActive => CounterActiveTimer > 0;
    public bool IsCounterReady => CounterCooldownTimer <= 0;
    public bool IsParrying => IsCounterActive && CounterActiveTimer > CounterDuration - ParryWindow;
    public bool CounterTriggered { get; set; }

    public bool TryActivateCounter(float currentKi)
    {
        if (!IsCounterReady || IsCounterActive || currentKi < CounterKiCost) return false;
        CounterActiveTimer = CounterDuration;
        CounterCooldownTimer = CounterCooldown;
        return true;
    }

    public void Update(float dt)
    {
        if (CounterActiveTimer > 0) CounterActiveTimer -= dt;
        if (CounterCooldownTimer > 0) CounterCooldownTimer -= dt;

        // Reset counter trigger when counter ends
        if (!IsCounterActive) CounterTriggered = false;
    }

    public void Reset()
    {
        CounterCooldownTimer = 0;
        CounterActiveTimer = 0;
        CounterTriggered = false;
    }
}
