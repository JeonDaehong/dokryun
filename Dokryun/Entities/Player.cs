using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dokryun.Engine;

namespace Dokryun.Entities;

public class Player : Entity
{
    public float MaxHP { get; set; } = 100f;
    public float HP { get; set; } = 100f;
    public float MaxKi { get; set; } = 50f;
    public float Ki { get; set; } = 50f;
    public float KiRegen { get; set; } = 5f;
    public float Speed { get; set; } = 200f;
    public float Attack { get; set; } = 12f;
    public float CritRate { get; set; } = 0.05f;
    public float BaseAttackCooldown { get; set; } = 0.35f;

    public bool IsDead => HP <= 0;
    public bool IsDashing { get; private set; }
    public bool FacingLeft { get; private set; }

    // Aim direction (toward mouse)
    public Vector2 AimDirection { get; set; }
    public float AimAngle => MathF.Atan2(AimDirection.Y, AimDirection.X);

    // Bow draw animation
    public float DrawTimer { get; private set; }
    private float _recoilTimer;

    // Hit flash
    private float _hitFlashTimer;

    private readonly Timer _dashTimer = new(0.2f);
    private readonly Timer _dashCooldown = new(0.5f);
    private readonly Timer _attackCooldown = new(0.35f);
    private readonly Timer _invincibleTimer = new(0f);
    private Vector2 _dashDirection;
    private const float DashSpeed = 500f;

    // Knockback
    private Vector2 _knockbackVel;
    public bool IsKnockedBack => _knockbackVel.LengthSquared() > 1f;

    public Player()
    {
        _dashTimer.Reset(0f);
        _dashCooldown.Reset(0f);
        _attackCooldown.Reset(0f);
    }

    public override void Update(GameTime gameTime)
    {
        if (gameTime == null) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _dashTimer.Update(dt);
        _dashCooldown.Update(dt);
        _attackCooldown.Update(dt);
        _invincibleTimer.Update(dt);

        Ki = Math.Min(Ki + KiRegen * dt, MaxKi);

        if (_hitFlashTimer > 0) _hitFlashTimer -= dt;
        if (DrawTimer > 0) DrawTimer -= dt;
        if (_recoilTimer > 0) _recoilTimer -= dt;

        // Knockback decay
        if (_knockbackVel.LengthSquared() > 1f)
        {
            Position += _knockbackVel * dt;
            _knockbackVel *= MathF.Pow(0.01f, dt);
        }

        if (IsDashing)
        {
            if (_dashTimer.IsFinished)
                IsDashing = false;
            else
                Velocity = _dashDirection * DashSpeed;
        }
        else
        {
            var moveDir = InputManager.GetMovementDirection();
            Velocity = moveDir * Speed;

            if (AimDirection.X < 0) FacingLeft = true;
            else if (AimDirection.X > 0) FacingLeft = false;

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Space) && _dashCooldown.IsFinished && moveDir != Vector2.Zero)
            {
                StartDash(moveDir);
            }
        }

        Position += Velocity * dt;
        UpdateBounds(24, 24);
    }

    private void StartDash(Vector2 direction)
    {
        IsDashing = true;
        _dashDirection = direction;
        _dashTimer.Reset(0.2f);
        _dashCooldown.Reset(0.5f);
        _invincibleTimer.Reset(0.2f);
    }

    public bool CanAttack() => _attackCooldown.IsFinished;

    public void OnAttack(float fireRateMultiplier)
    {
        float cd = BaseAttackCooldown / Math.Max(0.3f, fireRateMultiplier);
        _attackCooldown.Reset(cd);
        DrawTimer = 0.08f;
        _recoilTimer = 0.1f;
    }

    public bool IsInvincible => !_invincibleTimer.IsFinished;

    public void TakeDamage(float damage)
    {
        if (IsInvincible) return;
        HP = Math.Max(0, HP - damage);
        _invincibleTimer.Reset(0.5f);
        _hitFlashTimer = 0.12f;
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        _knockbackVel += direction * force;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var color = new Color(220, 210, 190);

        // Hit flash
        if (_hitFlashTimer > 0)
        {
            float t = Math.Min(1f, _hitFlashTimer / 0.06f);
            color = Color.Lerp(color, new Color(255, 80, 80), t);
        }

        // Invincible blinking
        if (IsInvincible)
        {
            float blink = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 30f);
            color *= blink > 0 ? 0.9f : 0.4f;
        }

        // Recoil offset
        float recoilOffset = _recoilTimer > 0 ? -3f : 0f;
        var recoilVec = AimDirection.LengthSquared() > 0 ? Vector2.Normalize(AimDirection) * recoilOffset : Vector2.Zero;
        var drawPos = Position + recoilVec;

        // Body
        var bodyRect = new Rectangle((int)drawPos.X - 10, (int)drawPos.Y - 14, 20, 28);
        DrawRect(spriteBatch, bodyRect, color);

        // Head
        var headRect = new Rectangle((int)drawPos.X - 7, (int)drawPos.Y - 16, 14, 8);
        DrawRect(spriteBatch, headRect, new Color(200, 190, 170) * (color.A / 255f));

        // Bow
        float aimAngle = AimAngle;
        float bowDist = 12f;
        var bowPos = drawPos + new Vector2(MathF.Cos(aimAngle), MathF.Sin(aimAngle)) * bowDist;

        // Bow arc (3 segments)
        var perpendicular = new Vector2(-MathF.Sin(aimAngle), MathF.Cos(aimAngle));
        var bowTop = bowPos + perpendicular * 7f;
        var bowBot = bowPos - perpendicular * 7f;
        var bowColor = new Color(140, 100, 50);
        DrawRect(spriteBatch, new Rectangle((int)bowTop.X - 1, (int)bowTop.Y - 1, 3, 3), bowColor);
        DrawRect(spriteBatch, new Rectangle((int)bowPos.X - 1, (int)bowPos.Y - 1, 2, 2), bowColor);
        DrawRect(spriteBatch, new Rectangle((int)bowBot.X - 1, (int)bowBot.Y - 1, 3, 3), bowColor);

        // String
        DrawRect(spriteBatch, new Rectangle((int)bowTop.X, (int)bowTop.Y, 1, (int)(bowBot.Y - bowTop.Y)), new Color(180, 170, 150) * 0.6f);

        // Arrow nocked (when drawing)
        if (DrawTimer > 0)
        {
            var arrowTip = bowPos + new Vector2(MathF.Cos(aimAngle), MathF.Sin(aimAngle)) * 10f;
            DrawRect(spriteBatch, new Rectangle((int)arrowTip.X - 1, (int)arrowTip.Y - 1, 3, 3), new Color(255, 240, 180));
        }
    }

    private static Texture2D _pixel;
    public static void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }
        spriteBatch.Draw(_pixel, rect, color);
    }
}
