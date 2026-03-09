using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Systems;

public enum MeteoriteId
{
    // === 중첩 가능 파편 (7개) ===
    RageFragment,       // 분노의 운석 파편 - 공격력 +3
    MistFragment,       // 안개의 운석 파편 - 회피율 +5%
    PainFragment,       // 고통의 운석 파편 - 공격속도 +10%
    RuinFragment,       // 파멸의 운석 파편 - 치명타확률 +10%
    SlaughterFragment,  // 학살의 운석 파편 - 치명타 데미지 +5%
    GiantFragment,      // 거인의 운석 파편 - 최대체력 +50
    GuardFragment,      // 수호의 운석 파편 - 방어력 +5

    // === 고유 운석 (8개) ===
    AfterImageMeteor,   // 잔영의 운석
    CrescentMeteor,     // 초승의 운석
    VampireMeteor,      // 흡혈의 운석
    DrawSlashMeteor,    // 발도의 운석
    CrackMeteor,        // 균열의 운석
    ExplosionMeteor,    // 폭발의 운석
    LightningMeteor,    // 번개의 운석
    WindMeteor,         // 바람의 운석
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
        // ===== 중첩 가능 파편 =====
        [MeteoriteId.RageFragment] = new(MeteoriteId.RageFragment, "분노의 운석 파편", "공격력 +3",
            MeteoriteRarity.White, true, s => s.AttackBonus += 3f,
            new Color(255, 140, 100), new Color(230, 110, 70)),

        [MeteoriteId.MistFragment] = new(MeteoriteId.MistFragment, "안개의 운석 파편", "회피율 +5%",
            MeteoriteRarity.White, true, s => s.EvasionBonus += 0.05f,
            new Color(180, 190, 220), new Color(150, 160, 190)),

        [MeteoriteId.PainFragment] = new(MeteoriteId.PainFragment, "고통의 운석 파편", "공격속도 +10%",
            MeteoriteRarity.White, true, s => s.FireRateMultiplier += 0.10f,
            new Color(200, 100, 140), new Color(170, 70, 110)),

        [MeteoriteId.RuinFragment] = new(MeteoriteId.RuinFragment, "파멸의 운석 파편", "치명타확률 +10%",
            MeteoriteRarity.Blue, true, s => s.CritRateBonus += 0.10f,
            new Color(255, 200, 80), new Color(230, 170, 50)),

        [MeteoriteId.SlaughterFragment] = new(MeteoriteId.SlaughterFragment, "학살의 운석 파편", "치명타 데미지 +5%",
            MeteoriteRarity.Blue, true, s => s.CritDamageMultiplier += 0.05f,
            new Color(255, 80, 80), new Color(230, 50, 50)),

        [MeteoriteId.GiantFragment] = new(MeteoriteId.GiantFragment, "거인의 운석 파편", "최대체력 +50",
            MeteoriteRarity.White, true, s => s.MaxHPBonus += 50f,
            new Color(100, 220, 120), new Color(70, 190, 90)),

        [MeteoriteId.GuardFragment] = new(MeteoriteId.GuardFragment, "수호의 운석 파편", "방어력 +5",
            MeteoriteRarity.White, true, s => s.Defense += 5f,
            new Color(160, 180, 200), new Color(130, 150, 170)),

        // ===== 고유 운석 =====
        [MeteoriteId.AfterImageMeteor] = new(MeteoriteId.AfterImageMeteor, "잔영의 운석",
            "공격 시 0.2초 뒤 같은 위치에 잔상 검격 발생",
            MeteoriteRarity.Red, false, s => s.AfterImage = true,
            new Color(180, 150, 255), new Color(150, 120, 230)),

        [MeteoriteId.CrescentMeteor] = new(MeteoriteId.CrescentMeteor, "초승의 운석",
            "3타 시 전방으로 초승달 검기 파동 발사",
            MeteoriteRarity.Red, false, s => s.CrescentWave = true,
            new Color(220, 240, 255), new Color(190, 210, 230)),

