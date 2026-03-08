using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Dungeon;

public enum ThemeType
{
    ChungcheongMountain // 충청도 월악산
}

public class TileTheme
{
    private const int TS = 32; // TileSize

    // Generated tile textures
    public Texture2D[] Floor { get; private set; }
    public Texture2D[] FloorAlt { get; private set; }
    public Texture2D[] WallInner { get; private set; }
    public Texture2D[] WallFace { get; private set; }
    public Texture2D WaterBase { get; private set; }
    public Texture2D EntranceTile { get; private set; }

    // Edge decoration textures (grass tufts, vines at wall edges)
    public Texture2D EdgeGrassTop { get; private set; }
    public Texture2D EdgeGrassLeft { get; private set; }
    public Texture2D EdgeGrassRight { get; private set; }

    // Ambient colors for this theme
    public Color AmbientLight { get; private set; }
    public Color FogColor { get; private set; }

    public ThemeType Type { get; private set; }

    public void Generate(GraphicsDevice device, ThemeType theme)
    {
        Type = theme;
        var rng = new Random(42);

        switch (theme)
        {
            case ThemeType.ChungcheongMountain:
                GenerateMountain(device, rng);
                break;
        }
    }

    private void GenerateMountain(GraphicsDevice device, Random rng)
    {
        AmbientLight = new Color(20, 30, 15);
        FogColor = new Color(40, 50, 35);

        // Forest floor (dirt + leaves + pebbles)
        Floor = new Texture2D[4];
        for (int i = 0; i < 4; i++)
            Floor[i] = GenForestFloor(device, rng);

        // Mossy/grassy floor variant
        FloorAlt = new Texture2D[3];
        for (int i = 0; i < 3; i++)
            FloorAlt[i] = GenMossyFloor(device, rng);

        // Wall inner (dense tree canopy from above)
        WallInner = new Texture2D[3];
        for (int i = 0; i < 3; i++)
            WallInner[i] = GenTreeCanopy(device, rng);

        // Wall face (rocky cliff / tree trunk side view)
        WallFace = new Texture2D[2];
        for (int i = 0; i < 2; i++)
            WallFace[i] = GenCliffFace(device, rng);

        WaterBase = GenMountainStream(device, rng);
        EntranceTile = GenEntrance(device, rng);

        // Edge decorations
        EdgeGrassTop = GenEdgeGrass(device, rng, false);
        EdgeGrassLeft = GenEdgeGrassSide(device, rng, true);
        EdgeGrassRight = GenEdgeGrassSide(device, rng, false);
    }

    // ==================== TILE GENERATORS ====================

    private Texture2D GenForestFloor(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Base earthy brown with subtle checker
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            int checker = ((x / 8 + y / 8) % 2 == 0) ? 0 : -3;
            int n = rng.Next(-10, 11);
            data[y * TS + x] = C(78 + n + checker, 64 + n + checker - 2, 40 + n + checker - 3);
        }

        // Pebbles (darker clusters)
        for (int p = 0; p < rng.Next(4, 8); p++)
        {
            int px = rng.Next(2, 30), py = rng.Next(2, 30);
            var c = C(58 + rng.Next(10), 48 + rng.Next(10), 32 + rng.Next(8));
            SP(data, px, py, c);
            if (rng.NextDouble() < 0.6) SP(data, px + 1, py, Darker(c, 5));
            if (rng.NextDouble() < 0.4) SP(data, px, py + 1, Darker(c, 3));
        }

        // Fallen leaves (orange/brown/red specks)
        for (int l = 0; l < rng.Next(3, 7); l++)
        {
            int lx = rng.Next(1, 31), ly = rng.Next(1, 31);
            Color lc = rng.Next(3) switch
            {
                0 => C(130 + rng.Next(30), 75 + rng.Next(20), 25 + rng.Next(10)), // orange
                1 => C(110 + rng.Next(20), 55 + rng.Next(15), 20),                // brown
                _ => C(140 + rng.Next(20), 50 + rng.Next(15), 30)                 // reddish
            };
            SP(data, lx, ly, lc);
            if (rng.NextDouble() < 0.5)
                SP(data, lx + (rng.NextDouble() < 0.5 ? 1 : -1), ly, Darker(lc, 10));
        }

        // Tiny moss specks
        for (int m = 0; m < rng.Next(2, 5); m++)
        {
            int mx = rng.Next(TS), my = rng.Next(TS);
            SP(data, mx, my, C(45 + rng.Next(20), 70 + rng.Next(25), 28 + rng.Next(12)));
        }

        // Subtle dirt cracks
        if (rng.NextDouble() < 0.4)
        {
            int cx = rng.Next(5, 25);
            int cy = rng.Next(5, 25);
            int len = rng.Next(4, 10);
            var crackColor = C(60, 50, 32);
            for (int i = 0; i < len; i++)
            {
                SP(data, cx + i, cy + (i % 3 == 0 ? 1 : 0), crackColor);
            }
        }

