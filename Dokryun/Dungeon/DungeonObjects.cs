using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Dungeon;

public enum DungeonObjectType
{
    TreasureChest,
    HealthPickup,
    KiPickup,
    BossPortal,
    // Event room markers
    ShopNPC,
    Altar,
    HealingSpring,
    GamblingDen,
    // Environmental hazards
    PoisonTrap,
    SpikeTrap,
}

public class DungeonObject
{
    public Vector2 Position { get; set; }
    public DungeonObjectType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOpened { get; set; }
    public float InteractRadius { get; set; } = 36f;
    public float AnimTimer { get; set; }
    public int EventIndex { get; set; } = -1; // links to EventRoomData index

    // Hazard
    public float HazardCooldown { get; set; }

    public Rectangle Bounds => new Rectangle(
        (int)(Position.X - 12), (int)(Position.Y - 12), 24, 24);

    public void Update(float dt)
    {
        AnimTimer += dt;
        if (HazardCooldown > 0) HazardCooldown -= dt;
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
            case DungeonObjectType.ShopNPC:
                DrawShopNPC(spriteBatch, pixel);
                break;
            case DungeonObjectType.Altar:
                DrawAltar(spriteBatch, pixel);
                break;
            case DungeonObjectType.HealingSpring:
                DrawHealingSpring(spriteBatch, pixel);
                break;
            case DungeonObjectType.GamblingDen:
                DrawGamblingDen(spriteBatch, pixel);
                break;
            case DungeonObjectType.PoisonTrap:
                DrawPoisonTrap(spriteBatch, pixel);
                break;
            case DungeonObjectType.SpikeTrap:
                DrawSpikeTrap(spriteBatch, pixel);
                break;
        }
    }

    private void DrawTreasureChest(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened)
        {
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 10, (int)Position.Y - 4, 20, 12),
                new Color(120, 90, 30) * 0.5f);
            return;
        }

        float bob = MathF.Sin(AnimTimer * 2f) * 1.5f;
        int y = (int)(Position.Y + bob);

        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 10, y - 6, 20, 14), new Color(160, 120, 40));
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 11, y - 9, 22, 5), new Color(180, 140, 50));
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 2, y - 6, 4, 4), new Color(220, 200, 80));
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 9, y - 8, 18, 1), new Color(220, 180, 80) * 0.5f);

        float glow = MathF.Sin(AnimTimer * 3f) * 0.2f + 0.3f;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 14, y - 12, 28, 22), new Color(200, 170, 50) * glow * 0.15f);
    }

    private void DrawBossPortal(SpriteBatch spriteBatch, Texture2D pixel)
    {
        float time = AnimTimer;
        float pulse = MathF.Sin(time * 3f) * 0.2f + 0.8f;
        int px = (int)Position.X;
        int py = (int)Position.Y;

        var glowColor = new Color(200, 50, 50) * (0.15f * pulse);
        spriteBatch.Draw(pixel, new Rectangle(px - 18, py - 18, 36, 36), glowColor);

        var ringColor = new Color(220, 40, 40) * pulse;
        spriteBatch.Draw(pixel, new Rectangle(px - 12, py - 14, 24, 28), ringColor);
        spriteBatch.Draw(pixel, new Rectangle(px - 8, py - 10, 16, 20), new Color(20, 5, 10));

        for (int i = 0; i < 3; i++)
        {
            float angle = time * 2f + i * MathHelper.TwoPi / 3f;
            int sx = px + (int)(MathF.Cos(angle) * 5);
            int sy = py + (int)(MathF.Sin(angle) * 7);
            spriteBatch.Draw(pixel, new Rectangle(sx - 1, sy - 1, 3, 3), new Color(255, 120, 60) * pulse);
        }

        float hintGlow = MathF.Sin(time * 2f) * 0.3f + 0.7f;
        spriteBatch.Draw(pixel, new Rectangle(px - 10, py - 22, 20, 3), new Color(255, 60, 40) * hintGlow * 0.6f);
    }

    private void DrawShopNPC(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened) return;
        int px = (int)Position.X;
        int py = (int)Position.Y;
        float bob = MathF.Sin(AnimTimer * 1.5f) * 1f;

        // NPC body
        spriteBatch.Draw(pixel, new Rectangle(px - 8, (int)(py - 12 + bob), 16, 24), new Color(100, 80, 50));
        // Head
        spriteBatch.Draw(pixel, new Rectangle(px - 5, (int)(py - 18 + bob), 10, 8), new Color(200, 180, 140));
        // Hat (gat)
        spriteBatch.Draw(pixel, new Rectangle(px - 8, (int)(py - 22 + bob), 16, 4), new Color(30, 25, 20));
        spriteBatch.Draw(pixel, new Rectangle(px - 4, (int)(py - 26 + bob), 8, 5), new Color(30, 25, 20));
        // Pack behind
        spriteBatch.Draw(pixel, new Rectangle(px + 6, (int)(py - 8 + bob), 8, 14), new Color(140, 110, 60));
        // Gold coin indicator
        float glow = MathF.Sin(AnimTimer * 3f) * 0.3f + 0.7f;
        spriteBatch.Draw(pixel, new Rectangle(px - 3, (int)(py + 14), 6, 6), new Color(255, 210, 60) * glow);
    }

    private void DrawAltar(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened) return;
        int px = (int)Position.X;
        int py = (int)Position.Y;

        // Stone base
        spriteBatch.Draw(pixel, new Rectangle(px - 14, py - 4, 28, 12), new Color(80, 75, 65));
        spriteBatch.Draw(pixel, new Rectangle(px - 12, py - 8, 24, 6), new Color(90, 85, 75));
        // Flame
        float flicker = MathF.Sin(AnimTimer * 8f) * 3f;
        var flameColor = Color.Lerp(new Color(200, 50, 50), new Color(255, 140, 40), MathF.Sin(AnimTimer * 6f) * 0.5f + 0.5f);
        spriteBatch.Draw(pixel, new Rectangle(px - 3, (int)(py - 18 + flicker), 6, 10), flameColor);
        spriteBatch.Draw(pixel, new Rectangle(px - 1, (int)(py - 22 + flicker), 2, 5), flameColor * 0.7f);
        // Glow
        float glow = MathF.Sin(AnimTimer * 4f) * 0.15f + 0.2f;
        spriteBatch.Draw(pixel, new Rectangle(px - 18, py - 24, 36, 36), new Color(200, 80, 40) * glow * 0.2f);
    }

    private void DrawHealingSpring(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened) return;
        int px = (int)Position.X;
        int py = (int)Position.Y;

        // Water pool
        float ripple = MathF.Sin(AnimTimer * 2f) * 1f;
        spriteBatch.Draw(pixel, new Rectangle(px - 14, (int)(py - 6 + ripple), 28, 14), new Color(40, 120, 180) * 0.6f);
        spriteBatch.Draw(pixel, new Rectangle(px - 10, (int)(py - 4 + ripple), 20, 10), new Color(60, 160, 220) * 0.7f);
        // Sparkles
        float sparkle = MathF.Sin(AnimTimer * 5f);
        if (sparkle > 0.3f)
        {
            spriteBatch.Draw(pixel, new Rectangle(px - 6, (int)(py - 8), 2, 2), new Color(180, 240, 255) * sparkle);
            spriteBatch.Draw(pixel, new Rectangle(px + 4, (int)(py - 4), 2, 2), new Color(180, 240, 255) * sparkle * 0.7f);
        }
        // Stone border
        spriteBatch.Draw(pixel, new Rectangle(px - 16, py - 8, 2, 18), new Color(90, 85, 70) * 0.8f);
        spriteBatch.Draw(pixel, new Rectangle(px + 14, py - 8, 2, 18), new Color(90, 85, 70) * 0.8f);
    }

    private void DrawGamblingDen(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsOpened) return;
        int px = (int)Position.X;
        int py = (int)Position.Y;

        // Table
        spriteBatch.Draw(pixel, new Rectangle(px - 12, py - 4, 24, 12), new Color(100, 70, 30));
        spriteBatch.Draw(pixel, new Rectangle(px - 10, py - 6, 20, 4), new Color(120, 90, 40));
        // Dice
        float roll = MathF.Sin(AnimTimer * 4f);
        int diceX = px - 4 + (int)(roll * 2);
        spriteBatch.Draw(pixel, new Rectangle(diceX, py - 10, 6, 6), Color.White * 0.9f);
        spriteBatch.Draw(pixel, new Rectangle(diceX + 1, py - 9, 1, 1), new Color(30, 30, 30));
        spriteBatch.Draw(pixel, new Rectangle(diceX + 3, py - 7, 1, 1), new Color(30, 30, 30));
        // Glow
        float glow = MathF.Sin(AnimTimer * 3f) * 0.2f + 0.3f;
        spriteBatch.Draw(pixel, new Rectangle(px - 16, py - 14, 32, 28), new Color(255, 200, 50) * glow * 0.1f);
    }

    private void DrawPoisonTrap(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int px = (int)Position.X;
        int py = (int)Position.Y;
        float pulse = MathF.Sin(AnimTimer * 3f) * 0.15f + 0.25f;
        // Green mist
        spriteBatch.Draw(pixel, new Rectangle(px - 10, py - 6, 20, 12), new Color(60, 180, 40) * pulse);
        // Bubbles
        if (AnimTimer % 0.4f < 0.1f)
        {
            float bx = px - 5 + MathF.Sin(AnimTimer * 7f) * 6;
            spriteBatch.Draw(pixel, new Rectangle((int)bx, py - 8, 3, 3), new Color(80, 220, 60) * 0.5f);
        }
    }

    private void DrawSpikeTrap(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int px = (int)Position.X;
        int py = (int)Position.Y;
        // Spike pattern (periodic activation)
        bool extended = MathF.Sin(AnimTimer * 2f) > 0.3f;
        if (extended)
        {
            // Spikes up
            for (int i = 0; i < 3; i++)
            {
                int sx = px - 6 + i * 6;
                spriteBatch.Draw(pixel, new Rectangle(sx, py - 8, 2, 8), new Color(160, 150, 130));
                spriteBatch.Draw(pixel, new Rectangle(sx, py - 10, 2, 3), new Color(200, 190, 170));
            }
        }
        // Base plate
        spriteBatch.Draw(pixel, new Rectangle(px - 8, py - 2, 16, 4), new Color(80, 75, 60));
    }

    private void DrawPickup(SpriteBatch spriteBatch, Texture2D pixel, Color main, Color highlight)
    {
        float bob = MathF.Sin(AnimTimer * 3f) * 2f;
        int y = (int)(Position.Y + bob);

        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 5, y - 5, 10, 10), main);
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 3, y - 4, 4, 3), highlight * 0.6f);
        float glow = MathF.Sin(AnimTimer * 4f) * 0.2f + 0.3f;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 8, y - 8, 16, 16), main * glow * 0.2f);
    }
}
