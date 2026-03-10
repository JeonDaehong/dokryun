using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public class Projectile
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Damage { get; set; }
    public float Life { get; set; }
    public float Size { get; set; } = 4f;
    public Color Color { get; set; } = Color.White;
    public bool IsPlayerOwned { get; set; }
    public bool IsActive { get; set; } = true;

    // Special properties
    public int PierceRemaining { get; set; } = 0;
    public bool Explosive { get; set; }
    public float ExplosionRadius { get; set; }
    public float ExplosionDamageRatio { get; set; }
    public int BounceRemaining { get; set; } = 0;
    public bool Homing { get; set; }
    public float HomingStrength { get; set; }
    public bool ChainLightning { get; set; }
    public float ChainDamage { get; set; }
    public int ChainCount { get; set; }
    public bool FlameTrail { get; set; }
    public bool FrostEffect { get; set; }
    public float FrostSlow { get; set; }

    // Trail timer
    public float TrailTimer { get; set; }

    // Crescent wave projectile
    public bool IsCrescent { get; set; }
    public float InitialLife { get; set; }

    public Rectangle Bounds => IsCrescent ? CrescentBounds : new Rectangle(
        (int)(Position.X - Size / 2), (int)(Position.Y - Size / 2),
        (int)Size, (int)Size);

    private Rectangle CrescentBounds
    {
        get
        {
            float lifeRatio = InitialLife > 0 ? Life / InitialLife : 1f;
            float scale = 1f + (1f - lifeRatio) * 0.3f;
            float radius = 45f * scale;
            float arcSpan = 2.8f;
            float travelAngle = MathF.Atan2(Velocity.Y, Velocity.X);

            // Compute bounding box of the crescent arc points
            float minX = Position.X, maxX = Position.X;
            float minY = Position.Y, maxY = Position.Y;
            for (int i = 0; i <= 8; i++)
            {
                float t = (float)i / 8;
                float angle = travelAngle - arcSpan / 2f + arcSpan * t;
                float px = Position.X + MathF.Cos(angle) * radius;
                float py = Position.Y + MathF.Sin(angle) * radius;
                if (px < minX) minX = px;
                if (px > maxX) maxX = px;
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
            }
            return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }
    }

    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        Position += Velocity * deltaTime;
        Life -= deltaTime;
        TrailTimer += deltaTime;

        if (Life <= 0)
            IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsActive) return;

        if (IsCrescent)
        {
            DrawCrescent(spriteBatch, pixel);
            return;
        }

        // Arrow shape: elongated in direction of travel
        float angle = MathF.Atan2(Velocity.Y, Velocity.X);
        float len = Size * 2.5f;
        float width = Size * 0.6f;

        var tip = Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * len * 0.5f;
        var tail = Position - new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * len * 0.5f;

        // Glow
        int glowSize = (int)(Size * 3f);
        spriteBatch.Draw(pixel,
            new Rectangle((int)(Position.X - glowSize / 2), (int)(Position.Y - glowSize / 2), glowSize, glowSize),
            Color * 0.2f);

        // Arrow body (3 segments for arrow shape)
        int bodyLen = (int)(len * 0.6f);
        int bodyW = (int)Math.Max(2, width);
        spriteBatch.Draw(pixel,
            new Rectangle((int)(tail.X - bodyW / 2), (int)(tail.Y - bodyW / 2), bodyLen, bodyW),
            null, Color * 0.8f, angle, Vector2.Zero, SpriteEffects.None, 0);

        // Arrow tip (brighter)
        spriteBatch.Draw(pixel,
            new Rectangle((int)tip.X - 2, (int)tip.Y - 2, 4, 4),
            Color);
    }

    private void DrawCrescent(SpriteBatch spriteBatch, Texture2D pixel)
    {
        float travelAngle = MathF.Atan2(Velocity.Y, Velocity.X);
        float lifeRatio = InitialLife > 0 ? Life / InitialLife : 1f;

        // Fade out near end of life
        float alpha = MathF.Min(1f, lifeRatio * 3f);
        // Scale up slightly over time for "expanding wave" feel
        float scale = 1f + (1f - lifeRatio) * 0.3f;

        float outerRadius = 45f * scale;
        float thickness = 18f * scale;
        float arcSpan = 2.8f; // wide crescent arc

        // Perpendicular to travel direction = crescent faces forward
        float centerAngle = travelAngle;

        int segments = 28;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = centerAngle - arcSpan / 2f + arcSpan * t;

            // Crescent shape: thickest at center, thin at tips
            float crescentT = MathF.Sin(t * MathF.PI);
            float currentThickness = thickness * crescentT;
            if (currentThickness < 1.5f) currentThickness = 1.5f;

            float outerR = outerRadius;
            float innerR = outerRadius - currentThickness;

            // Draw filled layers from inner to outer
            int layers = (int)(currentThickness / 2.5f) + 1;
            for (int layer = 0; layer < layers; layer++)
            {
                float lt = (float)layer / Math.Max(1, layers - 1);
                float r = innerR + (outerR - innerR) * lt;
                float px = Position.X + MathF.Cos(angle) * r;
                float py = Position.Y + MathF.Sin(angle) * r;

                // Core is bright white, edges are blue-tinted
                float coreness = 1f - lt; // 1 at inner, 0 at outer
                var coreColor = Color.Lerp(new Color(180, 220, 255), new Color(255, 250, 240), coreness);
                float pixelSize = 3f + crescentT * 2f;

                spriteBatch.Draw(pixel,
                    new Rectangle((int)(px - pixelSize / 2), (int)(py - pixelSize / 2), (int)pixelSize + 1, (int)pixelSize + 1),
                    coreColor * alpha * (0.6f + coreness * 0.4f));
            }

            // Outer edge glow (trailing sparkle)
            if (crescentT > 0.2f)
            {
                float gx = Position.X + MathF.Cos(angle) * (outerR + 4f);
                float gy = Position.Y + MathF.Sin(angle) * (outerR + 4f);
                float glowPixSize = 3f * crescentT * alpha;
                spriteBatch.Draw(pixel,
                    new Rectangle((int)(gx - glowPixSize / 2), (int)(gy - glowPixSize / 2), (int)glowPixSize + 1, (int)glowPixSize + 1),
                    new Color(200, 230, 255) * alpha * 0.5f);
            }
        }

        // Bright leading edge (the "blade" of the crescent)
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = centerAngle - arcSpan / 2f + arcSpan * t;
            float crescentT = MathF.Sin(t * MathF.PI);
            if (crescentT < 0.15f) continue;

            float px = Position.X + MathF.Cos(angle) * outerRadius;
            float py = Position.Y + MathF.Sin(angle) * outerRadius;
            float edgeSize = 2f + crescentT;
            spriteBatch.Draw(pixel,
                new Rectangle((int)(px - edgeSize / 2), (int)(py - edgeSize / 2), (int)edgeSize + 1, (int)edgeSize + 1),
                Color.White * alpha * 0.7f * crescentT);
        }
    }
}

