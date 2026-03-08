using Microsoft.Xna.Framework;

namespace Dokryun.Systems;

public class AugmentStats
{
    // 기본 공격
    public float AttackMultiplier { get; set; } = 1f;
    public float ArrowSpeed { get; set; } = 400f;
    public float FireRateMultiplier { get; set; } = 1f;
    public float ArrowSize { get; set; } = 1f;

    // 다중 화살
    public int ExtraArrows { get; set; } = 0;
    public float SpreadAngle { get; set; } = 0.2f;

    // 특수 효과
    public bool PiercingArrows { get; set; }
    public int PierceCount { get; set; } = 0;
    public bool ExplosiveArrows { get; set; }
    public float ExplosionRadius { get; set; } = 0f;
    public float ExplosionDamage { get; set; } = 0f;
    public bool HomingArrows { get; set; }
    public float HomingStrength { get; set; } = 0f;
    public bool BouncingArrows { get; set; }
    public int BounceCount { get; set; } = 0;
    public bool ChainLightning { get; set; }
    public float ChainDamage { get; set; } = 0f;
    public int ChainCount { get; set; } = 0;

    // 화살 비주얼
    public Color ArrowColor { get; set; } = new Color(255, 230, 150);
    public bool FlameArrows { get; set; }
    public bool FrostArrows { get; set; }
    public float FrostSlow { get; set; } = 0f;

    // 이동/방어
    public float SpeedMultiplier { get; set; } = 1f;
    public float DamageReduction { get; set; } = 0f;
    public float MaxHPBonus { get; set; } = 0f;
    public float CritRateBonus { get; set; } = 0f;
    public float CritDamageMultiplier { get; set; } = 2f;
    public float LifeSteal { get; set; } = 0f;

    // 특수 스킬
    public bool ArrowRain { get; set; }
    public float ArrowRainCooldown { get; set; } = 0f;
    public bool GhostStep { get; set; }
    public bool LightningDash { get; set; }
    public float LightningDashDamage { get; set; } = 0f;

    // === 신규 스탯 ===
    // 원소 피해
    public float FireDamage { get; set; } = 0f;
    public float PoisonDamage { get; set; } = 0f;
    public float IceDamage { get; set; } = 0f;
    public float LightningDamage { get; set; } = 0f;

    // 명중/회피
    public float AccuracyBonus { get; set; } = 0f;
    public float EvasionBonus { get; set; } = 0f;

    // 특수
    public int ShrapnelCount { get; set; } = 0;
    public float EchoChance { get; set; } = 0f;
    public float MovingAttackBonus { get; set; } = 0f;
    public float StillAttackBonus { get; set; } = 0f;
    public bool KillAttackBuff { get; set; }
    public float ItemDropBonus { get; set; } = 0f;

    // 보석 전용 효과
    public bool MeteorStrike { get; set; }
    public bool TornadoOnHit { get; set; }
    public bool ShadowArrow { get; set; }
    public bool PhantomArcher { get; set; }
    public bool TimeSlowOnDodge { get; set; }
    public float BossDamageBonus { get; set; } = 0f;

    // 시너지 효과
    public bool MeteorTornado { get; set; }
    public bool IceLightning { get; set; }
    public bool PoisonExplosion { get; set; }
    public bool KillMeteor { get; set; }
    public bool ShadowExplosion { get; set; }
    public bool SlowTornado { get; set; }
    public bool BossMeteor { get; set; }
    public bool SkyArrows { get; set; }

    public void Reset()
    {
        AttackMultiplier = 1f;
        ArrowSpeed = 400f;
        FireRateMultiplier = 1f;
        ArrowSize = 1f;
        ExtraArrows = 0;
        SpreadAngle = 0.2f;

        PiercingArrows = false;
        PierceCount = 0;
        ExplosiveArrows = false;
        ExplosionRadius = 0f;
        ExplosionDamage = 0f;
        HomingArrows = false;
        HomingStrength = 0f;
        BouncingArrows = false;
        BounceCount = 0;
        ChainLightning = false;
        ChainDamage = 0f;
        ChainCount = 0;

        ArrowColor = new Color(255, 230, 150);
        FlameArrows = false;
        FrostArrows = false;
        FrostSlow = 0f;

        SpeedMultiplier = 1f;
        DamageReduction = 0f;
        MaxHPBonus = 0f;
        CritRateBonus = 0f;
        CritDamageMultiplier = 2f;
        LifeSteal = 0f;

        ArrowRain = false;
        ArrowRainCooldown = 0f;
        GhostStep = false;
        LightningDash = false;
        LightningDashDamage = 0f;

        FireDamage = 0f;
        PoisonDamage = 0f;
        IceDamage = 0f;
        LightningDamage = 0f;
        AccuracyBonus = 0f;
        EvasionBonus = 0f;
        ShrapnelCount = 0;
        EchoChance = 0f;
        MovingAttackBonus = 0f;
        StillAttackBonus = 0f;
        KillAttackBuff = false;
        ItemDropBonus = 0f;

        MeteorStrike = false;
        TornadoOnHit = false;
        ShadowArrow = false;
        PhantomArcher = false;
        TimeSlowOnDodge = false;
        BossDamageBonus = 0f;

        MeteorTornado = false;
        IceLightning = false;
        PoisonExplosion = false;
        KillMeteor = false;
        ShadowExplosion = false;
        SlowTornado = false;
        BossMeteor = false;
        SkyArrows = false;
    }
}
