using Microsoft.Xna.Framework;

namespace Dokryun.Systems;

public class AugmentStats
{
    // 기본 스탯
    public float AttackBonus { get; set; } = 0f;          // 공격력 추가 (절대값)
    public float FireRateMultiplier { get; set; } = 1f;    // 공격속도 배율
    public float CritRateBonus { get; set; } = 0f;         // 치명타 확률 추가
    public float CritDamageMultiplier { get; set; } = 1.5f; // 치명타 데미지 배율
    public float MaxHPBonus { get; set; } = 0f;            // 최대 체력 추가
    public float Defense { get; set; } = 0f;               // 방어력 (데미지 감소 절대값)
    public float EvasionBonus { get; set; } = 0f;          // 회피율

    // 검사 전용 기본
    public float SwordRange { get; set; } = 75f;
    public float SwordArc { get; set; } = 2.5f;
    public float SwordKnockback { get; set; } = 150f;

    // === 고유 아이템 플래그 ===
    // 잔영의 운석
    public bool AfterImage { get; set; }

    // 초승의 운석
    public bool CrescentWave { get; set; }

    // 흡혈의 운석
    public float LifeSteal { get; set; } = 0f;

    // 발도의 운석
    public bool DrawSlash { get; set; }

    // 균열의 운석
    public bool GroundCrack { get; set; }

    // 폭발의 운석
    public bool ExplosiveFlame { get; set; }

    // 번개의 운석
    public bool CritLightning { get; set; }

    // 바람의 운석
    public bool WindBurst { get; set; }
    public float WindBurstTimer { get; set; } = 0f;

    // === 시너지 플래그 ===
    // 집중의 공명 (잔영 + 균열 → 추가피해 2배)
    public bool SynergyFocusResonance { get; set; }

    // 광전사의 공명 (흡혈 + 바람 → 체력 40% 미만시 1.5배)
    public bool SynergyBerserkerResonance { get; set; }

    public void Reset()
    {
        AttackBonus = 0f;
        FireRateMultiplier = 1f;
        CritRateBonus = 0f;
        CritDamageMultiplier = 1.5f;
        MaxHPBonus = 0f;
        Defense = 0f;
        EvasionBonus = 0f;

        SwordRange = 75f;
        SwordArc = 2.5f;
        SwordKnockback = 150f;

        AfterImage = false;
        CrescentWave = false;
        LifeSteal = 0f;
        DrawSlash = false;
        GroundCrack = false;
        ExplosiveFlame = false;
        CritLightning = false;
        WindBurst = false;
        WindBurstTimer = 0f;

        SynergyFocusResonance = false;
        SynergyBerserkerResonance = false;
    }
}
