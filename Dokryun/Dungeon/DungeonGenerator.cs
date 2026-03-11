using Microsoft.Xna.Framework;

namespace Dokryun.Dungeon;

public static class DungeonGenerator
{
    private const int MinLeafSize = 10;
    private const int MinRoomSize = 6;
    private const int CorridorWidth = 4;

    public static TileMap Generate(int floor, Random rng, bool isBossFloor = false)
    {
        if (isBossFloor)
            return GenerateBossFloor(rng);

        // Map size scales with floor (reduced to 1/4 area)
        int width = 60 + Math.Min(floor * 4, 30);
        int height = 45 + Math.Min(floor * 3, 25);

        var map = new TileMap(width, height);
        var rooms = new List<Rectangle>();

        var root = new BspNode(2, 2, width - 4, height - 4);
        SplitNode(root, rng, 0);
        CarveRooms(root, map, rooms, rng);
        ConnectRooms(root, map, rng);
        AddDecorations(map, rooms, rng);

        // Entrance
        var entrance = rooms[0];
        var entranceCenter = new Vector2(
            (entrance.X + entrance.Width / 2) * TileMap.TileSize + TileMap.TileSize / 2f,
            (entrance.Y + entrance.Height / 2) * TileMap.TileSize + TileMap.TileSize / 2f);
        map.PlayerSpawn = entranceCenter;
        map.SetTile(entrance.X + entrance.Width / 2, entrance.Y + entrance.Height / 2, TileType.Entrance);

        // 포탈 제거됨 (1층만 사용)

        int treasureCount = 6 + rng.Next(5); // 6~10 per floor
        PlaceTreasures(map, rooms, entrance, entrance, treasureCount, rng);

        return map;
    }

    /// <summary>보스 층: 넓은 보스 방 + 입구 + 장식</summary>
    private static TileMap GenerateBossFloor(Random rng)
    {
        int width = 56;
        int height = 48;
        var map = new TileMap(width, height);

        // Entrance room (small)
        var entranceRoom = new Rectangle(4, height / 2 - 4, 8, 8);
        CarveRect(map, entranceRoom);

        // Boss room (large arena)
        int bossW = 28;
        int bossH = 24;
        var bossRoom = new Rectangle(width / 2 - bossW / 2 + 4, height / 2 - bossH / 2, bossW, bossH);
        CarveRect(map, bossRoom);

        // Wide corridor connecting entrance to boss room
        int corridorY = height / 2;
        for (int x = entranceRoom.Right; x < bossRoom.X; x++)
        for (int dy = -2; dy <= 2; dy++)
            map.SetTile(x, corridorY + dy, TileType.Floor);

        // 8 pillars around the arena (2x2 each)
        int pillarOx = 5;
        int pillarOy = 4;
        int[][] pillarPositions = new[]
        {
            new[] { bossRoom.X + pillarOx, bossRoom.Y + pillarOy },
            new[] { bossRoom.Right - pillarOx - 2, bossRoom.Y + pillarOy },
            new[] { bossRoom.X + pillarOx, bossRoom.Bottom - pillarOy - 2 },
            new[] { bossRoom.Right - pillarOx - 2, bossRoom.Bottom - pillarOy - 2 },
            new[] { bossRoom.X + bossW / 2 - 1, bossRoom.Y + 2 },
            new[] { bossRoom.X + bossW / 2 - 1, bossRoom.Bottom - 4 },
            new[] { bossRoom.X + 2, bossRoom.Y + bossH / 2 - 1 },
            new[] { bossRoom.Right - 4, bossRoom.Y + bossH / 2 - 1 },
        };
        foreach (var pp in pillarPositions)
        {
            for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 2; dy++)
                map.SetTile(pp[0] + dx, pp[1] + dy, TileType.Wall);
        }

        // Decorative water pools in corners
        for (int dx = 0; dx < 3; dx++)
        for (int dy = 0; dy < 3; dy++)
        {
            map.SetTile(bossRoom.X + 1 + dx, bossRoom.Y + 1 + dy, TileType.Water);
            map.SetTile(bossRoom.Right - 4 + dx, bossRoom.Y + 1 + dy, TileType.Water);
            map.SetTile(bossRoom.X + 1 + dx, bossRoom.Bottom - 4 + dy, TileType.Water);
            map.SetTile(bossRoom.Right - 4 + dx, bossRoom.Bottom - 4 + dy, TileType.Water);
        }

