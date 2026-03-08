using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

/// <summary>도깨비 주머니: 3초 후 십자 폭발</summary>
public class BossPouch
{
    public Vector2 Position { get; set; }
    public float Timer { get; set; }
    public float MaxTimer { get; set; }
    public float Damage { get; set; }
    public float CrossLength { get; set; } = 120f;
    public float CrossWidth { get; set; } = 20f;
    public bool IsActive { get; set; } = true;
    public bool HasExploded { get; set; }

    // Explosion visual
    public float ExplosionTimer { get; set; }

    public void Update(float dt)
    {
        if (!IsActive) return;

        if (HasExploded)
        {
            ExplosionTimer -= dt;
            if (ExplosionTimer <= 0) IsActive = false;
            return;
        }

        Timer -= dt;
        if (Timer <= 0)
        {
            HasExploded = true;
            ExplosionTimer = 0.4f;
        }
    }

    public bool IsInCrossExplosion(Vector2 target)
    {
        if (!HasExploded || ExplosionTimer < 0.2f) return false; // only damage in first 0.2s

        float dx = MathF.Abs(target.X - Position.X);
        float dy = MathF.Abs(target.Y - Position.Y);

        // Horizontal arm
        if (dy < CrossWidth / 2f && dx < CrossLength) return true;
        // Vertical arm
        if (dx < CrossWidth / 2f && dy < CrossLength) return true;

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsActive) return;

        int px = (int)Position.X;
        int py = (int)Position.Y;

        if (HasExploded)
        {
            // Cross explosion effect
            float t = ExplosionTimer / 0.4f;
            float alpha = t;
            int armLen = (int)(CrossLength * (1f - t * 0.3f));
            int armW = (int)(CrossWidth * (0.5f + t * 0.5f));
            var color = Color.Lerp(new Color(255, 200, 50), new Color(255, 60, 20), 1f - t) * alpha;
            var coreColor = Color.Lerp(Color.White, new Color(255, 100, 30), 1f - t) * alpha;

            // Horizontal arm
            spriteBatch.Draw(pixel, new Rectangle(px - armLen, py - armW / 2, armLen * 2, armW), color);
            // Vertical arm
            spriteBatch.Draw(pixel, new Rectangle(px - armW / 2, py - armLen, armW, armLen * 2), color);
            // Core
            spriteBatch.Draw(pixel, new Rectangle(px - armW, py - armW, armW * 2, armW * 2), coreColor);
            return;
        }

        // Ticking pouch
        float timeRatio = Timer / MaxTimer;
        float pulse = MathF.Sin((1f - timeRatio) * 30f) * 0.3f + 0.7f;

        // Warning zone (cross preview, faint)
        if (timeRatio < 0.4f)
        {
            float warnAlpha = (0.4f - timeRatio) / 0.4f * 0.15f * pulse;
            int warnLen = (int)CrossLength;
            int warnW = (int)CrossWidth;
            var warnColor = new Color(255, 50, 30) * warnAlpha;
            spriteBatch.Draw(pixel, new Rectangle(px - warnLen, py - warnW / 2, warnLen * 2, warnW), warnColor);
            spriteBatch.Draw(pixel, new Rectangle(px - warnW / 2, py - warnLen, warnW, warnLen * 2), warnColor);
        }

        // Pouch body
        var pouchColor = Color.Lerp(new Color(120, 80, 40), new Color(255, 60, 30), (1f - timeRatio) * pulse);
        spriteBatch.Draw(pixel, new Rectangle(px - 7, py - 8, 14, 16), pouchColor);
        // Tie
        spriteBatch.Draw(pixel, new Rectangle(px - 3, py - 11, 6, 4), new Color(180, 140, 60));
        // String
        spriteBatch.Draw(pixel, new Rectangle(px - 1, py - 13, 2, 3), new Color(160, 120, 50));

        // Sparking fuse when close to exploding
        if (timeRatio < 0.5f)
        {
            float sparkPulse = MathF.Sin(Timer * 20f);
            if (sparkPulse > 0)
            {
                var sparkColor = new Color(255, 220, 80) * (1f - timeRatio);
                spriteBatch.Draw(pixel, new Rectangle(px - 1, py - 15, 3, 3), sparkColor);
            }
        }
    }
}
