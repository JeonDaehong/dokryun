using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Systems;

public enum EliteModifier
{
    None,
    Berserker,   // 광폭 - 속도/공격력 1.5배, 붉은 오라
    Ironclad,    // 철갑 - 체력 2.5배, 방어 느낌, 회색 오라
    Swift,       // 질풍 - 속도 2배, 노란 오라
    Vampiric,    // 흡혈 - 공격 시 회복, 보라 오라
    Explosive    // 폭발 - 사망 시 폭발, 주황 오라
}

public static class EliteSystem
{
    public static bool ShouldBeElite(int floor)
    {
        // Floor 2+ 부터 엘리트 등장, 층이 올라갈수록 확률 증가
        if (floor < 2) return false;
        float chance = Math.Min(0.08f + (floor - 2) * 0.04f, 0.25f);
        return Random.Shared.NextDouble() < chance;
    }

    public static EliteModifier RollModifier()
    {
        var mods = new[] { EliteModifier.Berserker, EliteModifier.Ironclad, EliteModifier.Swift, EliteModifier.Vampiric, EliteModifier.Explosive };
        return mods[Random.Shared.Next(mods.Length)];
    }

    public static void ApplyModifier(Entities.Enemy enemy, EliteModifier mod)
    {
        switch (mod)
        {
            case EliteModifier.Berserker:
                enemy.MaxHP *= 1.3f;
                enemy.HP = enemy.MaxHP;
                enemy.Speed *= 1.5f;
                enemy.BaseSpeed *= 1.5f;
                enemy.Attack *= 1.5f;
                break;
            case EliteModifier.Ironclad:
                enemy.MaxHP *= 2.5f;
                enemy.HP = enemy.MaxHP;
                enemy.Speed *= 0.8f;
                enemy.BaseSpeed *= 0.8f;
                break;
            case EliteModifier.Swift:
                enemy.MaxHP *= 1.2f;
                enemy.HP = enemy.MaxHP;
                enemy.Speed *= 2f;
                enemy.BaseSpeed *= 2f;
                break;
            case EliteModifier.Vampiric:
                enemy.MaxHP *= 1.5f;
                enemy.HP = enemy.MaxHP;
                enemy.Attack *= 1.3f;
                break;
            case EliteModifier.Explosive:
                enemy.MaxHP *= 1.4f;
                enemy.HP = enemy.MaxHP;
                enemy.Attack *= 1.2f;
                break;
        }
    }

    public static Color GetAuraColor(EliteModifier mod) => mod switch
    {
        EliteModifier.Berserker => new Color(255, 60, 40),
        EliteModifier.Ironclad => new Color(160, 160, 180),
        EliteModifier.Swift => new Color(255, 220, 60),
        EliteModifier.Vampiric => new Color(180, 60, 200),
        EliteModifier.Explosive => new Color(255, 140, 40),
        _ => Color.White
    };

    public static string GetModifierName(EliteModifier mod) => mod switch
    {
        EliteModifier.Berserker => "광폭",
        EliteModifier.Ironclad => "철갑",
        EliteModifier.Swift => "질풍",
        EliteModifier.Vampiric => "흡혈",
        EliteModifier.Explosive => "폭발",
        _ => ""
    };
}
