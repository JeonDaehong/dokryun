using Dokryun.Dungeon;
using Dokryun.Entities;

namespace Dokryun.Systems;

public class StageData
{
    public string Name { get; set; }
    public string Region { get; set; }
    public string BossName { get; set; }
    public ThemeType Theme { get; set; }
    public int FloorCount { get; set; }
    public EnemyType BossType { get; set; }

    public static readonly StageData[] Stages = new[]
    {
        new StageData
        {
            Name = "월악산의 도깨비왕",
            Region = "충청도",
            BossName = "월악 도깨비왕",
            Theme = ThemeType.ChungcheongMountain,
            FloorCount = 5,
            BossType = EnemyType.DokkaebiKing
        }
        // Future stages can be added here
    };

    public bool IsBossFloor(int floor) => floor >= FloorCount;
}
