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

    public Rectangle Bounds => new Rectangle(
        (int)(Position.X - Size / 2), (int)(Position.Y - Size / 2),
        (int)Size, (int)Size);

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