public class ProjectileManager
{
    private readonly List<Projectile> _projectiles = new();

    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    public void Spawn(Vector2 position, Vector2 velocity, float damage, bool isPlayerOwned, Color color,
        float size = 4f, float life = 3f)
    {
        _projectiles.Add(new Projectile
        {
            Position = position,
            Velocity = velocity,
            Damage = damage,
            IsPlayerOwned = isPlayerOwned,
            Color = color,
            Size = size,
            Life = life
        });
    }

    /// <summary>플레이어 화살 발사</summary>
    public Projectile SpawnPlayerArrow(Vector2 origin, Vector2 direction, float damage, float speed, float size, Color color)
    {
        if (direction.LengthSquared() > 0)
            direction = Vector2.Normalize(direction);
        else
            direction = Vector2.UnitX;

        var proj = new Projectile
        {
            Position = origin,
            Velocity = direction * speed,
            Damage = damage,
            IsPlayerOwned = true,
            Color = color,
            Size = size,
            Life = 2f
        };
        _projectiles.Add(proj);
        return proj;
    }

    /// <summary>적 화살 (경고 이펙트 포함)</summary>
    public void SpawnArrow(Vector2 origin, Vector2 target, float damage)
    {
        var dir = target - origin;
        if (dir.LengthSquared() > 0)
            dir.Normalize();
        Spawn(origin, dir * 200f, damage, false, new Color(255, 80, 50), 3f, 2f);
    }

    /// <summary>적 돌진 공격 투사체 (근접 공격용)</summary>
    public void SpawnMeleeStrike(Vector2 origin, Vector2 direction, float damage)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        var proj = new Projectile
        {
            Position = origin + direction * 15f,
            Velocity = direction * 350f,
            Damage = damage,
            IsPlayerOwned = false,
            Color = new Color(255, 60, 40),
            Size = 8f,
            Life = 0.15f
        };
        _projectiles.Add(proj);
    }

    /// <summary>화살비용 화살</summary>
    public Projectile SpawnRainArrow(Vector2 position, float damage, Color color, float size)
    {
        var proj = new Projectile
        {
            Position = position + new Vector2(0, -200),
            Velocity = new Vector2(
                (float)(Random.Shared.NextDouble() * 40 - 20),
                350f + (float)Random.Shared.NextDouble() * 100f),
            Damage = damage,
            IsPlayerOwned = true,
            Color = color,
            Size = size,
            Life = 1.5f
        };
        _projectiles.Add(proj);
        return proj;
    }

    public void Update(float deltaTime)
    {
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            _projectiles[i].Update(deltaTime);
            if (!_projectiles[i].IsActive)
                _projectiles.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var p in _projectiles)
            p.Draw(spriteBatch, pixel);
    }

    public void Clear() => _projectiles.Clear();
}
