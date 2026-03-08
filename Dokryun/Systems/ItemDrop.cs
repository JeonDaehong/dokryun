using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Systems;

public enum MeteoriteId
{
    // === 중첩 가능 (25개) ===
    // 공격
    PowerStone,         // 힘의 돌 - 공격력 +8%
    SwiftStone,         // 신속의 돌 - 공격속도 +10%
    SharpStone,         // 예리의 돌 - 치명타율 +5%
    GiantStone,         // 거인의 돌 - 화살 크기 +15%, 공격력 +3%
    MultiStone,         // 다발의 돌 - 화살 +1
    SpreadStone,        // 산개의 돌 - 산탄 각도 +0.1, 화살 +1
    VelocityStone,      // 탄속의 돌 - 화살 속도 +60
    CritDmgStone,       // 급소의 돌 - 치명타 피해 +30%

    // 방어
    IronStone,          // 철벽의 돌 - 피해 감소 +5%
    VitalStone,         // 생명의 돌 - 최대 HP +15
    DrainStone,         // 흡혈의 돌 - 생명력 흡수 +2%
    WindStone,          // 질풍의 돌 - 이동속도 +8%

    // 원소
    FireStone,          // 화염의 돌 - 화염 피해 +10
    PoisonStone,        // 독의 돌 - 독 피해 +8 (3초간)
    IceStone,           // 빙결의 돌 - 빙결 피해 +5, 감속 +10%
    ThunderStone,       // 뇌전의 돌 - 번개 피해 +12

    // 특수
    AccuracyStone,      // 정밀의 돌 - 명중률 +5%
    EvasionStone,       // 회피의 돌 - 회피율 +3%
    PierceStone,        // 관통의 돌 - 관통 +1
    BounceStone,        // 반사의 돌 - 반사 +1
    ShrapnelStone,      // 파편의 돌 - 파편 수 +2
    ChainStone,         // 연쇄의 돌 - 연쇄 대상 +1
    EchoStone,          // 잔향의 돌 - 10% 확률 이중발사
    MoveAtkStone,       // 유격의 돌 - 이동 중 공격력 +12%
    StillAtkStone,      // 집중의 돌 - 정지 시 공격력 +20%

    // === 비중첩 (15개) ===
    HomingGem,          // 추적의 보석 - 유도 화살
    ExplosiveGem,       // 폭렬의 보석 - 폭발 화살
    ChainLightningGem,  // 뇌쇄의 보석 - 연쇄 번개
    FlameGem,           // 화염의 보석 - 불화살
    FrostGem,           // 빙결의 보석 - 얼음 화살
    ArrowRainGem,       // 천화의 보석 - 화살비
    GhostStepGem,       // 유령보의 보석 - 대시 무적
    LightningDashGem,   // 뇌보의 보석 - 대시 번개
    KillBuffGem,        // 살의의 보석 - 처치 시 공격력 버프
    ItemDropGem,        // 행운의 보석 - 아이템 드롭률 증가
    MeteorStrikeGem,    // 유성의 보석 - 일정 시간마다 유성 낙하
    TornadoGem,         // 회오리의 보석 - 적중 시 회오리 생성
    ShadowArrowGem,     // 그림자의 보석 - 그림자 화살 추가 발사
    PhantomArcherGem,   // 환영의 보석 - 분신 궁수
    TimeSlowGem,        // 시간의 보석 - 회피 시 시간 감속
}

public enum MeteoriteRarity
{
    White,  // 백 - 일반
    Blue,   // 청 - 고급
    Red,    // 홍 - 희귀
    Gold    // 금 - 전설
}

public static class MeteoriteDatabase
{
    public record MeteoriteInfo(
        MeteoriteId Id,
        string Name,
        string Description,
        MeteoriteRarity Rarity,
        bool Stackable,
        Action<AugmentStats> Apply,
        Color MainColor,
        Color GlowColor
    );

