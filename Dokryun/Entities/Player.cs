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
    public float KiRegen { get; set; } = 1f;
    public float Speed { get; set; } = 200f;
    public float Attack { get; set; } = 12f;
    public float CritRate { get; set; } = 0.05f;
    public float BaseAttackCooldown { get; set; } = 0.35f;

    public static float GameTime { get; set; }

    public bool IsDead => HP <= 0;
    public bool IsDashing { get; private set; }
    public bool FacingLeft { get; private set; }

    // Sprite animation
    private SpriteAnimation _runAnim;
    private SpriteAnimation _idleAnim;
    private SpriteAnimation _attackAnim;
    private bool _isMoving;

    // Class
    public bool IsSwordsman { get; set; }

    // Attack lock (swordsman: can't move while attacking)
    public bool IsAttacking { get; private set; }
    private float _attackLockTimer;

    // Aim direction (toward mouse)
    public Vector2 AimDirection { get; set; }
    public float AimAngle => MathF.Atan2(AimDirection.Y, AimDirection.X);

    // Bow draw animation
    public float DrawTimer { get; private set; }
    private float _recoilTimer;

    // Hit flash
    private float _hitFlashTimer;

    // Sword slash effect
    private float _slashTimer;
    private float _slashAngle;
    private const float SlashDuration = 0.2f;
    public bool FlameSlash { get; set; }

    // Combo system (swordsman)
    public int ComboStep { get; private set; } // 0=약, 1=중, 2=강
    private float _comboWindowTimer; // time left to chain next hit
    private const float ComboWindow = 0.5f;

    private readonly Timer _dashTimer = new(0.2f);
    private readonly Timer _attackCooldown = new(0.35f);
    private readonly Timer _invincibleTimer = new(0f);
    private Vector2 _dashDirection;
    private const float DashSpeed = 500f;

    // Dash charges (max 3, recharge 5s each)
    public int DashCharges { get; private set; } = 3;
    public const int MaxDashCharges = 3;
    public const float DashRechargeTime = 5f;
    private float _dashRechargeTimer;

    // Knockback
    private Vector2 _knockbackVel;
    public bool IsKnockedBack => _knockbackVel.LengthSquared() > 1f;

    public Player()
    {
        _dashTimer.Reset(0f);
        _attackCooldown.Reset(0f);
    }

    public void LoadAnimations(Texture2D moveSheet, Texture2D idleSheet = null, Texture2D attackSheet = null)
    {
        // Auto-detect frame size from sheet (5 frames)
        int frameW = moveSheet.Width / 5;
        int frameH = moveSheet.Height;
        _runAnim = new SpriteAnimation(moveSheet, frameW, frameH, 0, 5, 0.1f);

        if (idleSheet != null)
        {
            int idleFrameW = idleSheet.Width / 5;
            int idleFrameH = idleSheet.Height;
            _idleAnim = new SpriteAnimation(idleSheet, idleFrameW, idleFrameH, 0, 5, 0.15f);
        }

        if (attackSheet != null)
        {
            int atkFrameW = attackSheet.Width / 5;
            int atkFrameH = attackSheet.Height;
            _attackAnim = new SpriteAnimation(attackSheet, atkFrameW, atkFrameH, 0, 5, 0.025f) { IsLooping = false };
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (gameTime == null) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _dashTimer.Update(dt);
        _attackCooldown.Update(dt);
        _invincibleTimer.Update(dt);

        // Dash charge recharge
        if (DashCharges < MaxDashCharges)
        {
            _dashRechargeTimer += dt;
            if (_dashRechargeTimer >= DashRechargeTime)
            {
                DashCharges++;
                _dashRechargeTimer -= DashRechargeTime;
            }
        }

        // Attack lock timer
        if (_attackLockTimer > 0)
        {
            _attackLockTimer -= dt;
            if (_attackLockTimer <= 0) IsAttacking = false;
        }

        // Attack animation + slash effect
        if (IsAttacking && _attackAnim != null)
            _attackAnim.Update(dt);
        if (_slashTimer > 0)
            _slashTimer -= dt;

        // Combo window expires → reset
        if (_comboWindowTimer > 0)
        {
            _comboWindowTimer -= dt;
            if (_comboWindowTimer <= 0)
                ComboStep = 0;
        }
        // After 3rd hit (강), force reset after attack ends
        if (ComboStep >= 3 && !IsAttacking)
            ComboStep = 0;

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
        else if (IsAttacking)
        {
            // Can't move while attacking
            Velocity = Vector2.Zero;
        }
        else
        {
            var moveDir = InputManager.GetMovementDirection();
            Velocity = moveDir * Speed;

            if (IsSwordsman)
            {
                // Swordsman: face movement direction
                if (moveDir.X < 0) FacingLeft = true;
                else if (moveDir.X > 0) FacingLeft = false;
            }
            else
            {
                // Archer: face aim (mouse) direction
                if (AimDirection.X < 0) FacingLeft = true;
                else if (AimDirection.X > 0) FacingLeft = false;
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Space) && DashCharges > 0 && moveDir != Vector2.Zero)
            {
                StartDash(moveDir);
            }
        }

        Position += Velocity * dt;
        if (IsSwordsman)
        {
            // Hitbox on lower body portion of sprite (offset down)
            Bounds = new Rectangle(
                (int)(Position.X - 18), (int)(Position.Y + 5),
                36, 38);
        }
        else
            UpdateBounds(24, 24);

        // Animation update
        _isMoving = Velocity.LengthSquared() > 100f;
        if (_isMoving)
        {
            _runAnim?.Update(dt);
            _idleAnim?.Reset();
        }
        else
        {
            _idleAnim?.Update(dt);
            _runAnim?.Reset();
        }
    }

    private void StartDash(Vector2 direction)
    {
        IsDashing = true;
        _dashDirection = direction;
        _dashTimer.Reset(0.2f);
        _invincibleTimer.Reset(0.2f);
        DashCharges--;
        if (DashCharges < MaxDashCharges && _dashRechargeTimer <= 0)
            _dashRechargeTimer = 0f; // start recharging immediately
    }

    public bool CanAttack() => _attackCooldown.IsFinished && !IsAttacking;

    public void OnAttack(float fireRateMultiplier)
    {
        float cd = BaseAttackCooldown / Math.Max(0.3f, fireRateMultiplier);
        _attackCooldown.Reset(cd);
        DrawTimer = 0.08f;
        _recoilTimer = 0.1f;

        if (IsSwordsman)
        {
            IsAttacking = true;

            // Combo: lock duration and slash scale per step
            float lockTime = ComboStep switch { 0 => 0.2f, 1 => 0.25f, _ => 0.35f };
            _attackLockTimer = lockTime;

            // Face attack direction (toward mouse)
            if (AimDirection.X < 0) FacingLeft = true;
            else if (AimDirection.X > 0) FacingLeft = false;

            // Start attack animation + slash effect
            _attackAnim?.Reset();
            _slashTimer = SlashDuration;
            _slashAngle = AimAngle;

            // Advance combo, then open window for next
            ComboStep = Math.Min(ComboStep + 1, 3); // 1,2,3 during attack
            _comboWindowTimer = ComboWindow + lockTime; // window starts after lock ends
        }
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
            float blink = MathF.Sin(GameTime * 30f);
            color *= blink > 0 ? 0.9f : 0.4f;
        }

        // Recoil offset
        float recoilOffset = _recoilTimer > 0 ? -3f : 0f;
        var recoilVec = AimDirection.LengthSquared() > 0 ? Vector2.Normalize(AimDirection) * recoilOffset : Vector2.Zero;
        var drawPos = Position + recoilVec;

        if (_runAnim != null)
        {
            SpriteAnimation anim;
            if (IsAttacking && _attackAnim != null)
                anim = _attackAnim;
            else if (_isMoving || _idleAnim == null)
                anim = _runAnim;
            else
                anim = _idleAnim;

            float spriteScale = 96f / anim.FrameHeight;
            anim.Draw(spriteBatch, drawPos, FacingLeft, color, spriteScale);
        }
        else
        {
            // Fallback: placeholder rectangles with better proportions
            int px = (int)drawPos.X;
            int py = (int)drawPos.Y;

            // Shadow
            DrawRect(spriteBatch, new Rectangle(px - 8, py + 12, 16, 4), new Color(0, 0, 0) * 0.25f);

            // Body
            DrawRect(spriteBatch, new Rectangle(px - 9, py - 10, 18, 24), color);
            // Shoulders
            DrawRect(spriteBatch, new Rectangle(px - 11, py - 8, 22, 6), color * 0.9f);
            // Head
            var headColor = Color.Lerp(color, new Color(220, 210, 195), 0.2f);
            DrawRect(spriteBatch, new Rectangle(px - 6, py - 18, 12, 10), headColor);
            // Hair/hat accent
            DrawRect(spriteBatch, new Rectangle(px - 7, py - 19, 14, 3), new Color(80, 60, 40) * (color.A / 255f));
        }

        // Sword slash - combo-dependent crescent arc
        if (IsSwordsman && _slashTimer > 0)
        {
            float progress = 1f - (_slashTimer / SlashDuration);
            float alpha = progress < 0.3f ? 1f : 1f - (progress - 0.3f) / 0.7f;
            alpha = MathF.Max(0, alpha);

            // Combo visuals: 약(1) → 중(2) → 강(3)
            int combo = Math.Clamp(ComboStep, 1, 3);
            float arcSpan = combo switch { 1 => 2.0f, 2 => 2.5f, _ => 3.2f };
            float outerRadius = combo switch { 1 => 65f, 2 => 75f, _ => 95f };
            float thickness = combo switch { 1 => 20f, 2 => 28f, _ => 38f };
            float sweepSpeed = combo switch { 1 => 3f, 2 => 3f, _ => 2.5f };
            Color slashTint = FlameSlash
                ? combo switch { 1 => new Color(255, 180, 60), 2 => new Color(255, 140, 40), _ => new Color(255, 100, 20) }
                : combo switch { 1 => Color.White, 2 => new Color(220, 230, 255), _ => new Color(255, 240, 180) };

            float sweepProgress = MathF.Min(1f, progress * sweepSpeed);
            // 강(3): sweep direction reversed for variety
            bool reverseSweep = combo == 2;

            int segments = 32;
            for (int i = 0; i <= (int)(segments * sweepProgress); i++)
            {
                float t = (float)i / segments;
                float angle;
                if (reverseSweep)
                    angle = _slashAngle + arcSpan / 2f - arcSpan * t;
                else
                    angle = _slashAngle - arcSpan / 2f + arcSpan * t;

                float crescentT = MathF.Sin(t * MathF.PI);
                float currentThickness = thickness * crescentT;
                if (currentThickness < 2f) currentThickness = 2f;

                float outerR = outerRadius;
                float innerR = outerRadius - currentThickness;

                int layers = (int)(currentThickness / 2f) + 1;
                for (int layer = 0; layer < layers; layer++)
                {
                    float lt = (float)layer / Math.Max(1, layers - 1);
                    float r = innerR + (outerR - innerR) * lt;
                    float px = drawPos.X + MathF.Cos(angle) * r;
                    float py = drawPos.Y + MathF.Sin(angle) * r;

                    float size = 4f + 2f * crescentT;
                    if (combo == 3) size += 2f; // extra thick for 강
                    DrawRect(spriteBatch, new Rectangle((int)(px - size / 2), (int)(py - size / 2), (int)size + 1, (int)size + 1), slashTint * alpha);
                }

                // Outer glow
                float gx = drawPos.X + MathF.Cos(angle) * (outerR + 3f);
                float gy = drawPos.Y + MathF.Sin(angle) * (outerR + 3f);
                float glowSize = (combo == 3 ? 5f : 3f) * crescentT * alpha;
                if (FlameSlash) glowSize *= 1.8f;
                if (glowSize > 0.5f)
                {
                    var glowColor = FlameSlash
                        ? new Color(255, 120 + (int)(crescentT * 100), 20)
                        : combo == 3 ? new Color(255, 220, 120) : new Color(200, 220, 255);
                    DrawRect(spriteBatch, new Rectangle((int)(gx - glowSize / 2), (int)(gy - glowSize / 2), (int)glowSize + 1, (int)glowSize + 1), glowColor * (alpha * 0.7f));
                }

                // Flame flicker particles along the arc
                if (FlameSlash && crescentT > 0.3f && i % 3 == 0)
                {
                    float flameR = outerR + 5f + (float)Math.Sin(Player.GameTime * 20f + angle * 5f) * 4f;
                    float fx = drawPos.X + MathF.Cos(angle) * flameR;
                    float fy = drawPos.Y + MathF.Sin(angle) * flameR;
                    float flameSize = (3f + crescentT * 5f) * alpha;
                    float flicker = 0.6f + 0.4f * MathF.Sin(Player.GameTime * 30f + i * 2f);
                    var flameColor = Color.Lerp(new Color(255, 60, 10), new Color(255, 220, 50), MathF.Sin(Player.GameTime * 15f + angle * 3f) * 0.5f + 0.5f);
                    DrawRect(spriteBatch, new Rectangle((int)(fx - flameSize / 2), (int)(fy - flameSize / 2), (int)flameSize + 1, (int)flameSize + 1), flameColor * (alpha * flicker * 0.8f));
                }
            }
        }

        if (!IsSwordsman)
        {
            // Bow (archer only)
            float aimAngle = AimAngle;
            float bowDist = 12f;
            var bowPos = drawPos + new Vector2(MathF.Cos(aimAngle), MathF.Sin(aimAngle)) * bowDist;

            var perpendicular = new Vector2(-MathF.Sin(aimAngle), MathF.Cos(aimAngle));
            var bowTop = bowPos + perpendicular * 7f;
            var bowBot = bowPos - perpendicular * 7f;
            var bowColor = new Color(140, 100, 50);
            DrawRect(spriteBatch, new Rectangle((int)bowTop.X - 1, (int)bowTop.Y - 1, 3, 3), bowColor);
            DrawRect(spriteBatch, new Rectangle((int)bowPos.X - 1, (int)bowPos.Y - 1, 2, 2), bowColor);
            DrawRect(spriteBatch, new Rectangle((int)bowBot.X - 1, (int)bowBot.Y - 1, 3, 3), bowColor);

            DrawRect(spriteBatch, new Rectangle((int)bowTop.X, (int)bowTop.Y, 1, (int)(bowBot.Y - bowTop.Y)), new Color(180, 170, 150) * 0.6f);

            if (DrawTimer > 0)
            {
                var arrowTip = bowPos + new Vector2(MathF.Cos(aimAngle), MathF.Sin(aimAngle)) * 10f;
                DrawRect(spriteBatch, new Rectangle((int)arrowTip.X - 1, (int)arrowTip.Y - 1, 3, 3), new Color(255, 240, 180));
            }
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
