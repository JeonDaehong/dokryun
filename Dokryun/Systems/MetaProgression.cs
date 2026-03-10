using System.IO;
using System.Text.Json;

namespace Dokryun.Systems;

public class MetaProgression
{
    private const string SaveFile = "meta_save.json";

    // 업(業) 포인트
    public int Karma { get; set; }
    public int TotalRuns { get; set; }
    public int BestFloor { get; set; }
    public int TotalKills { get; set; }

    // 영구 업그레이드 레벨 (0 = 미구매)
    public int MaxHPLevel { get; set; }       // 시작 체력 증가 (레벨당 +10, 최대 5)
    public int AttackLevel { get; set; }      // 시작 공격력 증가 (레벨당 +2, 최대 5)
    public int KiRegenLevel { get; set; }     // 기력 회복 증가 (레벨당 +0.3, 최대 5)
    public int DashLevel { get; set; }        // 대쉬 충전 속도 (레벨당 -0.5초, 최대 3)
    public int StartAugmentLevel { get; set; } // 시작 시 증강 1개 (0 or 1)

    // 해금된 운석 풀 (추후 확장용)
    public List<string> UnlockedMeteors { get; set; } = new();

    public static readonly UpgradeInfo[] Upgrades = new[]
    {
        new UpgradeInfo("체력 강화", "시작 체력 +10", 5, new[] { 30, 60, 100, 150, 220 }),
        new UpgradeInfo("공격 강화", "시작 공격력 +2", 5, new[] { 40, 80, 130, 190, 260 }),
        new UpgradeInfo("기력 수련", "기력 회복 +0.3/초", 5, new[] { 35, 70, 120, 170, 240 }),
        new UpgradeInfo("보법 강화", "대쉬 충전 -0.5초", 3, new[] { 50, 100, 180 }),
        new UpgradeInfo("초기 증강", "시작 시 증강 선택", 1, new[] { 200 }),
    };

    public int GetLevel(int upgradeIndex) => upgradeIndex switch
    {
        0 => MaxHPLevel,
        1 => AttackLevel,
        2 => KiRegenLevel,
        3 => DashLevel,
        4 => StartAugmentLevel,
        _ => 0
    };

    public bool CanUpgrade(int upgradeIndex)
    {
        var info = Upgrades[upgradeIndex];
        int level = GetLevel(upgradeIndex);
        if (level >= info.MaxLevel) return false;
        return Karma >= info.Costs[level];
    }

    public bool TryUpgrade(int upgradeIndex)
    {
        if (!CanUpgrade(upgradeIndex)) return false;
        var info = Upgrades[upgradeIndex];
        int level = GetLevel(upgradeIndex);
        Karma -= info.Costs[level];

        switch (upgradeIndex)
        {
            case 0: MaxHPLevel++; break;
            case 1: AttackLevel++; break;
            case 2: KiRegenLevel++; break;
            case 3: DashLevel++; break;
            case 4: StartAugmentLevel++; break;
        }
        return true;
    }

    // 런 종료 시 업 포인트 계산
    public int CalculateKarma(int floor, int kills, bool bossDefeated)
    {
        int karma = floor * 10 + kills * 2;
        if (bossDefeated) karma += 50;
        return karma;
    }

    // 영구 스탯 적용
    public float GetBonusMaxHP() => MaxHPLevel * 10f;
    public float GetBonusAttack() => AttackLevel * 2f;
    public float GetBonusKiRegen() => KiRegenLevel * 0.3f;
    public float GetDashRechargeReduction() => DashLevel * 0.5f;

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFile, json);
        }
        catch { /* silent fail for now */ }
    }

    public static MetaProgression Load()
    {
        try
        {
            if (File.Exists(SaveFile))
            {
                var json = File.ReadAllText(SaveFile);
                return JsonSerializer.Deserialize<MetaProgression>(json) ?? new MetaProgression();
            }
        }
        catch { /* silent fail */ }
        return new MetaProgression();
    }
}

public record UpgradeInfo(string Name, string Description, int MaxLevel, int[] Costs);
