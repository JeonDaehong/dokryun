using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float Life;
    public float MaxLife;
    public float Size;
    public float Rotation;
    public float RotationSpeed;
    public float Gravity;
    public float Friction;
    public bool IsActive;

    public float Alpha => MaxLife > 0 ? Math.Max(0, Life / MaxLife) : 0;
    public float Progress => MaxLife > 0 ? 1f - (Life / MaxLife) : 1f;
}

public class ParticleSystem
{
    private Particle[] _particles;
    private int _count;
    private int _nextFree; // free-slot hint for O(1) emit
    private static Texture2D _pixel;

    public ParticleSystem(int maxParticles = 3000)
    {
        _particles = new Particle[maxParticles];
    }

    private static void EnsurePixel(GraphicsDevice device)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }
    }

    public void Emit(Vector2 position, Vector2 velocity, Color color, float life, float size, float gravity = 0f, float friction = 0f)
    {
        int len = _particles.Length;
        // Start from hint, wrap around once
        for (int j = 0; j < len; j++)
        {
            int i = (_nextFree + j) % len;
            if (!_particles[i].IsActive)
            {
                _particles[i] = new Particle
                {
                    Position = position,
                    Velocity = velocity,
                    Color = color,
                    Life = life,
                    MaxLife = life,
                    Size = size,
                    Rotation = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi),
                    RotationSpeed = (float)(Random.Shared.NextDouble() * 4 - 2),
                    Gravity = gravity,
                    Friction = friction,
                    IsActive = true
                };
                _count++;
                _nextFree = (i + 1) % len;
                return;
            }
        }
        // Pool full: recycle oldest (lowest life)
        int oldest = 0;
        float lowestLife = float.MaxValue;
        for (int i = 0; i < len; i++)
        {
            if (_particles[i].Life < lowestLife)
            {
                lowestLife = _particles[i].Life;
                oldest = i;
            }
        }
        _particles[oldest] = new Particle
        {
            Position = position, Velocity = velocity, Color = color,
            Life = life, MaxLife = life, Size = size,
            Rotation = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi),
            RotationSpeed = (float)(Random.Shared.NextDouble() * 4 - 2),
            Gravity = gravity, Friction = friction, IsActive = true
        };
        _nextFree = (oldest + 1) % len;
    }

    /// <summary>Hit spark burst - 타격 이펙트</summary>
    public void EmitBurst(Vector2 position, int count, Color color, float speed = 150f, float life = 0.3f, float size = 2f)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float spd = speed * (0.5f + (float)Random.Shared.NextDouble() * 0.5f);
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * spd;
            float l = life * (0.5f + (float)Random.Shared.NextDouble() * 0.5f);
            float s = size * (0.5f + (float)Random.Shared.NextDouble());
            Emit(position, vel, color, l, s, 0, 3f);
        }
    }

    /// <summary>방향성 스파크 - 타격 방향으로 튀는 불꽃</summary>
    public void EmitDirectionalSpark(Vector2 position, Vector2 direction, int count, Color color, float speed = 250f)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        float baseAngle = MathF.Atan2(direction.Y, direction.X);

        for (int i = 0; i < count; i++)
        {
            float spread = MathHelper.ToRadians(45);
            float angle = baseAngle + (float)(Random.Shared.NextDouble() * spread - spread / 2);
            float spd = speed * (0.4f + (float)Random.Shared.NextDouble() * 0.8f);
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * spd;
            float life = 0.15f + (float)Random.Shared.NextDouble() * 0.2f;
            float size = 1.5f + (float)Random.Shared.NextDouble() * 2f;
            Emit(position, vel, color, life, size, 0, 5f);
        }
    }

    /// <summary>칼 베기 호 - 부채꼴 슬래시 이펙트</summary>
    public void EmitSlashArc(Vector2 origin, float angle, float range, Color color, int count = 20)
    {
        float arcHalf = MathHelper.ToRadians(55);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float a = angle - arcHalf + arcHalf * 2 * t;
            float dist = range * (0.3f + (float)Random.Shared.NextDouble() * 0.7f);
            var pos = origin + new Vector2(MathF.Cos(a), MathF.Sin(a)) * dist;

            // Particles move outward along the arc
            float outSpeed = 80f + (float)Random.Shared.NextDouble() * 120f;
            var vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * outSpeed;

            // Vary color slightly
            float bright = 0.7f + (float)Random.Shared.NextDouble() * 0.3f;
            var c = new Color(
                (int)(color.R * bright),
                (int)(color.G * bright),
                (int)(color.B * bright)
            );

            float size = 1.5f + (float)Random.Shared.NextDouble() * 2.5f;
            Emit(pos, vel, c, 0.12f + (float)Random.Shared.NextDouble() * 0.1f, size, 0, 8f);
        }

        // Core bright line along arc edge
        for (int i = 0; i < 8; i++)
        {
            float t = i / 7f;
            float a = angle - arcHalf + arcHalf * 2 * t;
            var pos = origin + new Vector2(MathF.Cos(a), MathF.Sin(a)) * range * 0.8f;
            Emit(pos, Vector2.Zero, Color.White * 0.9f, 0.06f, 3f);
        }
    }

    /// <summary>Death explosion - 적 사망 대폭발</summary>
    public void EmitExplosion(Vector2 position, int count, Color color)
    {
        // Main burst
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 60f + (float)Random.Shared.NextDouble() * 250f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            float life = 0.3f + (float)Random.Shared.NextDouble() * 0.6f;
            float size = 1.5f + (float)Random.Shared.NextDouble() * 4f;
            Emit(position, vel, color, life, size, 120f, 2f);
        }

        // White flash core
        for (int i = 0; i < 6; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 20f + (float)Random.Shared.NextDouble() * 60f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            Emit(position + vel * 0.02f, vel * 0.5f, Color.White, 0.08f, 4f + (float)Random.Shared.NextDouble() * 3f);
        }

        // Lingering embers (gravity-affected)
        for (int i = 0; i < count / 2; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 30f + (float)Random.Shared.NextDouble() * 80f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed + new Vector2(0, -40);
            float life = 0.5f + (float)Random.Shared.NextDouble() * 0.8f;
            var emberColor = Color.Lerp(color, new Color(255, 200, 100), (float)Random.Shared.NextDouble() * 0.5f);
            Emit(position, vel, emberColor, life, 1f + (float)Random.Shared.NextDouble() * 1.5f, 150f);
        }
    }

    /// <summary>화염 폭발 - 폭발의 운석 전용 대형 폭발</summary>
    public void EmitFireExplosion(Vector2 position, float radius)
    {
        // 1) 거대 화염 파편 - 사방으로 퍼지는 불덩이
        for (int i = 0; i < 35; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 80f + (float)Random.Shared.NextDouble() * 320f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            float life = 0.4f + (float)Random.Shared.NextDouble() * 0.7f;
            float size = 4f + (float)Random.Shared.NextDouble() * 7f;
            var fireColor = Color.Lerp(new Color(255, 60, 10), new Color(255, 200, 30), (float)Random.Shared.NextDouble());
            Emit(position, vel, fireColor, life, size, 80f, 2f);
        }

        // 2) 중심부 밝은 폭발 코어 (노랑~흰)
        for (int i = 0; i < 12; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 30f + (float)Random.Shared.NextDouble() * 80f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            var coreColor = Color.Lerp(Color.White, new Color(255, 240, 120), (float)Random.Shared.NextDouble());
            Emit(position + vel * 0.01f, vel * 0.4f, coreColor, 0.12f, 6f + (float)Random.Shared.NextDouble() * 5f);
        }

        // 3) 검붉은 연기 (느리게 퍼지며 오래 지속)
        for (int i = 0; i < 20; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 20f + (float)Random.Shared.NextDouble() * 60f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed + new Vector2(0, -30);
            float life = 0.6f + (float)Random.Shared.NextDouble() * 1.0f;
            var smokeColor = Color.Lerp(new Color(80, 30, 10), new Color(180, 80, 20), (float)Random.Shared.NextDouble());
            Emit(position, vel, smokeColor, life, 5f + (float)Random.Shared.NextDouble() * 6f, 40f, 1f);
        }

        // 4) 불씨 (위로 솟구치는 잔불)
        for (int i = 0; i < 25; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float hSpeed = 40f + (float)Random.Shared.NextDouble() * 100f;
            var vel = new Vector2(MathF.Cos(angle) * hSpeed, -100f - (float)Random.Shared.NextDouble() * 200f);
            float life = 0.5f + (float)Random.Shared.NextDouble() * 1.2f;
            var emberColor = Color.Lerp(new Color(255, 120, 20), new Color(255, 220, 60), (float)Random.Shared.NextDouble());
            Emit(position, vel, emberColor, life, 1.5f + (float)Random.Shared.NextDouble() * 2.5f, 200f);
        }

        // 5) 폭발 링 (2중)
        EmitImpactRing(position, new Color(255, 140, 30), radius, 24);
        EmitImpactRing(position, new Color(255, 80, 10), radius * 0.65f, 16);
    }

    /// <summary>Dash trail - 대시 잔상</summary>
    public void EmitTrail(Vector2 position, Color color, float size = 3f)
    {
        for (int i = 0; i < 3; i++)
        {
            var offset = new Vector2(
                (float)(Random.Shared.NextDouble() * 8 - 4),
                (float)(Random.Shared.NextDouble() * 8 - 4)
            );
            Emit(position + offset, Vector2.Zero, color, 0.2f, size);
        }
    }

    /// <summary>스피드 라인 - 대시 시 속도감 표현</summary>
    public void EmitSpeedLines(Vector2 position, Vector2 direction, Color color, int count = 5)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        for (int i = 0; i < count; i++)
        {
            float offset = (float)(Random.Shared.NextDouble() * 40 - 20);
            var pos = position + perpendicular * offset;
            var vel = -direction * (200f + (float)Random.Shared.NextDouble() * 150f);
            Emit(pos, vel, color * 0.5f, 0.1f, 1.5f + (float)Random.Shared.NextDouble() * 1f, 0, 6f);
        }
    }

    /// <summary>임팩트 링 - 강한 타격 시 원형 이펙트</summary>
    public void EmitImpactRing(Vector2 position, Color color, float radius = 30f, int count = 24)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            var pos = position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius * 0.3f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius * 4f;
            Emit(pos, vel, color, 0.12f, 2.5f, 0, 8f);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].IsActive) continue;

            _particles[i].Life -= deltaTime;
            if (_particles[i].Life <= 0)
            {
                _particles[i].IsActive = false;
                _count--;
                continue;
            }

            _particles[i].Velocity.Y += _particles[i].Gravity * deltaTime;

            // Friction deceleration
            if (_particles[i].Friction > 0)
            {
                float friction = 1f - _particles[i].Friction * deltaTime;
                if (friction < 0) friction = 0;
                _particles[i].Velocity *= friction;
            }

            _particles[i].Position += _particles[i].Velocity * deltaTime;
            _particles[i].Rotation += _particles[i].RotationSpeed * deltaTime;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        EnsurePixel(spriteBatch.GraphicsDevice);

        for (int i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].IsActive) continue;

            ref var p = ref _particles[i];
            var color = p.Color * p.Alpha;
            float size = p.Size * (0.3f + 0.7f * p.Alpha); // shrink over time

            spriteBatch.Draw(
                _pixel,
                p.Position,
                null,
                color,
                p.Rotation,
                new Vector2(0.5f, 0.5f),
                size,
                SpriteEffects.None,
                0f
            );
        }
    }
}
