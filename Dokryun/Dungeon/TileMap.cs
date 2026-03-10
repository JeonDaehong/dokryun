using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Dungeon;

public enum TileType
{
    Wall,
    Floor,
    FloorAlt,
    WallTop,
    Water,
    Portal,
    PortalLocked,
    TreasureSpot,
    Entrance
}

public class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public const int TileSize = 32;

    public TileType[,] Tiles { get; }

    public Vector2 PlayerSpawn { get; set; }
    public Vector2 PortalPosition { get; set; }
    public Vector2? BossSpawn { get; set; }
    public List<Vector2> EnemySpawns { get; set; } = new();
    public List<Vector2> TreasurePositions { get; set; } = new();

    public TileTheme Theme { get; set; }

    // Fog of war: tracks which tiles have been revealed
    public bool[,] Revealed { get; private set; }

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new TileType[width, height];
        Revealed = new bool[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            Tiles[x, y] = TileType.Wall;
    }

    /// <summary>
    /// Reveal tiles around the player position within a given tile radius.
    /// </summary>
    public void RevealAround(Vector2 worldPos, int tileRadius = 6)
    {
        var (cx, cy) = WorldToTile(worldPos);
        int r2 = tileRadius * tileRadius;
        for (int x = cx - tileRadius; x <= cx + tileRadius; x++)
        for (int y = cy - tileRadius; y <= cy + tileRadius; y++)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) continue;
            int dx = x - cx, dy = y - cy;
            if (dx * dx + dy * dy <= r2)
                Revealed[x, y] = true;
        }
    }

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return TileType.Wall;
        return Tiles[x, y];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            Tiles[x, y] = type;
    }

    public bool IsWalkable(int x, int y)
    {
        var tile = GetTile(x, y);
        return tile != TileType.Wall && tile != TileType.WallTop && tile != TileType.Water;
    }

    public bool IsWalkableWorld(Vector2 worldPos)
    {
        var (tx, ty) = WorldToTile(worldPos);
        return IsWalkable(tx, ty);
    }

    public Vector2 TileToWorld(int x, int y)
    {
        return new Vector2(x * TileSize + TileSize / 2f, y * TileSize + TileSize / 2f);
    }

    public (int x, int y) WorldToTile(Vector2 worldPos)
    {
        return ((int)(worldPos.X / TileSize), (int)(worldPos.Y / TileSize));
    }

    public Vector2 ResolveCollision(Vector2 position, int entityW, int entityH)
    {
        float halfW = entityW / 2f;
        float halfH = entityH / 2f;
        float newX = position.X;
        float newY = position.Y;

        if (!IsWalkableWorld(new Vector2(newX - halfW, newY - halfH + 2)) ||
            !IsWalkableWorld(new Vector2(newX - halfW, newY + halfH - 2)))
        {
            int tileX = (int)((newX - halfW) / TileSize);
            newX = (tileX + 1) * TileSize + halfW;
        }
        if (!IsWalkableWorld(new Vector2(newX + halfW, newY - halfH + 2)) ||
            !IsWalkableWorld(new Vector2(newX + halfW, newY + halfH - 2)))
        {
            int tileX = (int)((newX + halfW) / TileSize);
            newX = tileX * TileSize - halfW;
        }
        if (!IsWalkableWorld(new Vector2(newX - halfW + 2, newY - halfH)) ||
            !IsWalkableWorld(new Vector2(newX + halfW - 2, newY - halfH)))
        {
            int tileY = (int)((newY - halfH) / TileSize);
            newY = (tileY + 1) * TileSize + halfH;
        }
        if (!IsWalkableWorld(new Vector2(newX - halfW + 2, newY + halfH)) ||
            !IsWalkableWorld(new Vector2(newX + halfW - 2, newY + halfH)))
        {
            int tileY = (int)((newY + halfH) / TileSize);
            newY = tileY * TileSize - halfH;
        }

        return new Vector2(newX, newY);
    }

    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        var (x0, y0) = WorldToTile(from);
        var (x1, y1) = WorldToTile(to);

        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if (!IsWalkable(x0, y0)) return false;
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Rectangle cameraBounds, float gameTimer)
    {
        int startX = Math.Max(0, cameraBounds.Left / TileSize - 1);
        int endX = Math.Min(Width, cameraBounds.Right / TileSize + 2);
        int startY = Math.Max(0, cameraBounds.Top / TileSize - 1);
        int endY = Math.Min(Height, cameraBounds.Bottom / TileSize + 2);

        bool useTheme = Theme != null;

        for (int x = startX; x < endX; x++)
        for (int y = startY; y < endY; y++)
        {
            var tile = Tiles[x, y];
            var rect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);

            if (useTheme)
                DrawThemedTile(spriteBatch, pixel, tile, x, y, rect, gameTimer);
            else
                DrawFallbackTile(spriteBatch, pixel, tile, x, y, rect, gameTimer);
        }

        // Edge decorations (grass/vine at wall-floor transitions)
        if (useTheme)
        {
            for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
            {
                if (!IsWalkable(x, y)) continue;
                var rect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);

                if (GetTile(x, y - 1) == TileType.Wall)
                    spriteBatch.Draw(Theme.EdgeGrassTop, rect, Color.White * 0.8f);
                if (GetTile(x - 1, y) == TileType.Wall)
                    spriteBatch.Draw(Theme.EdgeGrassLeft, rect, Color.White * 0.7f);
                if (GetTile(x + 1, y) == TileType.Wall)
                    spriteBatch.Draw(Theme.EdgeGrassRight, rect, Color.White * 0.7f);
            }
        }

        // Wall depth borders
        for (int x = startX; x < endX; x++)
        for (int y = startY; y < endY; y++)
        {
            if (Tiles[x, y] != TileType.Wall) continue;
            if (x + 1 < Width && IsWalkable(x + 1, y))
                spriteBatch.Draw(pixel, new Rectangle((x + 1) * TileSize, y * TileSize, 2, TileSize),
                    new Color(10, 8, 5) * 0.5f);
            if (x - 1 >= 0 && IsWalkable(x - 1, y))
                spriteBatch.Draw(pixel, new Rectangle(x * TileSize - 2, y * TileSize, 2, TileSize),
                    new Color(10, 8, 5) * 0.5f);
        }
    }

    private void DrawThemedTile(SpriteBatch spriteBatch, Texture2D pixel, TileType tile, int x, int y, Rectangle rect, float gameTimer)
    {
        switch (tile)
        {
            case TileType.Floor:
                spriteBatch.Draw(Theme.GetFloor(x, y), rect, Color.White);
                break;
            case TileType.FloorAlt:
                spriteBatch.Draw(Theme.GetFloorAlt(x, y), rect, Color.White);
                break;
            case TileType.Wall:
                if (y + 1 < Height && IsWalkable(x, y + 1))
                {
                    spriteBatch.Draw(Theme.GetWallFace(x, y), rect, Color.White);
                    spriteBatch.Draw(pixel, new Rectangle(x * TileSize, (y + 1) * TileSize - 2, TileSize, 2),
                        new Color(30, 25, 18) * 0.4f);
                }
                else
                {
                    spriteBatch.Draw(Theme.GetWallInner(x, y), rect, Color.White);
                }
                break;
            case TileType.WallTop:
                spriteBatch.Draw(Theme.GetWallInner(x, y), rect, Color.White);
                break;
            case TileType.Water:
                spriteBatch.Draw(Theme.WaterBase, rect, Color.White);
                float shimmer = MathF.Sin(gameTimer * 2f + x * 0.5f + y * 0.3f) * 0.15f;
                spriteBatch.Draw(pixel, rect, new Color(60, 100, 140) * (shimmer + 0.05f));
                break;
            case TileType.Portal:
                spriteBatch.Draw(Theme.GetFloor(x, y), rect, Color.White);
                DrawPortalEffect(spriteBatch, pixel, x, y, gameTimer, true);
                break;
            case TileType.PortalLocked:
                spriteBatch.Draw(Theme.GetFloor(x, y), rect, Color.White);
                DrawPortalEffect(spriteBatch, pixel, x, y, gameTimer, false);
                break;
            case TileType.TreasureSpot:
                spriteBatch.Draw(Theme.GetFloor(x, y), rect, Color.White);
                break;
            case TileType.Entrance:
                spriteBatch.Draw(Theme.EntranceTile, rect, Color.White);
                break;
            default:
                spriteBatch.Draw(pixel, rect, new Color(20, 16, 12));
                break;
        }
    }

    private void DrawFallbackTile(SpriteBatch spriteBatch, Texture2D pixel, TileType tile, int x, int y, Rectangle rect, float gameTimer)
    {
        Color color;
        switch (tile)
        {
            case TileType.Floor:
                color = ((x + y) % 2 == 0) ? new Color(32, 26, 20) : new Color(28, 23, 17);
                break;
            case TileType.FloorAlt:
                color = new Color(36, 30, 22);
                break;
            case TileType.Wall:
                color = new Color(18, 14, 10);
                if (y + 1 < Height && IsWalkable(x, y + 1))
                    color = new Color(55, 42, 30);
                break;
            case TileType.WallTop:
                color = new Color(65, 50, 35);
                break;
            case TileType.Water:
                float sh = MathF.Sin(gameTimer * 2f + x * 0.5f + y * 0.3f) * 0.1f;
                color = new Color(20 + (int)(sh * 30), 30 + (int)(sh * 20), 55);
                break;
            case TileType.Portal:
                spriteBatch.Draw(pixel, rect, new Color(32, 26, 20));
                DrawPortalEffect(spriteBatch, pixel, x, y, gameTimer, true);
                return;
            case TileType.PortalLocked:
                spriteBatch.Draw(pixel, rect, new Color(32, 26, 20));
                DrawPortalEffect(spriteBatch, pixel, x, y, gameTimer, false);
                return;
            case TileType.TreasureSpot:
                spriteBatch.Draw(pixel, rect, new Color(32, 26, 20));
                float sparkle = MathF.Sin(gameTimer * 4f + x) * 0.3f + 0.7f;
                var tc = new Color(200, 170, 50) * sparkle;
                spriteBatch.Draw(pixel, new Rectangle(x * TileSize + 8, y * TileSize + 10, TileSize - 16, TileSize - 16), tc * 0.6f);
                spriteBatch.Draw(pixel, new Rectangle(x * TileSize + 6, y * TileSize + 8, TileSize - 12, 4), tc * 0.8f);
                return;
            case TileType.Entrance:
                color = new Color(40, 34, 26);
                break;
            default:
                color = new Color(20, 16, 12);
                break;
        }
        spriteBatch.Draw(pixel, rect, color);
        if (tile == TileType.Wall && y + 1 < Height && IsWalkable(x, y + 1))
            spriteBatch.Draw(pixel, new Rectangle(x * TileSize, (y + 1) * TileSize - 2, TileSize, 2), new Color(40, 32, 22) * 0.5f);
    }

    private void DrawPortalEffect(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, float gameTimer, bool active)
    {
        int cx = x * TileSize + TileSize / 2;
        int cy = y * TileSize + TileSize / 2;

        if (active)
        {
            float glow = MathF.Sin(gameTimer * 3f) * 0.3f + 0.7f;
            var pc = new Color(80, 200, 220);

            // Large outer glow (3x3 tile area)
            int glowSize = TileSize * 3;
            spriteBatch.Draw(pixel, new Rectangle(cx - glowSize / 2, cy - glowSize / 2, glowSize, glowSize),
                pc * (0.08f + glow * 0.06f));

            // Medium glow (2x2)
            int midSize = TileSize * 2;
            spriteBatch.Draw(pixel, new Rectangle(cx - midSize / 2, cy - midSize / 2, midSize, midSize),
                pc * (0.12f + glow * 0.08f));

            // Inner portal circle layers
            int ps = TileSize + 8;
            spriteBatch.Draw(pixel, new Rectangle(cx - ps / 2, cy - ps / 2, ps, ps), pc * 0.25f);
            int inner = TileSize - 4;
            spriteBatch.Draw(pixel, new Rectangle(cx - inner / 2, cy - inner / 2, inner, inner), pc * (0.4f * glow));
            int core = TileSize - 12;
            spriteBatch.Draw(pixel, new Rectangle(cx - core / 2, cy - core / 2, core, core), pc * (0.6f * glow));

            // Bright center
            spriteBatch.Draw(pixel, new Rectangle(cx - 4, cy - 4, 8, 8), Color.White * (0.5f * glow));

            // 8 orbiting particles (two rings)
            for (int i = 0; i < 8; i++)
            {
                float angle = gameTimer * 2.5f + i * MathHelper.PiOver4;
                float r = 18f + MathF.Sin(gameTimer * 3f + i) * 4f;
                int ppx = cx + (int)(MathF.Cos(angle) * r);
                int ppy = cy + (int)(MathF.Sin(angle) * r);
                spriteBatch.Draw(pixel, new Rectangle(ppx - 2, ppy - 2, 4, 4), pc * 0.9f);
            }
            for (int i = 0; i < 6; i++)
            {
                float angle = -gameTimer * 1.8f + i * MathHelper.TwoPi / 6f;
                float r = 28f + MathF.Sin(gameTimer * 2f + i * 2f) * 5f;
                int ppx = cx + (int)(MathF.Cos(angle) * r);
                int ppy = cy + (int)(MathF.Sin(angle) * r);
                spriteBatch.Draw(pixel, new Rectangle(ppx - 1, ppy - 1, 3, 3), pc * 0.6f);
            }

            // Vertical light beam
            float beamAlpha = 0.15f + glow * 0.1f;
            spriteBatch.Draw(pixel, new Rectangle(cx - 3, cy - TileSize * 2, 6, TileSize * 2), pc * beamAlpha);
            spriteBatch.Draw(pixel, new Rectangle(cx - 1, cy - TileSize * 2, 2, TileSize * 2), Color.White * (beamAlpha * 0.5f));
        }
        else
        {
            // Locked portal: still visible but muted
            var lc = new Color(80, 70, 55);

            // Subtle outer glow
            int glowSize = TileSize * 2;
            spriteBatch.Draw(pixel, new Rectangle(cx - glowSize / 2, cy - glowSize / 2, glowSize, glowSize),
                lc * 0.06f);

            // Stone circle
            int ps = TileSize + 4;
            spriteBatch.Draw(pixel, new Rectangle(cx - ps / 2, cy - ps / 2, ps, ps), lc * 0.3f);
            int inner = TileSize - 4;
            spriteBatch.Draw(pixel, new Rectangle(cx - inner / 2, cy - inner / 2, inner, inner), lc * 0.5f);

            // Lock symbol (cross)
            spriteBatch.Draw(pixel, new Rectangle(cx - 4, cy - 6, 8, 12), lc * 0.7f);
            spriteBatch.Draw(pixel, new Rectangle(cx - 6, cy - 3, 12, 6), lc * 0.7f);

            // Dim pulsing
            float pulse = MathF.Sin(gameTimer * 1.5f) * 0.1f + 0.1f;
            spriteBatch.Draw(pixel, new Rectangle(cx - inner / 2, cy - inner / 2, inner, inner), new Color(60, 50, 40) * pulse);
        }
    }

    public void DrawMinimap(SpriteBatch spriteBatch, Texture2D pixel, Rectangle mapArea, Vector2 playerPos, List<Vector2> enemyPositions, float gameTimer = 0f)
    {
        float scaleX = (float)mapArea.Width / (Width * TileSize);
        float scaleY = (float)mapArea.Height / (Height * TileSize);

        spriteBatch.Draw(pixel, mapArea, new Color(6, 4, 2) * 0.9f);

        int step = Math.Max(1, Width / mapArea.Width + 1);
        for (int x = 0; x < Width; x += step)
        for (int y = 0; y < Height; y += step)
        {
            if (!IsWalkable(x, y)) continue;
            // Fog of war: only show revealed tiles
            if (!Revealed[x, y]) continue;

            int mx = mapArea.X + (int)(x * TileSize * scaleX);
            int my = mapArea.Y + (int)(y * TileSize * scaleY);
            var tile = Tiles[x, y];
            Color c = tile == TileType.TreasureSpot ? new Color(200, 170, 50) :
                      new Color(50, 40, 30);
            if (tile == TileType.Portal || tile == TileType.PortalLocked)
            {
                var portalC = tile == TileType.Portal ? new Color(80, 220, 240) : new Color(100, 80, 60);
                float blink = tile == TileType.Portal ? (MathF.Sin(gameTimer * 5f) * 0.3f + 0.7f) : 0.5f;
                spriteBatch.Draw(pixel, new Rectangle(mx - 2, my - 2, 6, 6), portalC * blink);
                continue;
            }
            spriteBatch.Draw(pixel, new Rectangle(mx, my, 2, 2), c);
        }

        int px = mapArea.X + (int)(playerPos.X * scaleX);
        int py = mapArea.Y + (int)(playerPos.Y * scaleY);
        spriteBatch.Draw(pixel, new Rectangle(px - 1, py - 1, 3, 3), Color.White);

        // Only show enemies in revealed areas
        foreach (var epos in enemyPositions)
        {
            var (etx, ety) = WorldToTile(epos);
            if (etx >= 0 && etx < Width && ety >= 0 && ety < Height && Revealed[etx, ety])
            {
                int ex = mapArea.X + (int)(epos.X * scaleX);
                int ey = mapArea.Y + (int)(epos.Y * scaleY);
                spriteBatch.Draw(pixel, new Rectangle(ex, ey, 2, 2), new Color(200, 60, 50));
            }
        }

        DrawBorder(spriteBatch, pixel, mapArea, new Color(60, 50, 35));
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