        [MeteoriteId.VampireMeteor] = new(MeteoriteId.VampireMeteor, "흡혈의 운석",
            "입힌 피해의 10% 체력 회복",
            MeteoriteRarity.Red, false, s => s.LifeSteal += 0.10f,
            new Color(255, 60, 80), new Color(220, 40, 60)),

        [MeteoriteId.DrawSlashMeteor] = new(MeteoriteId.DrawSlashMeteor, "발도의 운석",
            "대쉬 직후 공격 시 x2.0 데미지, 2배 범위",
            MeteoriteRarity.Gold, false, s => s.DrawSlash = true,
            new Color(255, 220, 100), new Color(230, 190, 70)),

        [MeteoriteId.CrackMeteor] = new(MeteoriteId.CrackMeteor, "균열의 운석",
            "3타 시 지면 균열 생성, 0.8초 뒤 폭발",
            MeteoriteRarity.Red, false, s => s.GroundCrack = true,
            new Color(180, 120, 80), new Color(150, 90, 50)),

        [MeteoriteId.ExplosionMeteor] = new(MeteoriteId.ExplosionMeteor, "폭발의 운석",
            "화염 공격 + 적 타격 시 폭발 (주변 x0.3)",
            MeteoriteRarity.Red, false, s => s.ExplosiveFlame = true,
            new Color(255, 120, 40), new Color(230, 90, 20)),

        [MeteoriteId.LightningMeteor] = new(MeteoriteId.LightningMeteor, "번개의 운석",
            "치명타 시 주변 적에게 번개",
            MeteoriteRarity.Red, false, s => s.CritLightning = true,
            new Color(130, 160, 255), new Color(100, 130, 230)),

        [MeteoriteId.WindMeteor] = new(MeteoriteId.WindMeteor, "바람의 운석",
            "3타 이후 5초간 공격속도 2배",
            MeteoriteRarity.Red, false, s => s.WindBurst = true,
            new Color(150, 230, 200), new Color(120, 200, 170)),
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
        new("집중의 공명", "잔영 + 균열의 추가 피해 2배",
            MeteoriteId.AfterImageMeteor, MeteoriteId.CrackMeteor,
            s => s.SynergyFocusResonance = true,
            new Color(200, 160, 255)),

        new("광전사의 공명", "체력 40% 미만시 흡혈/바람 효과 1.5배",
            MeteoriteId.VampireMeteor, MeteoriteId.WindMeteor,
            s => s.SynergyBerserkerResonance = true,
            new Color(255, 100, 100)),
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
    public const int MaxSlots = 15;

    private readonly Dictionary<MeteoriteId, int> _items = new();

    public IReadOnlyDictionary<MeteoriteId, int> Items => _items;

    public int UsedSlots
    {
        get
        {
            int slots = 0;
            foreach (var kvp in _items)
                slots += 1;
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
            return false;
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
            _items[id]--;
        else
            _items.Remove(id);
        return true;
    }

    public void RemoveAll(MeteoriteId id)
    {
        _items.Remove(id);
    }

    public void RecalculateStats(AugmentStats stats)
    {
        stats.Reset();

        foreach (var kvp in _items)
        {
            var info = MeteoriteDatabase.Get(kvp.Key);
            int count = info.Stackable ? kvp.Value : 1;
            for (int i = 0; i < count; i++)
                info.Apply(stats);
        }

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

// ===== 드롭 아이템 =====
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

    public static MeteoriteId RollMeteoriteId(int floor, bool isBoss)
    {
        float roll = (float)Random.Shared.NextDouble() * 100f;
        MeteoriteRarity targetRarity;

        if (isBoss)
        {
            float goldChance = Math.Min(5 + floor * 2f, 25f);
            float redChance = Math.Min(25 + floor * 3f, 45f);
            float blueChance = 25f;
            if (roll < goldChance) targetRarity = MeteoriteRarity.Gold;
            else if (roll < goldChance + redChance) targetRarity = MeteoriteRarity.Red;
            else if (roll < goldChance + redChance + blueChance) targetRarity = MeteoriteRarity.Blue;
            else targetRarity = MeteoriteRarity.White;
        }
        else
        {
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
