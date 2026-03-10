using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Entities;

public class Enemy : Entity
{
    public float MaxHP { get; set; }
    public float HP { get; set; }
    public float Speed { get; set; }
    public float BaseSpeed { get; set; }
    public float Attack { get; set; }
    public float AttackRange { get; set; } = 30f;
    public EnemyType Type { get; set; }

    public bool IsDead => HP <= 0;
    public bool IsBoss => Type == EnemyType.DokkaebiKing;

    // Aggro system
    public bool IsAggro { get; set; }
    public float AggroRange { get; set; } = 350f;

    // Hit flash
    public float HitFlashTimer { get; private set; }

    // Knockback
    private Vector2 _knockbackVel;

    // Death animation
    public float DeathTimer { get; private set; }
    public bool IsDeathAnimating { get; private set; }

    // Squash & stretch
    private float _squashTimer;
    private float _squashScaleX = 1f;
    private float _squashScaleY = 1f;

    // Attack telegraph
    public float TelegraphTimer { get; set; }
    public float TelegraphDuration { get; set; }
    public bool IsTelegraphing => TelegraphTimer > 0;
    public Vector2 TelegraphDirection { get; set; }

    // Slow effect (from frost arrows)
    public float SlowMultiplier { get; set; } = 1f;
    private float _slowTimer;

    private float _attackTimer;
    private float _attackCooldown = 1f;

    // Cached bounds dimensions
    private int _boundsWidth;
    private int _boundsHeight;

    // Boss phase & attack state machine
    public int BossPhase { get; set; } = 0;
    public float BossSummonTimer { get; set; }
    public float BossSpecialTimer { get; set; }
    public BossAttackState BossState { get; set; } = BossAttackState.Idle;
    public float BossStateTimer { get; set; }
    public float BossAttackCooldown { get; set; }
    public int BossAttackIndex { get; set; } = -1;
    public Vector2 BossChargeDir { get; set; }
    public Vector2 BossAirOrigin { get; set; }
    public int BossPouchCount { get; set; }
    public float BossSpinAngle { get; set; }
    public float BossStompCount { get; set; }
    public bool BossUlt60Used { get; set; }
    public bool BossUlt30Used { get; set; }
    public int BossUltSubCount { get; set; }
    public Vector2 BossUltTarget { get; set; }

    public Enemy(EnemyType type, Vector2 position)
    {
        Type = type;
        Position = position;
        ApplyTypeStats();
    }

    private void ApplyTypeStats()
    {
        switch (Type)
        {
            // Tier 1
            case EnemyType.Soldier:
                MaxHP = 32; Speed = 80; Attack = 5; _attackCooldown = 1.2f; AttackRange = 30f;
                break;
            case EnemyType.Archer:
                MaxHP = 19; Speed = 60; Attack = 8; AttackRange = 200f; _attackCooldown = 2.5f;
                break;
            // Tier 2
            case EnemyType.Warrior:
                MaxHP = 64; Speed = 120; Attack = 12; _attackCooldown = 1.5f; AttackRange = 35f;
                break;
            case EnemyType.GhostFire:
                MaxHP = 13; Speed = 100; Attack = 6; _attackCooldown = 0f;
                break;
            case EnemyType.Spearman:
                MaxHP = 45; Speed = 70; Attack = 9; _attackCooldown = 1.8f; AttackRange = 45f;
                break;
            case EnemyType.ShieldBearer:
                MaxHP = 104; Speed = 45; Attack = 6; _attackCooldown = 2.0f; AttackRange = 25f;
                break;
            // Tier 3
            case EnemyType.Assassin:
                MaxHP = 24; Speed = 160; Attack = 14; _attackCooldown = 1.0f; AttackRange = 28f;
                break;
            case EnemyType.Shaman:
                MaxHP = 29; Speed = 50; Attack = 7; AttackRange = 180f; _attackCooldown = 3.0f;
                break;
            case EnemyType.FireArcher:
                MaxHP = 26; Speed = 65; Attack = 12; AttackRange = 220f; _attackCooldown = 2.0f;
                break;
            case EnemyType.PoisonThrower:
                MaxHP = 32; Speed = 55; Attack = 5; AttackRange = 150f; _attackCooldown = 2.5f;
                break;
            // Tier 4
            case EnemyType.DarkKnight:
                MaxHP = 128; Speed = 90; Attack = 18; _attackCooldown = 1.3f; AttackRange = 35f;
                break;
            case EnemyType.Summoner:
                MaxHP = 40; Speed = 40; Attack = 5; AttackRange = 250f; _attackCooldown = 4.0f;
                break;
            case EnemyType.BladeDancer:
                MaxHP = 72; Speed = 130; Attack = 15; _attackCooldown = 1.2f; AttackRange = 30f;
                break;
            case EnemyType.ThunderMonk:
                MaxHP = 88; Speed = 60; Attack = 20; AttackRange = 120f; _attackCooldown = 3.5f;
                break;
            // Boss
            case EnemyType.DokkaebiKing:
                MaxHP = 1200; Speed = 60; Attack = 20; _attackCooldown = 1.8f; AttackRange = 55f;
                AggroRange = 600f; IsAggro = true;
                BossSummonTimer = 8f;
                BossSpecialTimer = 12f;
                break;
        }
        BaseSpeed = Speed;
        HP = MaxHP;
        CacheBoundsDimensions();
    }

