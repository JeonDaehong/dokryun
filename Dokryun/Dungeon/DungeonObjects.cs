using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Dungeon;

public enum DungeonObjectType
{
    TreasureChest,
    HealthPickup,
    KiPickup,
    BossPortal
}

public class DungeonObject
{
    public Vector2 Position { get; set; }
    public DungeonObjectType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOpened { get; set; }
    public float InteractRadius { get; set; } = 36f;
    public float AnimTimer { get; set; }

    public Rectangle Bounds => new Rectangle(
        (int)(Position.X - 12), (int)(Position.Y - 12), 24, 24);

    public void Update(float dt)
    {
        AnimTimer += dt;
    }

    public bool IsPlayerNear(Vector2 playerPos)
    {
        return Vector2.Distance(Position, playerPos) < InteractRadius;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsActive) return;

        switch (Type)
        {
            case DungeonObjectType.TreasureChest:
                DrawTreasureChest(spriteBatch, pixel);
                break;
            case DungeonObjectType.HealthPickup:
                DrawPickup(spriteBatch, pixel, new Color(220, 50, 50), new Color(255, 100, 100));
                break;
            case DungeonObjectType.KiPickup:
                DrawPickup(spriteBatch, pixel, new Color(50, 70, 200), new Color(100, 130, 255));
                break;
            case DungeonObjectType.BossPortal:
                DrawBossPortal(spriteBatch, pixel);
                break;
        }
    }

    private void DrawTreasureChest(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened)
        {
            // Opened chest (flat)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 10, (int)Position.Y - 4, 20, 12),
                new Color(120, 90, 30) * 0.5f);
            return;
        }

        float bob = MathF.Sin(AnimTimer * 2f) * 1.5f;
        int y = (int)(Position.Y + bob);

        // Chest body
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 10, y - 6, 20, 14),
            new Color(160, 120, 40));
        // Lid
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 11, y - 9, 22, 5),
            new Color(180, 140, 50));
        // Lock
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 2, y - 6, 4, 4),
            new Color(220, 200, 80));
        // Highlight
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 9, y - 8, 18, 1),
            new Color(220, 180, 80) * 0.5f);

        // Interact hint glow
        float glow = MathF.Sin(AnimTimer * 3f) * 0.2f + 0.3f;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 14, y - 12, 28, 22),
            new Color(200, 170, 50) * glow * 0.15f);
    }

    private void DrawBossPortal(SpriteBatch spriteBatch, Texture2D pixel)
    {
        float time = AnimTimer;
        float pulse = MathF.Sin(time * 3f) * 0.2f + 0.8f;
        int px = (int)Position.X;
        int py = (int)Position.Y;

        // Outer glow
        var glowColor = new Color(200, 50, 50) * (0.15f * pulse);
        spriteBatch.Draw(pixel, new Rectangle(px - 18, py - 18, 36, 36), glowColor);

        // Portal ring
        var ringColor = new Color(220, 40, 40) * pulse;
        spriteBatch.Draw(pixel, new Rectangle(px - 12, py - 14, 24, 28), ringColor);
        // Inner dark
        spriteBatch.Draw(pixel, new Rectangle(px - 8, py - 10, 16, 20), new Color(20, 5, 10));

        // Swirl highlights
        for (int i = 0; i < 3; i++)
        {
            float angle = time * 2f + i * MathHelper.TwoPi / 3f;
            int sx = px + (int)(MathF.Cos(angle) * 5);
            int sy = py + (int)(MathF.Sin(angle) * 7);
            spriteBatch.Draw(pixel, new Rectangle(sx - 1, sy - 1, 3, 3), new Color(255, 120, 60) * pulse);
        }

        // "BOSS" text hint
        float hintGlow = MathF.Sin(time * 2f) * 0.3f + 0.7f;
        spriteBatch.Draw(pixel, new Rectangle(px - 10, py - 22, 20, 3), new Color(255, 60, 40) * hintGlow * 0.6f);
    }

    private void DrawPickup(SpriteBatch spriteBatch, Texture2D pixel, Color main, Color highlight)
    {
        float bob = MathF.Sin(AnimTimer * 3f) * 2f;
        int y = (int)(Position.Y + bob);

        // Orb
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 5, y - 5, 10, 10), main);
        // Highlight
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 3, y - 4, 4, 3), highlight * 0.6f);
        // Glow
        float glow = MathF.Sin(AnimTimer * 4f) * 0.2f + 0.3f;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 8, y - 8, 16, 16), main * glow * 0.2f);
    }
}