        return CreateTex(device, data);
    }

    private Texture2D GenMossyFloor(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Base green-brown
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            int n = rng.Next(-8, 9);
            data[y * TS + x] = C(52 + n, 72 + n, 34 + n - 2);
        }

        // Grass blade lines (short vertical strokes)
        for (int g = 0; g < rng.Next(6, 12); g++)
        {
            int gx = rng.Next(TS), gy = rng.Next(2, TS - 4);
            int height = rng.Next(2, 5);
            var gc = C(40 + rng.Next(20), 85 + rng.Next(25), 30 + rng.Next(15));
            for (int h = 0; h < height; h++)
                SP(data, gx, gy - h, h == 0 ? Brighter(gc, 15) : gc);
        }

        // Dirt patches showing through
        for (int d = 0; d < rng.Next(2, 4); d++)
        {
            int dx = rng.Next(3, 28), dy = rng.Next(3, 28);
            var dc = C(75 + rng.Next(10), 60 + rng.Next(10), 38);
            SP(data, dx, dy, dc);
            SP(data, dx + 1, dy, dc);
        }

        // Small flowers (occasional bright specks)
        if (rng.NextDouble() < 0.3)
        {
            int fx = rng.Next(4, 28), fy = rng.Next(4, 28);
            SP(data, fx, fy, C(200 + rng.Next(55), 180 + rng.Next(55), 80 + rng.Next(40)));
        }

        return CreateTex(device, data);
    }

    private Texture2D GenTreeCanopy(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Very dark green base
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            int n = rng.Next(-5, 6);
            data[y * TS + x] = C(18 + n, 30 + n, 12 + n);
        }

        // Leaf clusters (brighter green circles)
        for (int lc = 0; lc < rng.Next(4, 8); lc++)
        {
            int cx = rng.Next(4, 28), cy = rng.Next(4, 28);
            int r = rng.Next(2, 5);
            var leafColor = C(30 + rng.Next(20), 55 + rng.Next(25), 20 + rng.Next(12));
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy <= r * r + rng.Next(2))
                    SP(data, cx + dx, cy + dy, leafColor);
            }
            // Highlight on top
            SP(data, cx - 1, cy - 1, Brighter(leafColor, 15));
        }

        // Branch/trunk hints (brown lines)
        if (rng.NextDouble() < 0.5)
        {
            int bx = rng.Next(10, 22), by = rng.Next(8, 24);
            var branchColor = C(45 + rng.Next(10), 32 + rng.Next(8), 18);
            for (int i = 0; i < rng.Next(3, 7); i++)
                SP(data, bx + i, by + (i % 2), branchColor);
        }

        return CreateTex(device, data);
    }

    private Texture2D GenCliffFace(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Rock face - gradient from light top to dark bottom
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            float yRatio = y / (float)TS;
            int n = rng.Next(-8, 9);
            int baseR = (int)(70 - yRatio * 20) + n;
            int baseG = (int)(58 - yRatio * 18) + n;
            int baseB = (int)(38 - yRatio * 12) + n;
            data[y * TS + x] = C(baseR, baseG, baseB);
        }

        // Horizontal strata lines
        for (int s = 0; s < rng.Next(3, 6); s++)
        {
            int sy = rng.Next(4, 28);
            var sc = C(55 + rng.Next(10), 45 + rng.Next(10), 30);
            for (int x = rng.Next(4); x < TS - rng.Next(4); x++)
                SP(data, x, sy, sc);
        }

        // Cracks
        for (int cr = 0; cr < rng.Next(1, 3); cr++)
        {
            int cx = rng.Next(6, 26), cy = rng.Next(6, 26);
            var cc = C(35, 28, 18);
            int len = rng.Next(3, 8);
            for (int i = 0; i < len; i++)
                SP(data, cx, cy + i, cc);
        }

        // Moss on rock
        for (int m = 0; m < rng.Next(2, 5); m++)
        {
            int mx = rng.Next(TS), my = rng.Next(TS / 3); // moss prefers top
            SP(data, mx, my, C(40 + rng.Next(15), 60 + rng.Next(15), 25));
        }

        // Top edge highlight (light hitting from above)
        for (int x = 0; x < TS; x++)
        {
            int n = rng.Next(-3, 4);
            SP(data, x, 0, C(85 + n, 72 + n, 50 + n));
            SP(data, x, 1, C(78 + n, 65 + n, 44 + n));
        }

        // Bottom shadow
        for (int x = 0; x < TS; x++)
        {
            SP(data, x, TS - 1, C(25, 20, 14));
            SP(data, x, TS - 2, C(30, 24, 16));
        }

        return CreateTex(device, data);
    }

    private Texture2D GenMountainStream(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Dark teal base
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            int n = rng.Next(-5, 6);
            data[y * TS + x] = C(22 + n, 52 + n, 70 + n);
        }

        // Lighter ripple lines
        for (int r = 0; r < rng.Next(2, 4); r++)
        {
            int ry = rng.Next(4, 28);
            var rc = C(35, 70, 95);
            for (int x = rng.Next(3); x < TS - rng.Next(3); x += 2)
                SP(data, x, ry + (x % 4 == 0 ? 1 : 0), rc);
        }

        // Bright reflection specks
        for (int s = 0; s < rng.Next(2, 5); s++)
        {
            int sx = rng.Next(TS), sy = rng.Next(TS);
            SP(data, sx, sy, C(80, 130, 160));
        }

        // Rocky bottom showing through
        for (int rb = 0; rb < rng.Next(2, 4); rb++)
        {
            int bx = rng.Next(4, 28), by = rng.Next(4, 28);
            SP(data, bx, by, C(40, 55, 55));
            SP(data, bx + 1, by, C(38, 52, 52));
        }

        return CreateTex(device, data);
    }

    private Texture2D GenEntrance(GraphicsDevice device, Random rng)
    {
        var data = new Color[TS * TS];

        // Lighter floor base
        for (int y = 0; y < TS; y++)
        for (int x = 0; x < TS; x++)
        {
            int n = rng.Next(-6, 7);
            data[y * TS + x] = C(88 + n, 75 + n, 50 + n);
        }

        // Cross-shaped marker
        var mc = C(110, 95, 65);
        for (int i = 12; i < 20; i++)
        {
            SP(data, i, 15, mc); SP(data, i, 16, mc);
            SP(data, 15, i, mc); SP(data, 16, i, mc);
        }

        return CreateTex(device, data);
    }

    private Texture2D GenEdgeGrass(GraphicsDevice device, Random rng, bool bottom)
    {
        var data = new Color[TS * TS];
        // Transparent base
        for (int i = 0; i < data.Length; i++)
            data[i] = Color.Transparent;

        // Grass tufts hanging from top (wall above, floor below)
        for (int t = 0; t < rng.Next(5, 10); t++)
        {
            int gx = rng.Next(TS);
            int height = rng.Next(2, 6);
            var gc = C(35 + rng.Next(25), 65 + rng.Next(30), 25 + rng.Next(15));
            for (int h = 0; h < height; h++)
            {
                int y = bottom ? (TS - 1 - h) : h;
                SP(data, gx, y, h == height - 1 ? Brighter(gc, 10) : gc);
            }
        }

        return CreateTex(device, data);
    }

    private Texture2D GenEdgeGrassSide(GraphicsDevice device, Random rng, bool left)
    {
        var data = new Color[TS * TS];
        for (int i = 0; i < data.Length; i++)
            data[i] = Color.Transparent;

        for (int t = 0; t < rng.Next(4, 8); t++)
        {
            int gy = rng.Next(TS);
            int width = rng.Next(2, 5);
            var gc = C(35 + rng.Next(25), 65 + rng.Next(30), 25 + rng.Next(15));
            for (int w = 0; w < width; w++)
            {
                int x = left ? w : (TS - 1 - w);
                SP(data, x, gy, gc);
            }
        }

        return CreateTex(device, data);
    }

    // ==================== HELPERS ====================

    private static void SP(Color[] data, int x, int y, Color c)
    {
        if (x >= 0 && x < TS && y >= 0 && y < TS)
            data[y * TS + x] = c;
    }

    private static Color C(int r, int g, int b)
        => new Color(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));

    private static Color Darker(Color c, int amount)
        => C(c.R - amount, c.G - amount, c.B - amount);

    private static Color Brighter(Color c, int amount)
        => C(c.R + amount, c.G + amount, c.B + amount);

    private static Texture2D CreateTex(GraphicsDevice device, Color[] data)
    {
        var tex = new Texture2D(device, TS, TS);
        tex.SetData(data);
        return tex;
    }

    /// <summary>Get floor texture variant by tile position (deterministic)</summary>
    public Texture2D GetFloor(int tileX, int tileY)
        => Floor[(tileX * 7 + tileY * 13) % Floor.Length];

    public Texture2D GetFloorAlt(int tileX, int tileY)
        => FloorAlt[(tileX * 5 + tileY * 11) % FloorAlt.Length];

    public Texture2D GetWallInner(int tileX, int tileY)
        => WallInner[(tileX * 3 + tileY * 7) % WallInner.Length];

    public Texture2D GetWallFace(int tileX, int tileY)
        => WallFace[(tileX * 5 + tileY * 3) % WallFace.Length];
}
