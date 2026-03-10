using Microsoft.Xna.Framework;

namespace Dokryun.Systems;

public enum EventType
{
    Shop,           // 상점 - 운석 구매/판매
    Altar,          // 제단 - 체력 대가로 강력한 효과
    HealingSpring,  // 치유의 샘 - 체력 회복
    GamblingDen,    // 도박장 - 랜덤 운석 (좋거나 나쁘거나)
}

public class EventRoomData
{
    public EventType Type { get; set; }
    public Vector2 Position { get; set; }
    public bool IsUsed { get; set; }

    // Shop data
    public List<ShopItem> ShopItems { get; set; } = new();

    // Altar data
    public float AltarHPCost { get; set; }
    public MeteoriteId? AltarReward { get; set; }

    // Gambling data
    public bool GambleResult { get; set; } // true = good, false = bad

    public static EventRoomData CreateShop(Vector2 position, int floor)
    {
        var ev = new EventRoomData { Type = EventType.Shop, Position = position };

        // Generate 3 items for sale
        int itemCount = 3;
        for (int i = 0; i < itemCount; i++)
        {
            var meteId = DroppedItem.RollMeteoriteId(floor, false);
            var info = MeteoriteDatabase.Get(meteId);
            int price = info.Rarity switch
            {
                MeteoriteRarity.White => 15 + floor * 3,
                MeteoriteRarity.Blue => 30 + floor * 5,
                MeteoriteRarity.Red => 50 + floor * 8,
                MeteoriteRarity.Gold => 80 + floor * 12,
                _ => 20
            };
            ev.ShopItems.Add(new ShopItem { MeteoriteId = meteId, Price = price, IsSold = false });
        }
        return ev;
    }

    public static EventRoomData CreateAltar(Vector2 position, int floor)
    {
        var ev = new EventRoomData { Type = EventType.Altar, Position = position };
        ev.AltarHPCost = 30f + floor * 5f;

        // Higher floor = better chance at rare items
        float roll = (float)Random.Shared.NextDouble();
        if (roll < 0.3f + floor * 0.05f)
            ev.AltarReward = DroppedItem.RollMeteoriteId(floor + 2, true); // biased toward better
        else
            ev.AltarReward = DroppedItem.RollMeteoriteId(floor, false);

        return ev;
    }

    public static EventRoomData CreateHealingSpring(Vector2 position)
    {
        return new EventRoomData { Type = EventType.HealingSpring, Position = position };
    }

    public static EventRoomData CreateGamblingDen(Vector2 position, int floor)
    {
        var ev = new EventRoomData { Type = EventType.GamblingDen, Position = position };
        ev.GambleResult = Random.Shared.NextDouble() < 0.55; // 55% good
        return ev;
    }
}

public class ShopItem
{
    public MeteoriteId MeteoriteId { get; set; }
    public int Price { get; set; }
    public bool IsSold { get; set; }
}