        // Floor variations
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (map.GetTile(x, y) == TileType.Floor && rng.NextDouble() < 0.1)
                map.SetTile(x, y, TileType.FloorAlt);
        }

        // Entrance
        int entranceCx = entranceRoom.X + entranceRoom.Width / 2;
        int entranceCy = entranceRoom.Y + entranceRoom.Height / 2;
        map.SetTile(entranceCx, entranceCy, TileType.Entrance);
        map.PlayerSpawn = map.TileToWorld(entranceCx, entranceCy);

        // Boss spawn (center of boss room)
        int bossCx = bossRoom.X + bossRoom.Width / 2;
        int bossCy = bossRoom.Y + bossRoom.Height / 2;
        map.BossSpawn = map.TileToWorld(bossCx, bossCy);

        // Portal behind boss (appears after boss dies)
        int portalX = bossRoom.X + bossRoom.Width / 2;
        int portalY = bossRoom.Y + 3;
        map.SetTile(portalX, portalY, TileType.PortalLocked);
        map.PortalPosition = map.TileToWorld(portalX, portalY);

        return map;
    }

    private static void CarveRect(TileMap map, Rectangle room)
    {
        for (int x = room.X; x < room.Right; x++)
        for (int y = room.Y; y < room.Bottom; y++)
            map.SetTile(x, y, TileType.Floor);
    }

    private static void SplitNode(BspNode node, Random rng, int depth)
    {
        if (node.Width < MinLeafSize * 2 && node.Height < MinLeafSize * 2) return;
        if (depth > 10) return;

        bool splitH;
        if (node.Width < MinLeafSize * 2) splitH = true;
        else if (node.Height < MinLeafSize * 2) splitH = false;
        else splitH = rng.NextDouble() < (node.Width > node.Height ? 0.3 : 0.7);

        if (splitH)
        {
            if (node.Height < MinLeafSize * 2) return;
            int split = MinLeafSize + rng.Next(node.Height - MinLeafSize * 2 + 1);
            node.Left = new BspNode(node.X, node.Y, node.Width, split);
            node.Right = new BspNode(node.X, node.Y + split, node.Width, node.Height - split);
        }
        else
        {
            if (node.Width < MinLeafSize * 2) return;
            int split = MinLeafSize + rng.Next(node.Width - MinLeafSize * 2 + 1);
            node.Left = new BspNode(node.X, node.Y, split, node.Height);
            node.Right = new BspNode(node.X + split, node.Y, node.Width - split, node.Height);
        }

        SplitNode(node.Left, rng, depth + 1);
        SplitNode(node.Right, rng, depth + 1);
    }

    private static void CarveRooms(BspNode node, TileMap map, List<Rectangle> rooms, Random rng)
    {
        if (node.Left == null && node.Right == null)
        {
            int roomW = MinRoomSize + rng.Next(Math.Max(1, node.Width - MinRoomSize - 2));
            int roomH = MinRoomSize + rng.Next(Math.Max(1, node.Height - MinRoomSize - 2));
            roomW = Math.Min(roomW, node.Width - 2);
            roomH = Math.Min(roomH, node.Height - 2);

            int roomX = node.X + 1 + rng.Next(Math.Max(1, node.Width - roomW - 2));
            int roomY = node.Y + 1 + rng.Next(Math.Max(1, node.Height - roomH - 2));

            var room = new Rectangle(roomX, roomY, roomW, roomH);
            node.Room = room;
            rooms.Add(room);

            for (int x = roomX; x < roomX + roomW; x++)
            for (int y = roomY; y < roomY + roomH; y++)
                map.SetTile(x, y, TileType.Floor);

            return;
        }

        if (node.Left != null) CarveRooms(node.Left, map, rooms, rng);
        if (node.Right != null) CarveRooms(node.Right, map, rooms, rng);
    }

    private static void ConnectRooms(BspNode node, TileMap map, Random rng)
    {
        if (node.Left == null || node.Right == null) return;

        ConnectRooms(node.Left, map, rng);
        ConnectRooms(node.Right, map, rng);

        var leftCenter = GetNodeCenter(node.Left);
        var rightCenter = GetNodeCenter(node.Right);

        if (rng.NextDouble() < 0.5)
        {
            CarveHCorridor(map, leftCenter.x, rightCenter.x, leftCenter.y);
            CarveVCorridor(map, leftCenter.y, rightCenter.y, rightCenter.x);
        }
        else
        {
            CarveVCorridor(map, leftCenter.y, rightCenter.y, leftCenter.x);
            CarveHCorridor(map, leftCenter.x, rightCenter.x, rightCenter.y);
        }
    }

    private static (int x, int y) GetNodeCenter(BspNode node)
    {
        if (node.Room.HasValue)
        {
            var r = node.Room.Value;
            return (r.X + r.Width / 2, r.Y + r.Height / 2);
        }
        if (node.Left != null) return GetNodeCenter(node.Left);
        return (node.X + node.Width / 2, node.Y + node.Height / 2);
    }

    private static void CarveHCorridor(TileMap map, int x1, int x2, int y)
    {
        int minX = Math.Min(x1, x2);
        int maxX = Math.Max(x1, x2);
        for (int x = minX; x <= maxX; x++)
        for (int dy = 0; dy < CorridorWidth; dy++)
        {
            int ty = y - CorridorWidth / 2 + dy;
            if (ty >= 0 && ty < map.Height && x >= 0 && x < map.Width)
            {
                if (map.GetTile(x, ty) == TileType.Wall)
                    map.SetTile(x, ty, TileType.Floor);
            }
        }
    }

    private static void CarveVCorridor(TileMap map, int y1, int y2, int x)
    {
        int minY = Math.Min(y1, y2);
        int maxY = Math.Max(y1, y2);
        for (int y = minY; y <= maxY; y++)
        for (int dx = 0; dx < CorridorWidth; dx++)
        {
            int tx = x - CorridorWidth / 2 + dx;
            if (tx >= 0 && tx < map.Width && y >= 0 && y < map.Height)
            {
                if (map.GetTile(tx, y) == TileType.Wall)
                    map.SetTile(tx, y, TileType.Floor);
            }
        }
    }

    private static void AddDecorations(TileMap map, List<Rectangle> rooms, Random rng)
    {
        // Floor variations
        for (int x = 0; x < map.Width; x++)
        for (int y = 0; y < map.Height; y++)
        {
            if (map.GetTile(x, y) == TileType.Floor && rng.NextDouble() < 0.08)
                map.SetTile(x, y, TileType.FloorAlt);
        }

        foreach (var room in rooms)
        {
            int area = room.Width * room.Height;

            // Water pools (larger rooms)
            if (rng.NextDouble() < 0.2 && room.Width >= 8 && room.Height >= 8)
            {
                int poolX = room.X + 2 + rng.Next(Math.Max(1, room.Width - 6));
                int poolY = room.Y + 2 + rng.Next(Math.Max(1, room.Height - 6));
                int poolW = 2 + rng.Next(3);
                int poolH = 2 + rng.Next(3);
                for (int px = poolX; px < poolX + poolW && px < room.Right - 1; px++)
                for (int py = poolY; py < poolY + poolH && py < room.Bottom - 1; py++)
                    map.SetTile(px, py, TileType.Water);
            }

            // Pillars (medium-large rooms): 1x1 or 2x2 stone pillars
            if (rng.NextDouble() < 0.35 && room.Width >= 10 && room.Height >= 10)
            {
                int pillarCount = 2 + rng.Next(3); // 2-4 pillars
                for (int p = 0; p < pillarCount; p++)
                {
                    int px = room.X + 3 + rng.Next(Math.Max(1, room.Width - 6));
                    int py = room.Y + 3 + rng.Next(Math.Max(1, room.Height - 6));
                    if (map.GetTile(px, py) == TileType.Floor)
                    {
                        map.SetTile(px, py, TileType.Wall);
                        // 50% chance for 2x2 pillar
                        if (rng.NextDouble() < 0.5 && px + 1 < room.Right - 1 && py + 1 < room.Bottom - 1)
                        {
                            map.SetTile(px + 1, py, TileType.Wall);
                            map.SetTile(px, py + 1, TileType.Wall);
                            map.SetTile(px + 1, py + 1, TileType.Wall);
                        }
                    }
                }
            }

            // Wall alcoves (indent walls for visual interest)
            if (rng.NextDouble() < 0.25 && room.Width >= 12)
            {
                // Top or bottom wall indentation
                bool top = rng.NextDouble() < 0.5;
                int alcoveX = room.X + 3 + rng.Next(Math.Max(1, room.Width - 8));
                int alcoveW = 2 + rng.Next(3);
                int alcoveY = top ? room.Y : room.Bottom - 1;
                for (int ax = alcoveX; ax < alcoveX + alcoveW && ax < room.Right - 2; ax++)
                {
                    if (top && map.GetTile(ax, alcoveY - 1) == TileType.Wall)
                        map.SetTile(ax, alcoveY - 1, TileType.Floor);
                    else if (!top && map.GetTile(ax, alcoveY + 1) == TileType.Wall)
                        map.SetTile(ax, alcoveY + 1, TileType.Floor);
                }
            }

            // Rubble clusters (scattered wall tiles for cover)
            if (rng.NextDouble() < 0.2 && area >= 80)
            {
                int rubbleCount = 2 + rng.Next(4);
                int cx = room.X + room.Width / 2 + rng.Next(5) - 2;
                int cy = room.Y + room.Height / 2 + rng.Next(5) - 2;
                for (int r = 0; r < rubbleCount; r++)
                {
                    int rx = cx + rng.Next(5) - 2;
                    int ry = cy + rng.Next(5) - 2;
                    if (rx > room.X + 1 && rx < room.Right - 2 && ry > room.Y + 1 && ry < room.Bottom - 2)
                    {
                        if (map.GetTile(rx, ry) == TileType.Floor)
                            map.SetTile(rx, ry, TileType.Wall);
                    }
                }
            }

            // L-shaped wall formations
            if (rng.NextDouble() < 0.15 && room.Width >= 12 && room.Height >= 12)
            {
                int lx = room.X + 3 + rng.Next(Math.Max(1, room.Width - 8));
                int ly = room.Y + 3 + rng.Next(Math.Max(1, room.Height - 8));
                // Horizontal piece
                for (int dx = 0; dx < 3; dx++)
                    if (lx + dx < room.Right - 2) map.SetTile(lx + dx, ly, TileType.Wall);
                // Vertical piece
                for (int dy = 1; dy < 3; dy++)
                    if (ly + dy < room.Bottom - 2) map.SetTile(lx, ly + dy, TileType.Wall);
            }
        }
    }

    private static Rectangle FindFarthestRoom(List<Rectangle> rooms, Rectangle from)
    {
        var fromCenter = new Vector2(from.X + from.Width / 2f, from.Y + from.Height / 2f);
        Rectangle farthest = rooms[rooms.Count - 1];
        float maxDist = 0;

        foreach (var room in rooms)
        {
            if (room == from) continue;
            var center = new Vector2(room.X + room.Width / 2f, room.Y + room.Height / 2f);
            float dist = Vector2.Distance(fromCenter, center);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthest = room;
            }
        }
        return farthest;
    }

    private static void PlaceTreasures(TileMap map, List<Rectangle> rooms, Rectangle entrance, Rectangle portalRoom, int count, Random rng)
    {
        var candidates = rooms.Where(r => r != entrance && r != portalRoom).ToList();
        if (candidates.Count == 0) candidates = rooms.Where(r => r != entrance).ToList();
        if (candidates.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var room = candidates[rng.Next(candidates.Count)];

            // Offset placement so multiple chests don't stack
            int tx = room.X + 2 + rng.Next(Math.Max(1, room.Width - 4));
            int ty = room.Y + 2 + rng.Next(Math.Max(1, room.Height - 4));
            if (map.IsWalkable(tx, ty))
            {
                map.SetTile(tx, ty, TileType.TreasureSpot);
                map.TreasurePositions.Add(map.TileToWorld(tx, ty));
            }
        }
    }

    private static void PlaceEnemies(TileMap map, List<Rectangle> rooms, Rectangle entrance, int floor, Random rng)
    {
        foreach (var room in rooms)
        {
            if (room == entrance) continue;

            int area = room.Width * room.Height;
            int baseCount = area / 20;
            int count = Math.Max(1, baseCount + floor / 2);

            for (int i = 0; i < count; i++)
            {
                int ex = room.X + 1 + rng.Next(Math.Max(1, room.Width - 2));
                int ey = room.Y + 1 + rng.Next(Math.Max(1, room.Height - 2));
                if (map.IsWalkable(ex, ey))
                    map.EnemySpawns.Add(map.TileToWorld(ex, ey));
            }
        }
    }

    private class BspNode
    {
        public int X, Y, Width, Height;
        public BspNode Left, Right;
        public Rectangle? Room;

        public BspNode(int x, int y, int w, int h)
        {
            X = x; Y = y; Width = w; Height = h;
        }
    }
}