    public static readonly Dictionary<MeteoriteId, MeteoriteInfo> All = new()
    {
        // ===== 중첩 가능 - 공격 =====
        [MeteoriteId.PowerStone] = new(MeteoriteId.PowerStone, "힘의 돌", "공격력 +8%",
            MeteoriteRarity.White, true, s => s.AttackMultiplier += 0.08f,
            new Color(220, 180, 140), new Color(200, 160, 120)),

        [MeteoriteId.SwiftStone] = new(MeteoriteId.SwiftStone, "신속의 돌", "공격속도 +10%",
            MeteoriteRarity.White, true, s => s.FireRateMultiplier += 0.10f,
            new Color(180, 255, 200), new Color(140, 220, 160)),

        [MeteoriteId.SharpStone] = new(MeteoriteId.SharpStone, "예리의 돌", "치명타율 +5%",
            MeteoriteRarity.White, true, s => s.CritRateBonus += 0.05f,
            new Color(255, 220, 100), new Color(220, 190, 80)),

        [MeteoriteId.GiantStone] = new(MeteoriteId.GiantStone, "거인의 돌", "화살 크기 +15%, 공격력 +3%",
            MeteoriteRarity.White, true, s => { s.ArrowSize += 0.15f; s.AttackMultiplier += 0.03f; },
            new Color(200, 180, 160), new Color(170, 150, 130)),

        [MeteoriteId.MultiStone] = new(MeteoriteId.MultiStone, "다발의 돌", "화살 +1",
            MeteoriteRarity.Blue, true, s => s.ExtraArrows += 1,
            new Color(180, 220, 255), new Color(140, 180, 240)),

        [MeteoriteId.SpreadStone] = new(MeteoriteId.SpreadStone, "산개의 돌", "산탄 각도 증가, 화살 +1",
            MeteoriteRarity.Blue, true, s => { s.SpreadAngle += 0.1f; s.ExtraArrows += 1; },
            new Color(200, 180, 255), new Color(170, 150, 230)),

        [MeteoriteId.VelocityStone] = new(MeteoriteId.VelocityStone, "탄속의 돌", "화살 속도 +60",
            MeteoriteRarity.White, true, s => s.ArrowSpeed += 60f,
            new Color(200, 240, 255), new Color(170, 210, 240)),

        [MeteoriteId.CritDmgStone] = new(MeteoriteId.CritDmgStone, "급소의 돌", "치명타 피해 +30%",
            MeteoriteRarity.Blue, true, s => s.CritDamageMultiplier += 0.3f,
            new Color(255, 200, 80), new Color(230, 170, 60)),

        // ===== 중첩 가능 - 방어 =====
        [MeteoriteId.IronStone] = new(MeteoriteId.IronStone, "철벽의 돌", "피해 감소 +5%",
            MeteoriteRarity.White, true, s => s.DamageReduction += 0.05f,
            new Color(160, 170, 190), new Color(130, 140, 160)),

        [MeteoriteId.VitalStone] = new(MeteoriteId.VitalStone, "생명의 돌", "최대 HP +15",
            MeteoriteRarity.White, true, s => s.MaxHPBonus += 15f,
            new Color(100, 255, 120), new Color(70, 220, 90)),

        [MeteoriteId.DrainStone] = new(MeteoriteId.DrainStone, "흡혈의 돌", "생명력 흡수 +2%",
            MeteoriteRarity.Blue, true, s => s.LifeSteal += 0.02f,
            new Color(255, 80, 80), new Color(220, 60, 60)),

        [MeteoriteId.WindStone] = new(MeteoriteId.WindStone, "질풍의 돌", "이동속도 +8%",
            MeteoriteRarity.White, true, s => s.SpeedMultiplier += 0.08f,
            new Color(180, 255, 220), new Color(140, 220, 180)),

        // ===== 중첩 가능 - 원소 =====
        [MeteoriteId.FireStone] = new(MeteoriteId.FireStone, "화염의 돌", "화염 피해 +10",
            MeteoriteRarity.Blue, true, s => s.FireDamage += 10f,
            new Color(255, 120, 40), new Color(230, 90, 20)),

        [MeteoriteId.PoisonStone] = new(MeteoriteId.PoisonStone, "독의 돌", "독 피해 +8 (3초)",
            MeteoriteRarity.Blue, true, s => s.PoisonDamage += 8f,
            new Color(120, 255, 80), new Color(90, 220, 60)),

        [MeteoriteId.IceStone] = new(MeteoriteId.IceStone, "빙결의 돌", "빙결 피해 +5, 감속 +10%",
            MeteoriteRarity.Blue, true, s => { s.IceDamage += 5f; s.FrostSlow += 0.10f; },
            new Color(140, 220, 255), new Color(100, 190, 240)),

        [MeteoriteId.ThunderStone] = new(MeteoriteId.ThunderStone, "뇌전의 돌", "번개 피해 +12",
            MeteoriteRarity.Blue, true, s => s.LightningDamage += 12f,
            new Color(180, 160, 255), new Color(150, 130, 230)),

        // ===== 중첩 가능 - 특수 =====
        [MeteoriteId.AccuracyStone] = new(MeteoriteId.AccuracyStone, "정밀의 돌", "명중률 +5%",
            MeteoriteRarity.White, true, s => s.AccuracyBonus += 0.05f,
            new Color(220, 220, 200), new Color(190, 190, 170)),

        [MeteoriteId.EvasionStone] = new(MeteoriteId.EvasionStone, "회피의 돌", "회피율 +3%",
            MeteoriteRarity.White, true, s => s.EvasionBonus += 0.03f,
            new Color(200, 200, 220), new Color(170, 170, 190)),

        [MeteoriteId.PierceStone] = new(MeteoriteId.PierceStone, "관통의 돌", "관통 +1",
            MeteoriteRarity.Blue, true, s => { s.PiercingArrows = true; s.PierceCount += 1; },
            new Color(255, 220, 100), new Color(230, 190, 80)),

        [MeteoriteId.BounceStone] = new(MeteoriteId.BounceStone, "반사의 돌", "반사 +1",
            MeteoriteRarity.Blue, true, s => { s.BouncingArrows = true; s.BounceCount += 1; },
            new Color(200, 255, 220), new Color(170, 230, 190)),

        [MeteoriteId.ShrapnelStone] = new(MeteoriteId.ShrapnelStone, "파편의 돌", "파편 수 +2",
            MeteoriteRarity.Blue, true, s => s.ShrapnelCount += 2,
            new Color(220, 180, 140), new Color(190, 150, 110)),

        [MeteoriteId.ChainStone] = new(MeteoriteId.ChainStone, "연쇄의 돌", "연쇄 대상 +1",
            MeteoriteRarity.Blue, true, s => { s.ChainLightning = true; s.ChainCount += 1; },
            new Color(160, 200, 255), new Color(130, 170, 230)),

        [MeteoriteId.EchoStone] = new(MeteoriteId.EchoStone, "잔향의 돌", "10% 확률 이중 발사",
            MeteoriteRarity.Blue, true, s => s.EchoChance += 0.10f,
            new Color(200, 180, 255), new Color(170, 150, 230)),

        [MeteoriteId.MoveAtkStone] = new(MeteoriteId.MoveAtkStone, "유격의 돌", "이동 중 공격력 +12%",
            MeteoriteRarity.White, true, s => s.MovingAttackBonus += 0.12f,
            new Color(200, 240, 180), new Color(170, 210, 150)),

        [MeteoriteId.StillAtkStone] = new(MeteoriteId.StillAtkStone, "집중의 돌", "정지 시 공격력 +20%",
            MeteoriteRarity.White, true, s => s.StillAttackBonus += 0.20f,
            new Color(240, 200, 180), new Color(210, 170, 150)),

        // ===== 비중첩 - 보석 =====
        [MeteoriteId.HomingGem] = new(MeteoriteId.HomingGem, "추적의 보석", "화살이 적을 추적",
            MeteoriteRarity.Red, false, s => { s.HomingArrows = true; s.HomingStrength = Math.Max(s.HomingStrength, 3f); },
            new Color(100, 255, 180), new Color(60, 220, 140)),

        [MeteoriteId.ExplosiveGem] = new(MeteoriteId.ExplosiveGem, "폭렬의 보석", "적중 시 폭발",
            MeteoriteRarity.Red, false, s => { s.ExplosiveArrows = true; s.ExplosionRadius = Math.Max(s.ExplosionRadius, 50f); s.ExplosionDamage = Math.Max(s.ExplosionDamage, 0.6f); },
            new Color(255, 100, 50), new Color(255, 140, 60)),

        [MeteoriteId.ChainLightningGem] = new(MeteoriteId.ChainLightningGem, "뇌쇄의 보석", "연쇄 번개 3체",
            MeteoriteRarity.Red, false, s => { s.ChainLightning = true; s.ChainDamage = Math.Max(s.ChainDamage, 0.5f); s.ChainCount = Math.Max(s.ChainCount, 3); },
            new Color(130, 180, 255), new Color(100, 150, 230)),

        [MeteoriteId.FlameGem] = new(MeteoriteId.FlameGem, "화염의 보석", "불화살, 공격력 +20%",
            MeteoriteRarity.Red, false, s => { s.FlameArrows = true; s.AttackMultiplier += 0.20f; s.ArrowColor = new Color(255, 120, 30); },
            new Color(255, 150, 40), new Color(230, 120, 20)),

        [MeteoriteId.FrostGem] = new(MeteoriteId.FrostGem, "빙결의 보석", "얼음 화살, 감속 40%",
            MeteoriteRarity.Red, false, s => { s.FrostArrows = true; s.FrostSlow = Math.Max(s.FrostSlow, 0.4f); s.ArrowColor = new Color(100, 200, 255); },
            new Color(100, 200, 255), new Color(70, 170, 230)),

        [MeteoriteId.ArrowRainGem] = new(MeteoriteId.ArrowRainGem, "천화의 보석", "5초마다 화살비",
            MeteoriteRarity.Gold, false, s => { s.ArrowRain = true; s.ArrowRainCooldown = 5f; },
            new Color(255, 230, 100), new Color(230, 200, 70)),

        [MeteoriteId.GhostStepGem] = new(MeteoriteId.GhostStepGem, "유령보의 보석", "대시 중 무적",
            MeteoriteRarity.Red, false, s => s.GhostStep = true,
            new Color(180, 180, 220), new Color(150, 150, 190)),

        [MeteoriteId.LightningDashGem] = new(MeteoriteId.LightningDashGem, "뇌보의 보석", "대시 경로에 번개",
            MeteoriteRarity.Red, false, s => { s.LightningDash = true; s.LightningDashDamage = Math.Max(s.LightningDashDamage, 30f); },
            new Color(150, 130, 255), new Color(120, 100, 230)),

        [MeteoriteId.KillBuffGem] = new(MeteoriteId.KillBuffGem, "살의의 보석", "처치 시 3초간 공격력 +30%",
            MeteoriteRarity.Red, false, s => s.KillAttackBuff = true,
            new Color(255, 80, 80), new Color(230, 60, 60)),

        [MeteoriteId.ItemDropGem] = new(MeteoriteId.ItemDropGem, "행운의 보석", "아이템 드롭률 +50%",
            MeteoriteRarity.Blue, false, s => s.ItemDropBonus += 0.50f,
            new Color(255, 255, 100), new Color(230, 230, 70)),

        [MeteoriteId.MeteorStrikeGem] = new(MeteoriteId.MeteorStrikeGem, "유성의 보석", "8초마다 유성 낙하",
            MeteoriteRarity.Gold, false, s => s.MeteorStrike = true,
            new Color(255, 180, 60), new Color(230, 150, 40)),

        [MeteoriteId.TornadoGem] = new(MeteoriteId.TornadoGem, "회오리의 보석", "적중 시 10% 확률 회오리",
            MeteoriteRarity.Red, false, s => s.TornadoOnHit = true,
            new Color(160, 220, 180), new Color(130, 190, 150)),

        [MeteoriteId.ShadowArrowGem] = new(MeteoriteId.ShadowArrowGem, "그림자의 보석", "그림자 화살 추가",
            MeteoriteRarity.Gold, false, s => s.ShadowArrow = true,
            new Color(100, 80, 120), new Color(70, 50, 90)),

        [MeteoriteId.PhantomArcherGem] = new(MeteoriteId.PhantomArcherGem, "환영의 보석", "분신 궁수 소환",
            MeteoriteRarity.Gold, false, s => s.PhantomArcher = true,
            new Color(180, 150, 255), new Color(150, 120, 230)),

        [MeteoriteId.TimeSlowGem] = new(MeteoriteId.TimeSlowGem, "시간의 보석", "회피 시 2초간 시간 감속",
            MeteoriteRarity.Gold, false, s => s.TimeSlowOnDodge = true,
            new Color(200, 200, 255), new Color(170, 170, 230)),
    };