    private void CacheBoundsDimensions()
    {
        _boundsWidth = IsBoss ? 48 : 20;
        _boundsHeight = IsBoss ? 48 : 20;
        if (Type == EnemyType.Warrior || Type == EnemyType.DarkKnight || Type == EnemyType.BladeDancer) { _boundsWidth = 24; _boundsHeight = 24; }
        if (Type == EnemyType.ShieldBearer) { _boundsWidth = 26; _boundsHeight = 26; }
        if (Type == EnemyType.GhostFire || Type == EnemyType.Assassin) { _boundsWidth = 16; _boundsHeight = 16; }
        if (Type == EnemyType.ThunderMonk) { _boundsWidth = 22; _boundsHeight = 22; }
    }

    public override void Update(GameTime gameTime)
    {
        if (gameTime == null) { UpdateBounds(_boundsWidth, _boundsHeight); return; }
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _attackTimer -= dt;

        if (HitFlashTimer > 0) HitFlashTimer -= dt;
        if (TelegraphTimer > 0) TelegraphTimer -= dt;

        if (_slowTimer > 0)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0) SlowMultiplier = 1f;
        }

        // Knockback (boss has reduced knockback)
        if (_knockbackVel.LengthSquared() > 1f)
        {
            float kbMult = IsBoss ? 0.2f : 1f;
            Position += _knockbackVel * kbMult * dt;
            _knockbackVel *= MathF.Pow(0.01f, dt);
        }

        if (_squashTimer > 0)
        {
            _squashTimer -= dt;
            float t = _squashTimer / 0.15f;
            _squashScaleX = MathHelper.Lerp(1f, 1.4f, t);
            _squashScaleY = MathHelper.Lerp(1f, 0.7f, t);
        }
        else
        {
            _squashScaleX = 1f;
            _squashScaleY = 1f;
        }

        if (IsDeathAnimating)
        {
            DeathTimer -= dt;
            return;
        }

        // Boss timers
        if (IsBoss)
        {
            BossSummonTimer -= dt;
            BossSpecialTimer -= dt;
            BossAttackCooldown -= dt;
            BossStateTimer -= dt;
            // Phase transitions
            float hpRatio = MaxHP > 0 ? HP / MaxHP : 1f;
            if (hpRatio < 0.3f && BossPhase < 2)
            {
                BossPhase = 2;
                if (!BossUlt30Used)
                {
                    BossUlt30Used = true;
                    BossState = BossAttackState.UltBerserk;
                    BossStateTimer = 0.8f;
                    BossUltSubCount = 0;
                }
            }
            else if (hpRatio < 0.6f && BossPhase < 1)
            {
                BossPhase = 1;
                if (!BossUlt60Used)
                {
                    BossUlt60Used = true;
                    BossState = BossAttackState.UltFireRain;
                    BossStateTimer = 0.6f;
                    BossUltSubCount = 0;
                }
            }
        }

        UpdateBounds(_boundsWidth, _boundsHeight);
    }

    public void MoveToward(Vector2 target, float deltaTime)
    {
        if (IsTelegraphing) return;
        var dir = target - Position;
        if (dir.LengthSquared() > 1f)
        {
            dir.Normalize();
            Position += dir * Speed * SlowMultiplier * deltaTime;
        }
    }

    public bool CanAttack() => _attackTimer <= 0 && !IsTelegraphing;

    public void OnAttack()
    {
        _attackTimer = _attackCooldown;
    }

    public void StartTelegraph(Vector2 direction, float duration)
    {
        TelegraphTimer = duration;
        TelegraphDuration = duration;
        TelegraphDirection = direction;
        if (direction.LengthSquared() > 0) TelegraphDirection = Vector2.Normalize(direction);
    }

    public void TakeDamage(float damage)
    {
        HP = Math.Max(0, HP - damage);
        HitFlashTimer = 0.1f;
        _squashTimer = 0.15f;
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        _knockbackVel += direction * force;
    }

    public void ApplySlow(float slowAmount, float duration)
    {
        SlowMultiplier = Math.Max(0.2f, 1f - slowAmount);
        _slowTimer = duration;
    }

    public void StartDeathAnimation()
    {
        IsDeathAnimating = true;
        DeathTimer = IsBoss ? 0.6f : 0.25f;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Color color;

        if (HitFlashTimer > 0)
        {
            float flashIntensity = Math.Min(1f, HitFlashTimer / 0.05f);
            var baseColor = GetTypeColor();
            color = Color.Lerp(baseColor, Color.White, flashIntensity);
        }
        else
        {
            color = GetTypeColor();
        }

        if (SlowMultiplier < 0.9f)
            color = Color.Lerp(color, new Color(100, 180, 255), 0.3f);

        float hpRatio = MaxHP > 0 ? HP / MaxHP : 1f;
        if (hpRatio < 0.5f && !IsDead)
        {
            float flicker = MathF.Sin(Player.GameTime * 15f) * 0.15f;
            color = color * (0.7f + flicker);
        }

        float scale = 1f;
        if (IsDeathAnimating)
        {
            float deathDur = IsBoss ? 0.6f : 0.25f;
            float t = DeathTimer / deathDur;
            scale = 1f + (1f - t) * (IsBoss ? 1.5f : 0.3f);
            color *= t * t; // Quadratic fade for smoother dissolve
        }

        if (IsBoss)
            DrawBoss(spriteBatch, color, scale);
        else
            DrawNormal(spriteBatch, color, scale);
    }

    private (int w, int h) GetBaseSize() => Type switch
    {
        EnemyType.Warrior or EnemyType.DarkKnight or EnemyType.BladeDancer => (24, 24),
        EnemyType.ShieldBearer => (26, 26),
        EnemyType.GhostFire or EnemyType.Assassin => (16, 16),
        EnemyType.ThunderMonk => (22, 22),
        _ => (20, 20)
    };

    private void DrawNormal(SpriteBatch spriteBatch, Color color, float scale)
    {
        var (baseW, baseH) = GetBaseSize();
        int w = (int)(baseW * scale * _squashScaleX);
        int h = (int)(baseH * scale * _squashScaleY);
        int px = (int)Position.X;
        int py = (int)Position.Y;
        var rect = new Rectangle(px - w / 2, py - h / 2, w, h);
        Player.DrawRect(spriteBatch, rect, color);

        if (!IsDeathAnimating)
        {
            switch (Type)
            {
                case EnemyType.Archer:
                    Player.DrawRect(spriteBatch, new Rectangle(px + 8, py - 6, 2, 12), new Color(120, 80, 40));
                    break;
                case EnemyType.Warrior:
                    Player.DrawRect(spriteBatch, new Rectangle(px - 14, py - 8, 4, 16), new Color(150, 150, 160));
                    break;
                case EnemyType.Spearman:
                    // Long spear
                    Player.DrawRect(spriteBatch, new Rectangle(px + 8, py - 12, 2, 24), new Color(140, 120, 80));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 7, py - 14, 4, 4), new Color(180, 180, 190));
                    break;
                case EnemyType.ShieldBearer:
                    // Shield (left side)
                    Player.DrawRect(spriteBatch, new Rectangle(px - 15, py - 8, 5, 16), new Color(100, 90, 70));
                    Player.DrawRect(spriteBatch, new Rectangle(px - 14, py - 6, 3, 12), new Color(130, 120, 90));
                    break;
                case EnemyType.Assassin:
                    // Dual daggers
                    Player.DrawRect(spriteBatch, new Rectangle(px - 10, py - 2, 6, 2), new Color(200, 200, 210));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 4, py - 2, 6, 2), new Color(200, 200, 210));
                    break;
                case EnemyType.Shaman:
                    // Staff with orb
                    Player.DrawRect(spriteBatch, new Rectangle(px + 8, py - 10, 2, 20), new Color(100, 70, 40));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 6, py - 13, 6, 5), new Color(120, 200, 180));
                    break;
                case EnemyType.FireArcher:
                    // Bow + flame tip
                    Player.DrawRect(spriteBatch, new Rectangle(px + 8, py - 6, 2, 12), new Color(120, 80, 40));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 10, py - 8, 3, 3), new Color(255, 120, 30));
                    break;
                case EnemyType.PoisonThrower:
                    // Vial
                    Player.DrawRect(spriteBatch, new Rectangle(px + 6, py - 4, 5, 6), new Color(80, 200, 60));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 7, py - 6, 3, 3), new Color(60, 150, 40));
                    break;
                case EnemyType.DarkKnight:
                    // Heavy sword
                    Player.DrawRect(spriteBatch, new Rectangle(px - 16, py - 10, 5, 20), new Color(60, 50, 70));
                    Player.DrawRect(spriteBatch, new Rectangle(px - 15, py - 12, 3, 4), new Color(100, 80, 110));
                    // Helmet
                    Player.DrawRect(spriteBatch, new Rectangle(px - 5, py - 14, 10, 4), new Color(50, 45, 55));
                    break;
                case EnemyType.Summoner:
                    // Floating tome
                    float bob = MathF.Sin(Player.GameTime * 4f) * 2f;
                    Player.DrawRect(spriteBatch, new Rectangle(px + 8, (int)(py - 8 + bob), 6, 8), new Color(80, 50, 120));
                    Player.DrawRect(spriteBatch, new Rectangle(px + 9, (int)(py - 6 + bob), 4, 4), new Color(200, 150, 255));
                    break;
                case EnemyType.BladeDancer:
                    // Spinning blades indicator
                    float spin = Player.GameTime * 6f;
                    int bx1 = px + (int)(MathF.Cos(spin) * 12);
                    int by1 = py + (int)(MathF.Sin(spin) * 12);
                    int bx2 = px + (int)(MathF.Cos(spin + MathF.PI) * 12);
                    int by2 = py + (int)(MathF.Sin(spin + MathF.PI) * 12);
                    Player.DrawRect(spriteBatch, new Rectangle(bx1 - 2, by1 - 1, 4, 2), new Color(200, 200, 220));
                    Player.DrawRect(spriteBatch, new Rectangle(bx2 - 2, by2 - 1, 4, 2), new Color(200, 200, 220));
                    break;
                case EnemyType.ThunderMonk:
                    // Lightning aura
                    float pulse = MathF.Sin(Player.GameTime * 8f) * 0.2f + 0.3f;
                    Player.DrawRect(spriteBatch, new Rectangle(px - w / 2 - 3, py - h / 2 - 3, w + 6, h + 6),
                        new Color(130, 130, 255) * pulse);
                    // Prayer beads
                    Player.DrawRect(spriteBatch, new Rectangle(px - 3, py - 14, 6, 3), new Color(200, 180, 80));
                    break;
            }
        }

        float hpRatio = MaxHP > 0 ? HP / MaxHP : 1f;
        if (!IsDead && !IsDeathAnimating && hpRatio < 1f)
        {
            int barW = baseW + 6;
            int barH = 3;
            int barX = px - barW / 2;
            int barY = py - baseH / 2 - 7;
            // Background
            Player.DrawRect(spriteBatch, new Rectangle(barX - 1, barY - 1, barW + 2, barH + 2), new Color(0, 0, 0) * 0.5f);
            Player.DrawRect(spriteBatch, new Rectangle(barX, barY, barW, barH), new Color(30, 8, 8));
            // HP fill with color based on ratio
            var hpBarColor = hpRatio > 0.5f ? new Color(200, 50, 40) : new Color(255, 60, 40);
            int fillW = (int)(barW * hpRatio);
            Player.DrawRect(spriteBatch, new Rectangle(barX, barY, fillW, barH), hpBarColor);
            // Top highlight
            Player.DrawRect(spriteBatch, new Rectangle(barX, barY, fillW, 1), new Color(255, 120, 100) * 0.4f);
        }
    }

    private void DrawBoss(SpriteBatch spriteBatch, Color color, float scale)
    {
        int px = (int)Position.X;
        int py = (int)Position.Y;
        int baseW = (int)(44 * scale * _squashScaleX);
        int baseH = (int)(48 * scale * _squashScaleY);

        if (IsDeathAnimating)
        {
            // Boss death: flash and expand
            Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2, py - baseH / 2, baseW, baseH), color);
            return;
        }

        float time = Player.GameTime;

        // Body (large, greenish)
        Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2, py - baseH / 2 + 6, baseW, baseH - 6), color);

        // Head (slightly wider)
        int headW = (int)(baseW * 0.8f);
        int headH = (int)(baseH * 0.35f);
        Player.DrawRect(spriteBatch, new Rectangle(px - headW / 2, py - baseH / 2 - 2, headW, headH), color);

        // Horns (two triangular shapes)
        var hornColor = new Color(180, 160, 80);
        // Left horn
        Player.DrawRect(spriteBatch, new Rectangle(px - headW / 2 + 3, py - baseH / 2 - 12, 4, 10), hornColor);
        Player.DrawRect(spriteBatch, new Rectangle(px - headW / 2 + 4, py - baseH / 2 - 16, 2, 5), hornColor);
        // Right horn
        Player.DrawRect(spriteBatch, new Rectangle(px + headW / 2 - 7, py - baseH / 2 - 12, 4, 10), hornColor);
        Player.DrawRect(spriteBatch, new Rectangle(px + headW / 2 - 6, py - baseH / 2 - 16, 2, 5), hornColor);

        // Eyes (glowing red, pulse when low HP)
        float eyePulse = BossPhase >= 1 ? (MathF.Sin(time * 8f) * 0.3f + 0.7f) : 1f;
        var eyeColor = new Color((int)(255 * eyePulse), (int)(40 * eyePulse), (int)(20 * eyePulse));
        Player.DrawRect(spriteBatch, new Rectangle(px - 8, py - baseH / 2 + 4, 4, 3), eyeColor);
        Player.DrawRect(spriteBatch, new Rectangle(px + 4, py - baseH / 2 + 4, 4, 3), eyeColor);

        // Mouth (wide grin)
        var mouthColor = new Color(40, 20, 15);
        Player.DrawRect(spriteBatch, new Rectangle(px - 7, py - baseH / 2 + 10, 14, 3), mouthColor);
        // Fangs
        Player.DrawRect(spriteBatch, new Rectangle(px - 6, py - baseH / 2 + 13, 2, 3), Color.White * 0.8f);
        Player.DrawRect(spriteBatch, new Rectangle(px + 4, py - baseH / 2 + 13, 2, 3), Color.White * 0.8f);

        // Club (방망이) - held to the right
        int clubX = px + baseW / 2 + 2;
        int clubY = py - 8;
        var clubColor = new Color(130, 90, 40);
        // Handle
        Player.DrawRect(spriteBatch, new Rectangle(clubX, clubY, 4, 22), clubColor);
        // Head of club
        var clubHeadColor = new Color(160, 110, 50);
        Player.DrawRect(spriteBatch, new Rectangle(clubX - 3, clubY - 4, 10, 10), clubHeadColor);
        // Studs on club
        Player.DrawRect(spriteBatch, new Rectangle(clubX - 1, clubY - 2, 2, 2), new Color(200, 180, 80));
        Player.DrawRect(spriteBatch, new Rectangle(clubX + 3, clubY, 2, 2), new Color(200, 180, 80));

        // Belt/loincloth
        var beltColor = new Color(120, 80, 30);
        Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2 + 2, py + 4, baseW - 4, 4), beltColor);

        // Phase 1+: Aura glow
        if (BossPhase >= 1)
        {
            float aura = MathF.Sin(time * 4f) * 0.15f + 0.2f;
            if (BossPhase >= 2)
            {
                // Phase 2: intense red pulsing aura with multiple layers
                var auraColor = new Color(255, 60, 30) * aura;
                Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2 - 8, py - baseH / 2 - 8, baseW + 16, baseH + 16), auraColor * 0.5f);
                Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2 - 4, py - baseH / 2 - 4, baseW + 8, baseH + 8), auraColor);

                // Flame-like particles at edges
                for (int fi = 0; fi < 4; fi++)
                {
                    float fAngle = time * 3f + fi * MathHelper.PiOver2;
                    int fx = px + (int)(MathF.Cos(fAngle) * (baseW / 2 + 6));
                    int fy = py + (int)(MathF.Sin(fAngle) * (baseH / 2 + 6));
                    Player.DrawRect(spriteBatch, new Rectangle(fx - 3, fy - 3, 6, 6), new Color(255, 100, 30) * (aura + 0.2f));
                }

                // Cracked ground indicator
                float crackPulse = MathF.Sin(time * 8f) * 0.1f + 0.15f;
                Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2 - 12, py + baseH / 2, baseW + 24, 3), new Color(255, 80, 20) * crackPulse);
            }
            else
            {
                var auraColor = new Color(100, 255, 100) * aura;
                Player.DrawRect(spriteBatch, new Rectangle(px - baseW / 2 - 4, py - baseH / 2 - 4, baseW + 8, baseH + 8), auraColor);
            }
        }
    }

    private Color GetTypeColor()
    {
        return Type switch
        {
            // Tier 1
            EnemyType.Soldier => new Color(180, 60, 50),
            EnemyType.Archer => new Color(220, 160, 60),
            // Tier 2
            EnemyType.Warrior => new Color(160, 30, 30),
            EnemyType.GhostFire => new Color(60, 200, 220),
            EnemyType.Spearman => new Color(140, 100, 60),
            EnemyType.ShieldBearer => new Color(120, 110, 80),
            // Tier 3
            EnemyType.Assassin => new Color(80, 60, 80),
            EnemyType.Shaman => new Color(80, 160, 140),
            EnemyType.FireArcher => new Color(220, 100, 40),
            EnemyType.PoisonThrower => new Color(80, 180, 60),
            // Tier 4
            EnemyType.DarkKnight => new Color(50, 40, 65),
            EnemyType.Summoner => new Color(120, 60, 160),
            EnemyType.BladeDancer => new Color(180, 50, 80),
            EnemyType.ThunderMonk => new Color(80, 80, 180),
            // Boss
            EnemyType.DokkaebiKing => new Color(50, 140, 70),
            _ => Color.Red
        };
    }
}