    public static MeteoriteInfo Get(MeteoriteId id) => All[id];

    public static Color RarityColor(MeteoriteRarity rarity) => rarity switch
    {
        MeteoriteRarity.White => new Color(200, 200, 200),
        MeteoriteRarity.Blue => new Color(80, 140, 255),
        MeteoriteRarity.Red => new Color(255, 60, 60),
        MeteoriteRarity.Gold => new Color(255, 210, 50),
        _ => Color.White
    };

    public static string RarityName(MeteoriteRarity rarity) => rarity switch
    {
        MeteoriteRarity.White => "백급",
        MeteoriteRarity.Blue => "청급",
        MeteoriteRarity.Red => "홍급",
        MeteoriteRarity.Gold => "금급",
        _ => ""
    };
}

// ===== 시너지 시스템 =====
public static class SynergySystem
{
    public record Synergy(
        string Name,
        string Description,
        MeteoriteId ItemA,
        MeteoriteId ItemB,
        Action<AugmentStats> Apply,
        Color Color
    );

    public static readonly List<Synergy> AllSynergies = new()
    {
        new("유성회오리", "유성이 회오리를 동반",
            MeteoriteId.MeteorStrikeGem, MeteoriteId.TornadoGem,
            s => { s.MeteorTornado = true; },
            new Color(255, 200, 100)),

        new("빙뢰", "빙결 + 번개 = 연쇄 빙결",
            MeteoriteId.IceStone, MeteoriteId.ThunderStone,
            s => { s.IceLightning = true; },
            new Color(140, 200, 255)),

        new("독폭", "독 + 폭발 = 독구름",
            MeteoriteId.PoisonStone, MeteoriteId.ExplosiveGem,
            s => { s.PoisonExplosion = true; },
            new Color(120, 200, 60)),

        new("살의유성", "처치 시 유성 낙하",
            MeteoriteId.KillBuffGem, MeteoriteId.MeteorStrikeGem,
            s => { s.KillMeteor = true; },
            new Color(255, 120, 60)),

        new("그림자폭발", "그림자 화살 폭발",
            MeteoriteId.ShadowArrowGem, MeteoriteId.ExplosiveGem,
            s => { s.ShadowExplosion = true; },
            new Color(150, 80, 120)),

        new("감속회오리", "빙결 + 회오리 = 얼음 폭풍",
            MeteoriteId.FrostGem, MeteoriteId.TornadoGem,
            s => { s.SlowTornado = true; },
            new Color(100, 220, 255)),

        new("보스사냥꾼", "유성 + 관통 = 보스 추가 피해 +25%",
            MeteoriteId.MeteorStrikeGem, MeteoriteId.PierceStone,
            s => { s.BossDamageBonus += 0.25f; },
            new Color(255, 200, 50)),

        new("하늘비", "화살비 + 다발 = 화살비 강화",
            MeteoriteId.ArrowRainGem, MeteoriteId.MultiStone,
            s => { s.SkyArrows = true; },
            new Color(200, 230, 255)),

        new("환영추적", "환영 궁수 + 유도 = 환영 유도",
            MeteoriteId.PhantomArcherGem, MeteoriteId.HomingGem,
            s => { s.HomingStrength += 2f; s.ExtraArrows += 1; },
            new Color(180, 160, 255)),

        new("시간폭풍", "시간 감속 + 회오리 = 시간 폭풍",
            MeteoriteId.TimeSlowGem, MeteoriteId.TornadoGem,
            s => { s.SlowTornado = true; s.TornadoOnHit = true; },
            new Color(200, 200, 255)),
    };

    public static List<Synergy> GetActiveSynergies(Inventory inventory)
    {
        var active = new List<Synergy>();
        foreach (var syn in AllSynergies)
        {
            if (inventory.Has(syn.ItemA) && inventory.Has(syn.ItemB))
                active.Add(syn);
        }
        return active;
    }
}

// ===== 인벤토리 =====
public class Inventory
{
    public const int MaxSlots = 20;

    // MeteoriteId -> count (stackable) or 1 (non-stackable)
    private readonly Dictionary<MeteoriteId, int> _items = new();

    public IReadOnlyDictionary<MeteoriteId, int> Items => _items;

    public int UsedSlots
    {
        get
        {
            int slots = 0;
            foreach (var kvp in _items)
            {
                var info = MeteoriteDatabase.Get(kvp.Key);
                // Stackable items use 1 slot regardless of count
                slots += 1;
            }
            return slots;
        }
    }

    public bool IsFull => UsedSlots >= MaxSlots;

    public bool Has(MeteoriteId id) => _items.ContainsKey(id);

    public int GetCount(MeteoriteId id) => _items.TryGetValue(id, out int c) ? c : 0;

    public bool TryAdd(MeteoriteId id)
    {
        var info = MeteoriteDatabase.Get(id);
        if (_items.ContainsKey(id))
        {
            if (info.Stackable)
            {
                _items[id]++;
                return true;
            }
            return false; // Already have non-stackable
        }

        if (IsFull) return false;
        _items[id] = 1;
        return true;
    }