public enum EnemyType
{
    // Tier 1 (Floor 1-2)
    Soldier,        // 보졸 - 기본 근접
    Archer,         // 궁수 - 원거리
    // Tier 2 (Floor 3-4)
    Warrior,        // 무사 - 강한 근접
    GhostFire,      // 귀화 - 자폭 돌격
    Spearman,       // 창병 - 긴 사거리 근접
    ShieldBearer,   // 방패병 - 높은 체력, 느린 이동
    // Tier 3 (Floor 5-6)
    Assassin,       // 자객 - 빠른 이동, 낮은 체력
    Shaman,         // 무당 - 원거리 + 아군 버프 (느린 원거리)
    FireArcher,     // 화궁 - 불화살 (빠른 원거리)
    PoisonThrower,  // 독술사 - 독 투척
    // Tier 4 (Floor 7+)
    DarkKnight,     // 흑기사 - 높은 스탯 근접
    Summoner,       // 소환사 - 소환형 (특수 처리 필요)
    BladeDancer,    // 검무사 - 회전 공격
    ThunderMonk,    // 뇌승 - 번개 범위 공격
    // Boss
    DokkaebiKing
}

public enum BossAttackState
{
    Idle,
    PouchJump,       // 공중으로 점프 → 도깨비 주머니 투척
    PouchThrowing,   // 주머니 던지는 중
    PouchLanding,    // 착지
    ClubCharge,      // 방망이 휘두르며 돌진
    ClubCharging,    // 돌진 중
    SpinAttack,      // 회전 공격
    Spinning,        // 회전 중
    Stomp,           // 연속 지진 밟기
    Stomping,        // 밟는 중
    RoarPull,        // 포효 → 흡인
    Roaring,         // 포효 중
    ShadowClone,     // 분신 생성
    // === 궁극기 (Ultimate) ===
    UltFireRain,     // 궁극기: 불비 - 맵 전체에 불기둥 낙하
    UltFireRaining,  // 불비 진행 중
    UltMeteor,       // 궁극기: 거대 운석 - 중앙으로 거대 충격파
    UltMeteorFall,   // 운석 낙하 중
    UltBerserk,      // 궁극기: 광폭화 - 연속 돌진 + 회전 콤보
    UltBerserking,   // 광폭화 진행
    UltDarkWave,     // 궁극기: 암흑파 - 전방위 파동 연속
    UltDarkWaving,   // 암흑파 진행
}