    public bool Remove(MeteoriteId id)
    {
        if (!_items.ContainsKey(id)) return false;
        var info = MeteoriteDatabase.Get(id);
        if (info.Stackable && _items[id] > 1)
        {
            _items[id]--;
        }
        else
        {
            _items.Remove(id);
        }
        return true;
    }

    public void RemoveAll(MeteoriteId id)
    {
        _items.Remove(id);
    }

    public void RecalculateStats(AugmentStats stats)
    {
        stats.Reset();

        // Apply all items
        foreach (var kvp in _items)
        {
            var info = MeteoriteDatabase.Get(kvp.Key);
            int count = info.Stackable ? kvp.Value : 1;
            for (int i = 0; i < count; i++)
                info.Apply(stats);
        }

        // Apply synergies
        var synergies = SynergySystem.GetActiveSynergies(this);
        foreach (var syn in synergies)
            syn.Apply(stats);
    }

    public List<(MeteoriteId id, int count)> GetSortedItems()
    {
        var list = _items.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        list.Sort((a, b) =>
        {
            var infoA = MeteoriteDatabase.Get(a.Key);
            var infoB = MeteoriteDatabase.Get(b.Key);
            int cmp = infoA.Rarity.CompareTo(infoB.Rarity);
            if (cmp != 0) return cmp;
            return a.Key.CompareTo(b.Key);
        });
        return list;
    }
}

// ===== 드롭 아이템 (바닥에 떨어진 운석) =====
public class DroppedItem
{
    public Vector2 Position { get; set; }
    public MeteoriteId MeteoriteId { get; set; }
    public bool IsActive { get; set; } = true;
    public float AnimTimer { get; set; }
    public float PickupRadius { get; set; } = 28f;

    public Vector2 Velocity { get; set; }
    public bool IsSettled { get; set; }
    public float SettleTimer { get; set; }

    public MeteoriteDatabase.MeteoriteInfo Info => MeteoriteDatabase.Get(MeteoriteId);
    public string Name => Info.Name;
    public string Description => Info.Description;
    public Color MainColor => Info.MainColor;
    public Color GlowColor => Info.GlowColor;

    public void Update(float dt)
    {
        AnimTimer += dt;
        if (!IsSettled)
        {
            Position += Velocity * dt;
            Velocity = new Vector2(Velocity.X * 0.95f, Velocity.Y + 300f * dt);
            SettleTimer += dt;
            if (SettleTimer > 0.4f)
            {
                IsSettled = true;
                Velocity = Vector2.Zero;
            }
        }
    }

    public bool IsPlayerNear(Vector2 playerPos)
    {
        return IsSettled && Vector2.Distance(Position, playerPos) < PickupRadius;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsActive) return;

        float bob = IsSettled ? MathF.Sin(AnimTimer * 3f) * 2f : 0;
        int x = (int)Position.X;
        int y = (int)(Position.Y + bob);

        var info = Info;

        // Glow
        float glow = MathF.Sin(AnimTimer * 4f) * 0.15f + 0.25f;
        spriteBatch.Draw(pixel, new Rectangle(x - 12, y - 12, 24, 24), GlowColor * glow * 0.3f);

        // Body (diamond shape)
        int size = info.Stackable ? 5 : 6;
        spriteBatch.Draw(pixel, new Rectangle(x - size, y - 3, size * 2, 6), MainColor);
        spriteBatch.Draw(pixel, new Rectangle(x - 3, y - size, 6, size * 2), MainColor);

        // Core
        spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, 4, 4), Color.White * 0.7f);

        // Non-stackable gems get extra sparkle
        if (!info.Stackable)
        {
            float sparkle = MathF.Sin(AnimTimer * 6f);
            if (sparkle > 0.5f)
            {
                spriteBatch.Draw(pixel, new Rectangle(x - 1, y - 8, 2, 3), MainColor * 0.9f);
                spriteBatch.Draw(pixel, new Rectangle(x + 5, y - 1, 3, 2), MainColor * 0.9f);
                spriteBatch.Draw(pixel, new Rectangle(x - 7, y - 1, 3, 2), MainColor * 0.9f);
                spriteBatch.Draw(pixel, new Rectangle(x - 1, y + 6, 2, 3), MainColor * 0.9f);
            }
        }

        // Rarity border
        var rarityColor = MeteoriteDatabase.RarityColor(info.Rarity);
        float rarityPulse = MathF.Sin(AnimTimer * 2f) * 0.3f + 0.5f;
        spriteBatch.Draw(pixel, new Rectangle(x - size - 1, y - size - 1, (size + 1) * 2, 1), rarityColor * rarityPulse * 0.5f);
        spriteBatch.Draw(pixel, new Rectangle(x - size - 1, y + size, (size + 1) * 2, 1), rarityColor * rarityPulse * 0.5f);
    }

    // Roll a random meteorite from the pool (weighted by rarity)
    public static MeteoriteId RollMeteoriteId(int floor, bool isBoss)
    {
        float roll = (float)Random.Shared.NextDouble() * 100f;
        MeteoriteRarity targetRarity;

        if (isBoss)
        {
            float goldChance = Math.Min(5 + floor * 2f, 25f);
            float redChance = Math.Min(20 + floor * 3f, 40f);
            float blueChance = 30f;
            if (roll < goldChance) targetRarity = MeteoriteRarity.Gold;
            else if (roll < goldChance + redChance) targetRarity = MeteoriteRarity.Red;
            else if (roll < goldChance + redChance + blueChance) targetRarity = MeteoriteRarity.Blue;
            else targetRarity = MeteoriteRarity.White;
        }
        else
        {
            // Treasure chest
            float goldChance = Math.Min(2 + floor * 1f, 10f);
            float redChance = Math.Min(8 + floor * 2f, 25f);
            float blueChance = Math.Min(20 + floor * 2f, 35f);
            if (roll < goldChance) targetRarity = MeteoriteRarity.Gold;
            else if (roll < goldChance + redChance) targetRarity = MeteoriteRarity.Red;
            else if (roll < goldChance + redChance + blueChance) targetRarity = MeteoriteRarity.Blue;
            else targetRarity = MeteoriteRarity.White;
        }

        var candidates = MeteoriteDatabase.All.Values
            .Where(m => m.Rarity == targetRarity)
            .ToList();

        if (candidates.Count == 0)
            candidates = MeteoriteDatabase.All.Values.ToList();

        return candidates[Random.Shared.Next(candidates.Count)].Id;
    }

    public static DroppedItem Create(Vector2 position, int floor, bool isBoss)
    {
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
        float speed = 60f + (float)Random.Shared.NextDouble() * 40f;
        return new DroppedItem
        {
            Position = position,
            MeteoriteId = RollMeteoriteId(floor, isBoss),
            Velocity = new Vector2(MathF.Cos(angle) * speed, -100f - (float)Random.Shared.NextDouble() * 50f)
        };
    }

    public static DroppedItem CreateSpecific(Vector2 position, MeteoriteId id)
    {
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
        float speed = 60f + (float)Random.Shared.NextDouble() * 40f;
        return new DroppedItem
        {
            Position = position,
            MeteoriteId = id,
            Velocity = new Vector2(MathF.Cos(angle) * speed, -100f - (float)Random.Shared.NextDouble() * 50f)
        };
    }
}
