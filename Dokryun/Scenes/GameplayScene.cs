using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Entities;

namespace Dokryun.Scenes;

public struct DamagePopup
{
    public Vector2 Position;
    public int Damage;
    public float Life;
    public float MaxLife;
    public bool IsCrit;
}

public struct HitParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public Color Tint;
    public int Size;
}

public struct SwordProjectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public int TargetIndex;
    public bool Alive;
    public float Life;
    public float Age;          // total time alive, for spiral
    public float SpiralOffset; // unique spiral phase per sword
}

public class GameplayScene : Scene
{
    private Texture2D _background;
    private Texture2D _playerIdle;
    private Texture2D _enemyIdle;
    private Texture2D _groundTex;
    private Texture2D _swardTex;
    private Texture2D _attackTex;
    private Texture2D _pixel;
    private SpriteFont _font;

    // Platforms
    private Rectangle[] _platDraw;
    private Rectangle[] _platCollide;
    private readonly Rectangle _groundSrc = new(130, 155, 1020, 425);

    // Sprite animation
    private const int FrameCount = 5;
    private const int FrameWidth = 560;
    private const int FrameHeight = 560;
    private const float FrameDuration = 0.15f;
    private int _currentFrame;
    private float _frameTimer;
    private const float PlayerScale = 0.2f;

    private const int PX = 2;

    // === World ===
    private const int WorldWidth = 6400;
    private const int GroundY = 600;
    private const float Gravity = 1400f;
    private const float JumpForce = -520f;
    private const float MoveSpeed = 280f;
    private const float DashSpeed = 700f;
    private const float DashDuration = 0.15f;

    // === Player ===
    private Character _player;
    private Vector2 _playerPos;
    private Vector2 _playerVel;
    private bool _onGround;
    private bool _facingRight = true;
    private float _playerFlash;
    private int _jumpCount;
    private const int MaxJumps = 2;

    // Dash
    private float _dashTimer;
    private bool _isDashing;
    private float _dashDir;

    // Double-tap detection
    private float _lastLeftTap;
    private float _lastRightTap;
    private const float DoubleTapWindow = 0.25f;

    // Melee Attack (Ctrl)
    private bool _isAttacking;
    private float _attackTimer;
    private const float AttackDuration = 0.25f;
    private const float AttackRange = 100f;
    private bool _attackHit;
    private float _slashLife;

    // Sword projectile (A key)
    private List<SwordProjectile> _swords = new();
    private const float SwordSpeed = 500f;
    private const float SwordHomingStrength = 8f;
    private const float SwordDrawScale = 0.09f; // 960*0.09 = ~86px (1.5x bigger)
    private float _swordCooldown;
    private const float SwordCooldownTime = 0.4f;

    // Attack projectile (S key) - attack.png
    private List<SwordProjectile> _attackProjs = new(); // reuse struct
    private const float AttackProjSpeed = 600f;
    private const float AttackProjScale = 0.08f; // 1024*0.08 = ~82px
    private float _attackProjCooldown;
    private const float AttackProjCooldownTime = 0.5f;

    // === Enemies ===
    private List<Enemy> _enemies;
    private Vector2[] _enemyPositions;
    private float[] _enemyFlash;
    private bool[] _enemyFacingRight;

    // === Camera ===
    private Vector2 _camera;

    // === Input ===
    private KeyboardState _prevKeys;

    // === VFX ===
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private Random _rng = new();
    private List<DamagePopup> _popups = new();
    private List<HitParticle> _particles = new();

    protected override void LoadContent()
    {
        _background = Content.Load<Texture2D>("Sprites/stage1_background");
        _playerIdle = Content.Load<Texture2D>("Sprites/player_1_idle");
        _enemyIdle = Content.Load<Texture2D>("Sprites/enemy_1_idle");
        _swardTex = Content.Load<Texture2D>("Sprites/sward");
        _attackTex = Content.Load<Texture2D>("Sprites/attack");
        _font = Content.Load<SpriteFont>("Fonts/GameFont");

        _groundTex = Content.Load<Texture2D>("Sprites/ground");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // === Platforms: varying sizes ===
        // (x, y, width, height)
        var plats = new (int x, int y, int w, int h)[] {
            // Zone 1: intro - wide easy platforms
            (180, 490, 400, 100),
            (500, 430, 280, 80),
            // Zone 2: staircase
            (820, 470, 240, 70),
            (1000, 410, 320, 90),
            (1250, 350, 200, 60),
            // Zone 3: high + low mix
            (1450, 300, 360, 100),
            (1700, 320, 180, 55),
            (1550, 460, 260, 75),
            // Zone 4: gaps
            (2000, 400, 200, 60),
            (2300, 370, 280, 80),
            (2150, 500, 340, 95),
            // Zone 5: dense
            (2600, 430, 220, 65),
            (2780, 360, 300, 85),
            (2680, 510, 180, 55),
            (3000, 390, 260, 75),
            // Zone 6: descent
            (3300, 460, 320, 90),
            (3550, 400, 200, 60),
            (3380, 520, 280, 80),
            // Zone 7: final stretch
            (3900, 410, 240, 70),
            (4150, 350, 360, 100),
            (4050, 500, 200, 60),
            (4500, 380, 280, 80),
            // Zone 8: boss area
            (4800, 460, 340, 95),
            (5100, 410, 240, 70),
            (5350, 360, 300, 85),
            (5650, 430, 220, 65),
            (5950, 490, 400, 100),
        };

        _platDraw = new Rectangle[plats.Length];
        _platCollide = new Rectangle[plats.Length];
        for (int i = 0; i < plats.Length; i++)
        {
            var p = plats[i];
            _platDraw[i] = new Rectangle(p.x, p.y, p.w, p.h);
            // Collision proportional to draw size: inset ~20% each side, top 40% down
            int insetX = (int)(p.w * 0.20f);
            _platCollide[i] = new Rectangle(p.x + insetX, p.y + (int)(p.h * 0.40f), p.w - insetX * 2, 12);
        }

        _player = new Character { Name = "검사", Hp = 100, MaxHp = 100, Atk = 18, Def = 4 };
        _playerPos = new Vector2(200, GroundY);
        _playerVel = Vector2.Zero;
        _onGround = true;

        // === Enemies: all same stats ===
        _enemies = new List<Enemy>();
        var enemyPosList = new List<Vector2>();

        // Ground enemies
        Vector2[] groundEnemies = {
            new(500, GroundY), new(900, GroundY), new(1350, GroundY),
            new(1800, GroundY), new(2400, GroundY), new(2900, GroundY),
            new(3500, GroundY), new(4100, GroundY), new(4700, GroundY),
            new(5300, GroundY), new(5800, GroundY),
        };

        // Platform enemies: placed at platform collision Y (feet on platform)
        // Each entry: (platform index, x offset within platform)
        var platEnemyDefs = new (int platIdx, float xRatio)[] {
            (1, 0.5f), (3, 0.5f), (4, 0.5f),
            (6, 0.5f), (9, 0.5f),
            (12, 0.5f), (14, 0.5f),
            (16, 0.5f), (19, 0.5f),
            (23, 0.5f), (25, 0.5f),
        };

        int idx = 0;
        foreach (var pos in groundEnemies)
        {
            _enemies.Add(new Enemy($"도깨비{idx}", 40, 8, 2, idx));
            enemyPosList.Add(pos);
            idx++;
        }
        foreach (var def in platEnemyDefs)
        {
            var plat = _platCollide[def.platIdx];
            float ex = plat.X + plat.Width * def.xRatio;
            float ey = plat.Y; // feet on platform surface
            _enemies.Add(new Enemy($"도깨비{idx}", 40, 8, 2, idx));
            enemyPosList.Add(new Vector2(ex, ey));
            idx++;
        }

        _enemyPositions = enemyPosList.ToArray();
        _enemyFlash = new float[_enemies.Count];
        _enemyFacingRight = new bool[_enemies.Count];

        _prevKeys = Keyboard.GetState();
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keys = Keyboard.GetState();

        // Sprite animation
        _frameTimer += dt;
        if (_frameTimer >= FrameDuration)
        {
            _frameTimer -= FrameDuration;
            _currentFrame = (_currentFrame + 1) % FrameCount;
        }

        // === Dash double-tap ===
        _lastLeftTap += dt;
        _lastRightTap += dt;

        if (keys.IsKeyDown(Keys.Left) && _prevKeys.IsKeyUp(Keys.Left))
        {
            if (_lastLeftTap < DoubleTapWindow && !_isDashing)
            { _isDashing = true; _dashTimer = DashDuration; _dashDir = -1f; _facingRight = false; SpawnDashParticles(); }
            _lastLeftTap = 0;
        }
        if (keys.IsKeyDown(Keys.Right) && _prevKeys.IsKeyUp(Keys.Right))
        {
            if (_lastRightTap < DoubleTapWindow && !_isDashing)
            { _isDashing = true; _dashTimer = DashDuration; _dashDir = 1f; _facingRight = true; SpawnDashParticles(); }
            _lastRightTap = 0;
        }

        // === Movement ===
        if (_isDashing)
        {
            _dashTimer -= dt;
            _playerVel.X = _dashDir * DashSpeed;
            if (_dashTimer <= 0) _isDashing = false;
        }
        else
        {
            float moveInput = 0;
            if (keys.IsKeyDown(Keys.Left)) moveInput -= 1f;
            if (keys.IsKeyDown(Keys.Right)) moveInput += 1f;
            _playerVel.X = moveInput * MoveSpeed;
            if (moveInput > 0) _facingRight = true;
            else if (moveInput < 0) _facingRight = false;
        }

        // === Jump / Double Jump (연충각) ===
        if (keys.IsKeyDown(Keys.Space) && _prevKeys.IsKeyUp(Keys.Space))
        {
            if (_jumpCount < MaxJumps)
            {
                _playerVel.Y = JumpForce;
                _jumpCount++;
                if (_jumpCount == 2) SpawnDoubleJumpParticles();
            }
        }

        // Gravity
        if (!_onGround) _playerVel.Y += Gravity * dt;
        _playerPos += _playerVel * dt;

        // Platform collision
        _onGround = false;
        if (_playerVel.Y >= 0)
        {
            int pw = (int)(FrameWidth * PlayerScale);
            float footLeft = _playerPos.X - pw * 0.3f;
            float footRight = _playerPos.X + pw * 0.3f;
            float prevFeetY = _playerPos.Y - _playerVel.Y * dt;

            foreach (var plat in _platCollide)
            {
                if (footRight > plat.X && footLeft < plat.X + plat.Width
                    && prevFeetY <= plat.Y && _playerPos.Y >= plat.Y)
                {
                    _playerPos.Y = plat.Y;
                    _playerVel.Y = 0;
                    _onGround = true;
                    _jumpCount = 0;
                    break;
                }
            }
        }

        // Ground collision
        if (_playerPos.Y >= GroundY)
        {
            _playerPos.Y = GroundY;
            _playerVel.Y = 0;
            _onGround = true;
            _jumpCount = 0;
        }

        // World bounds
        int drawW = (int)(FrameWidth * PlayerScale);
        _playerPos.X = MathHelper.Clamp(_playerPos.X, drawW / 2f, WorldWidth - drawW / 2f);

        // === Melee Attack (Ctrl) ===
        if ((keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl)) && !_isAttacking)
        {
            _isAttacking = true;
            _attackTimer = AttackDuration;
            _attackHit = false;
            _slashLife = 0.2f;
        }
        if (_isAttacking)
        {
            _attackTimer -= dt;
            if (_attackTimer <= 0) _isAttacking = false;
            if (!_attackHit) CheckMeleeHits();
        }
        _slashLife = Math.Max(0, _slashLife - dt);

        // === Sword Projectile (A key) ===
        _swordCooldown = Math.Max(0, _swordCooldown - dt);
        if (keys.IsKeyDown(Keys.A) && _prevKeys.IsKeyUp(Keys.A) && _swordCooldown <= 0)
        {
            FireSword();
            _swordCooldown = SwordCooldownTime;
        }
        UpdateSwords(dt);

        // === Attack Projectile (S key) ===
        _attackProjCooldown = Math.Max(0, _attackProjCooldown - dt);
        if (keys.IsKeyDown(Keys.S) && _prevKeys.IsKeyUp(Keys.S) && _attackProjCooldown <= 0)
        {
            FireAttackProj();
            _attackProjCooldown = AttackProjCooldownTime;
        }
        UpdateAttackProjs(dt);

        // === Enemies ===
        UpdateEnemies(dt);

        // === Camera ===
        UpdateCamera();

        // === VFX ===
        UpdateShake(dt);
        UpdateFlash(dt);
        UpdatePopups(dt);
        UpdateParticles(dt);

        _prevKeys = keys;
    }

    // === Sword Projectile ===

    private void FireSword()
    {
        // Find nearest alive enemy
        int bestIdx = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!_enemies[i].IsAlive) continue;
            float d = Vector2.DistanceSquared(_playerPos, _enemyPositions[i]);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }
        if (bestIdx < 0) return;

        int drawH = (int)(FrameHeight * PlayerScale);
        var spawnPos = _playerPos + new Vector2(_facingRight ? 30 : -30, -drawH * 0.5f);

        // Fire 3 swords with different spiral offsets
        for (int s = 0; s < 3; s++)
        {
            // Each sword launches in a slightly different angle
            float spreadAngle = (s - 1) * 0.6f; // -0.6, 0, +0.6 radians
            var dir = _enemyPositions[bestIdx] + new Vector2(0, -drawH * 0.4f) - spawnPos;
            if (dir.LengthSquared() > 0) dir.Normalize();

            // Rotate initial direction by spread
            float cos = MathF.Cos(spreadAngle);
            float sin = MathF.Sin(spreadAngle);
            var spreadDir = new Vector2(dir.X * cos - dir.Y * sin, dir.X * sin + dir.Y * cos);

            _swords.Add(new SwordProjectile
            {
                Position = spawnPos,
                Velocity = spreadDir * SwordSpeed,
                Rotation = MathF.Atan2(spreadDir.X, -spreadDir.Y),
                TargetIndex = bestIdx,
                Alive = true,
                Life = 3f,
                Age = 0f,
                SpiralOffset = s * MathF.PI * 2f / 3f, // 120 degrees apart
            });
        }
    }

    private void UpdateSwords(float dt)
    {
        int drawH = (int)(FrameHeight * PlayerScale);
        float swordSize = 960 * SwordDrawScale; // drawn size of the sword

        for (int i = _swords.Count - 1; i >= 0; i--)
        {
            var s = _swords[i];
            s.Life -= dt;
            if (s.Life <= 0) { _swords.RemoveAt(i); continue; }

            s.Age += dt;

            // Homing: steer toward target
            if (s.TargetIndex >= 0 && s.TargetIndex < _enemies.Count && _enemies[s.TargetIndex].IsAlive)
            {
                var targetPos = _enemyPositions[s.TargetIndex] + new Vector2(0, -drawH * 0.4f);
                var toTarget = targetPos - s.Position;
                float distToTarget = toTarget.Length();
                if (distToTarget > 0)
                {
                    toTarget /= distToTarget; // normalize

                    // Spiral force: perpendicular to toTarget, oscillating
                    float spiralPhase = s.Age * 12f + s.SpiralOffset;
                    var perp = new Vector2(-toTarget.Y, toTarget.X);
                    float spiralStrength = MathF.Sin(spiralPhase) * 350f;
                    // Spiral weakens as sword gets close
                    float closeFactor = MathHelper.Clamp(distToTarget / 200f, 0f, 1f);

                    s.Velocity += toTarget * SwordHomingStrength * SwordSpeed * dt;
                    s.Velocity += perp * spiralStrength * closeFactor * dt;

                    // Clamp speed
                    if (s.Velocity.LengthSquared() > SwordSpeed * SwordSpeed * 1.5f)
                    {
                        s.Velocity.Normalize();
                        s.Velocity *= SwordSpeed;
                    }
                }
            }

            s.Position += s.Velocity * dt;

            // Sword trail particles
            if (_rng.NextDouble() < 0.5)
            {
                _particles.Add(new HitParticle
                {
                    Position = s.Position + new Vector2(_rng.Next(-5, 5), _rng.Next(-5, 5)),
                    Velocity = new Vector2((float)(_rng.NextDouble() - 0.5) * 30, (float)(_rng.NextDouble() - 0.5) * 30),
                    Life = 0.15f + (float)_rng.NextDouble() * 0.1f,
                    Tint = Color.Lerp(new Color(180, 200, 255), Color.White, (float)_rng.NextDouble() * 0.5f),
                    Size = PX * _rng.Next(1, 3)
                });
            }

            // Rotation: 12 o'clock (top) points in velocity direction
            if (s.Velocity.LengthSquared() > 0)
                s.Rotation = MathF.Atan2(s.Velocity.X, -s.Velocity.Y);

            // Hit detection: check against all enemies using sword image size
            float hitRadius = swordSize * 0.3f;
            for (int j = 0; j < _enemies.Count; j++)
            {
                if (!_enemies[j].IsAlive) continue;
                var enemyCenter = _enemyPositions[j] + new Vector2(0, -drawH * 0.4f);
                float dist = Vector2.Distance(s.Position, enemyCenter);
                if (dist < hitRadius + drawH * 0.3f)
                {
                    int dmg = _enemies[j].TakeDamage(_player.Atk);
                    _enemyFlash[j] = 0.5f;
                    TriggerShake(12f, 0.2f);

                    var hitPos = _enemyPositions[j] + new Vector2(0, -drawH * 0.5f);
                    SpawnPopup(hitPos, dmg, true);
                    SpawnHitParticles(hitPos, 18, new Color(100, 150, 255));
                    // Sword trail burst on impact
                    SpawnHitParticles(s.Position, 10, new Color(200, 220, 255));

                    s.Alive = false;
                    break;
                }
            }

            if (!s.Alive) { _swords.RemoveAt(i); continue; }
            _swords[i] = s;
        }
    }

    // === Attack Projectile (S key) ===

    private void FireAttackProj()
    {
        int drawH = (int)(FrameHeight * PlayerScale);
        float dir = _facingRight ? 1f : -1f;
        var spawnPos = _playerPos + new Vector2(dir * 40, -drawH * 0.5f);

        _attackProjs.Add(new SwordProjectile
        {
            Position = spawnPos,
            Velocity = new Vector2(dir * AttackProjSpeed, 0),
            Rotation = 0f,
            TargetIndex = -1,
            Alive = true,
            Life = 2f,
            Age = 0f,
            SpiralOffset = dir,
        });
    }

    private void UpdateAttackProjs(float dt)
    {
        int drawH = (int)(FrameHeight * PlayerScale);
        float projSize = 1024 * AttackProjScale;

        for (int i = _attackProjs.Count - 1; i >= 0; i--)
        {
            var p = _attackProjs[i];
            p.Life -= dt;
            p.Age += dt;
            if (p.Life <= 0) { _attackProjs.RemoveAt(i); continue; }

            p.Position += p.Velocity * dt;
            // Slight rotation for visual flair
            p.Rotation += dt * 3f * p.SpiralOffset;

            // Trail particles
            if (_rng.NextDouble() < 0.6)
            {
                _particles.Add(new HitParticle
                {
                    Position = p.Position + new Vector2(_rng.Next(-8, 8), _rng.Next(-8, 8)),
                    Velocity = new Vector2(-p.Velocity.X * 0.2f + (float)(_rng.NextDouble() - 0.5) * 40, (float)(_rng.NextDouble() - 0.5) * 40),
                    Life = 0.2f + (float)_rng.NextDouble() * 0.15f,
                    Tint = Color.Lerp(new Color(50, 120, 255), new Color(150, 200, 255), (float)_rng.NextDouble()),
                    Size = PX * _rng.Next(1, 3)
                });
            }

            // Hit detection
            float hitRadius = projSize * 0.35f;
            for (int j = 0; j < _enemies.Count; j++)
            {
                if (!_enemies[j].IsAlive) continue;
                var enemyCenter = _enemyPositions[j] + new Vector2(0, -drawH * 0.4f);
                if (Vector2.Distance(p.Position, enemyCenter) < hitRadius + drawH * 0.3f)
                {
                    int dmg = _enemies[j].TakeDamage(_player.Atk + 5);
                    _enemyFlash[j] = 0.6f;
                    TriggerShake(14f, 0.25f);

                    var hitPos = _enemyPositions[j] + new Vector2(0, -drawH * 0.5f);
                    SpawnPopup(hitPos, dmg, true);

                    // Big blue + lightning explosion
                    SpawnHitParticles(hitPos, 24, new Color(30, 100, 255));
                    SpawnHitParticles(hitPos, 12, new Color(180, 220, 255));
                    // Lightning bolts: long thin particles shooting outward
                    for (int b = 0; b < 8; b++)
                    {
                        float angle = b * MathF.PI * 2f / 8f + (float)_rng.NextDouble() * 0.3f;
                        float speed = 300 + (float)_rng.NextDouble() * 200;
                        _particles.Add(new HitParticle
                        {
                            Position = hitPos,
                            Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                            Life = 0.15f + (float)_rng.NextDouble() * 0.1f,
                            Tint = Color.Lerp(new Color(100, 180, 255), Color.White, (float)_rng.NextDouble() * 0.7f),
                            Size = PX * _rng.Next(1, 2)
                        });
                    }

                    p.Alive = false;
                    break;
                }
            }

            if (!p.Alive) { _attackProjs.RemoveAt(i); continue; }
            _attackProjs[i] = p;
        }
    }

    private void CheckMeleeHits()
    {
        int drawH = (int)(FrameHeight * PlayerScale);
        float attackX = _playerPos.X + (_facingRight ? AttackRange * 0.5f : -AttackRange * 0.5f);

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!_enemies[i].IsAlive) continue;
            float dx = Math.Abs(_enemyPositions[i].X - attackX);
            float dy = Math.Abs(_enemyPositions[i].Y - _playerPos.Y);
            if (dx < AttackRange * 0.7f && dy < drawH * 0.6f)
            {
                int dmg = _enemies[i].TakeDamage(_player.Atk);
                _attackHit = true;
                _enemyFlash[i] = 0.5f;
                float knockDir = _facingRight ? 1f : -1f;
                _enemyPositions[i] += new Vector2(knockDir * 40f, 0);
                TriggerShake(10f, 0.15f);
                var hitPos = _enemyPositions[i] + new Vector2(0, -drawH * 0.5f);
                SpawnPopup(hitPos, dmg, dmg >= 15);
                SpawnHitParticles(hitPos, 14, new Color(255, 200, 50));
                break;
            }
        }
    }

    private void SpawnDashParticles()
    {
        int drawH = (int)(FrameHeight * PlayerScale);
        for (int i = 0; i < 8; i++)
        {
            float dir = _dashDir > 0 ? -1f : 1f;
            _particles.Add(new HitParticle
            {
                Position = _playerPos + new Vector2(_rng.Next(-10, 10), -drawH * 0.3f + _rng.Next(-10, 10)),
                Velocity = new Vector2(dir * (100 + (float)_rng.NextDouble() * 80), -20 + (float)_rng.NextDouble() * 40),
                Life = 0.2f + (float)_rng.NextDouble() * 0.15f,
                Tint = Color.Lerp(new Color(200, 180, 140), Color.White, (float)_rng.NextDouble() * 0.5f),
                Size = PX * _rng.Next(1, 3)
            });
        }
    }

    private void SpawnDoubleJumpParticles()
    {
        for (int i = 0; i < 12; i++)
        {
            float angle = MathF.PI * 0.3f + (float)_rng.NextDouble() * MathF.PI * 0.4f;
            float speed = 60 + (float)_rng.NextDouble() * 100;
            _particles.Add(new HitParticle
            {
                Position = _playerPos + new Vector2(_rng.Next(-12, 12), -5),
                Velocity = new Vector2(MathF.Cos(angle) * speed * (_rng.Next(2) == 0 ? -1 : 1), MathF.Sin(angle) * speed),
                Life = 0.25f + (float)_rng.NextDouble() * 0.2f,
                Tint = Color.Lerp(new Color(150, 200, 255), Color.White, (float)_rng.NextDouble() * 0.4f),
                Size = PX * _rng.Next(1, 3)
            });
        }
    }

    private float[] _enemyVelY; // vertical velocity for gravity

    private void UpdateEnemies(float dt)
    {
        int drawW = (int)(FrameWidth * PlayerScale);
        int drawH = (int)(FrameHeight * PlayerScale);

        // Lazy init velocity array
        if (_enemyVelY == null) _enemyVelY = new float[_enemies.Count];

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!_enemies[i].IsAlive) continue;

            float dx = _playerPos.X - _enemyPositions[i].X;
            _enemyFacingRight[i] = dx > 0;

            float dist = Math.Abs(dx);
            if (dist < 400 && dist > 60)
            {
                float dir = Math.Sign(dx);
                _enemyPositions[i] += new Vector2(dir * 80f * dt, 0);
            }

            // Enemy gravity + platform collision
            _enemyVelY[i] += Gravity * dt;
            _enemyPositions[i] += new Vector2(0, _enemyVelY[i] * dt);

            // Check platform collision for enemy
            foreach (var plat in _platCollide)
            {
                if (_enemyPositions[i].X > plat.X && _enemyPositions[i].X < plat.X + plat.Width
                    && _enemyPositions[i].Y >= plat.Y && _enemyPositions[i].Y <= plat.Y + 20)
                {
                    _enemyPositions[i] = new Vector2(_enemyPositions[i].X, plat.Y);
                    _enemyVelY[i] = 0;
                    break;
                }
            }

            // Ground collision
            if (_enemyPositions[i].Y >= GroundY)
            {
                _enemyPositions[i] = new Vector2(_enemyPositions[i].X, GroundY);
                _enemyVelY[i] = 0;
            }

            // Contact damage
            float attackDist = drawW * 0.6f;
            float vertDist = Math.Abs(_playerPos.Y - _enemyPositions[i].Y);
            if (dist < attackDist && vertDist < drawH * 0.5f && _playerFlash <= 0 && !_isDashing)
            {
                int dmg = _player.TakeDamage(_enemies[i].Atk);
                _playerFlash = 0.8f;
                TriggerShake(6f, 0.15f);
                SpawnPopup(_playerPos + new Vector2(0, -drawH * 0.6f), dmg, false);
                SpawnHitParticles(_playerPos + new Vector2(0, -drawH * 0.4f), 8, new Color(255, 80, 80));
            }
        }
    }

    private void UpdateCamera()
    {
        int screenW = GraphicsDevice.Viewport.Width;
        float targetX = _playerPos.X - screenW / 2f;
        targetX = MathHelper.Clamp(targetX, 0, WorldWidth - screenW);
        _camera.X += (targetX - _camera.X) * 0.1f;
        _camera.Y = 0;
    }

    // === VFX ===

    private void TriggerShake(float intensity, float duration)
    {
        _shakeIntensity = Math.Max(_shakeIntensity, intensity);
        _shakeDuration = Math.Max(_shakeDuration, duration);
        _shakeTimer = 0f;
    }

    private void UpdateShake(float dt)
    {
        if (_shakeDuration > 0)
        {
            _shakeTimer += dt;
            if (_shakeTimer >= _shakeDuration)
            { _shakeDuration = 0; _shakeIntensity = 0; }
        }
    }

    private Vector2 GetShakeOffset()
    {
        if (_shakeDuration <= 0) return Vector2.Zero;
        float fade = 1f - (_shakeTimer / _shakeDuration);
        return new Vector2(
            (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity * fade,
            (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity * fade);
    }

    private void UpdateFlash(float dt)
    {
        if (_playerFlash > 0) _playerFlash = Math.Max(0, _playerFlash - dt * 2f);
        for (int i = 0; i < _enemyFlash.Length; i++)
            if (_enemyFlash[i] > 0) _enemyFlash[i] = Math.Max(0, _enemyFlash[i] - dt * 5f);
    }

    private void SpawnPopup(Vector2 worldPos, int damage, bool isCrit)
    {
        _popups.Add(new DamagePopup
        {
            Position = worldPos + new Vector2(_rng.Next(-15, 15), -20),
            Damage = damage, Life = 1.2f, MaxLife = 1.2f, IsCrit = isCrit
        });
    }

    private void UpdatePopups(float dt)
    {
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Life -= dt;
            p.Position -= new Vector2(0, 50 * dt);
            if (p.Life <= 0) _popups.RemoveAt(i);
            else _popups[i] = p;
        }
    }

    private void SpawnHitParticles(Vector2 center, int count, Color baseColor)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
            float speed = 80 + (float)_rng.NextDouble() * 200;
            _particles.Add(new HitParticle
            {
                Position = center + new Vector2(_rng.Next(-8, 8), _rng.Next(-8, 8)),
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Life = 0.3f + (float)_rng.NextDouble() * 0.3f,
                Tint = Color.Lerp(baseColor, Color.White, (float)_rng.NextDouble() * 0.5f),
                Size = PX * _rng.Next(1, 4)
            });
        }
    }

    private void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= dt;
            p.Position += p.Velocity * dt;
            p.Velocity *= 0.92f;
            p.Velocity += new Vector2(0, 300 * dt);
            if (p.Life <= 0) _particles.RemoveAt(i);
            else _particles[i] = p;
        }
    }

    // === Draw ===

    public override void Draw(SpriteBatch spriteBatch)
    {
        int screenW = GraphicsDevice.Viewport.Width;
        int screenH = GraphicsDevice.Viewport.Height;
        var shake = GetShakeOffset();

        GraphicsDevice.Clear(Color.Black);

        var camMatrix = Matrix.CreateTranslation(-_camera.X + shake.X, -_camera.Y + shake.Y, 0);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camMatrix);

        // Background
        int bgW = _background.Width;
        for (int bx = 0; bx < WorldWidth; bx += bgW)
            spriteBatch.Draw(_background, new Rectangle(bx, 0, bgW, screenH), Color.White);

        // Platforms
        for (int i = 0; i < _platDraw.Length; i++)
            spriteBatch.Draw(_groundTex, _platDraw[i], _groundSrc, Color.White);

        DrawEnemies(spriteBatch);
        DrawPlayer(spriteBatch);
        DrawSlashVFX(spriteBatch);
        DrawSwords(spriteBatch);
        DrawAttackProjs(spriteBatch);

        // Particles
        foreach (var p in _particles)
        {
            float alpha = Math.Clamp(p.Life * 3f, 0f, 1f);
            spriteBatch.Draw(_pixel, new Rectangle((int)p.Position.X, (int)p.Position.Y, p.Size, p.Size),
                p.Tint * alpha);
        }

        // Damage popups
        foreach (var pop in _popups)
        {
            float alpha = Math.Clamp(pop.Life / pop.MaxLife * 2f, 0f, 1f);
            float scale = pop.IsCrit ? 1.5f : 1f;
            float lifeRatio = 1f - (pop.Life / pop.MaxLife);
            if (lifeRatio < 0.1f) scale *= (0.5f + lifeRatio / 0.1f * 0.5f);

            string dmgText = pop.Damage.ToString();
            Color dmgColor = pop.IsCrit ? new Color(255, 220, 50) : Color.White;
            var sz = _font.MeasureString(dmgText);
            var pos = pop.Position - sz * scale / 2f;

            spriteBatch.DrawString(_font, dmgText, pos + new Vector2(PX, PX),
                new Color(0, 0, 0, (int)(180 * alpha)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, dmgText, pos,
                dmgColor * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        spriteBatch.End();

        // HUD
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHPBar(spriteBatch, screenW);
        spriteBatch.End();
    }

    private void DrawPlayer(SpriteBatch spriteBatch)
    {
        int drawW = (int)(FrameWidth * PlayerScale);
        int drawH = (int)(FrameHeight * PlayerScale);
        var sourceRect = new Rectangle(_currentFrame * FrameWidth, 0, FrameWidth, FrameHeight);

        int px = (int)_playerPos.X - drawW / 2;
        int py = (int)_playerPos.Y - drawH;

        if (_isDashing)
        {
            var effect2 = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int ghostX = px - (int)(_dashDir * 20);
            spriteBatch.Draw(_playerIdle, new Rectangle(ghostX, py, drawW, drawH),
                sourceRect, Color.White * 0.3f, 0f, Vector2.Zero, effect2, 0);
        }

        Color tint = Color.White;
        if (_playerFlash > 0)
        {
            if (((int)(_playerFlash * 10) % 2) != 0) return;
            tint = Color.Lerp(Color.White, new Color(255, 100, 100), _playerFlash);
        }

        var effect = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        spriteBatch.Draw(_playerIdle, new Rectangle(px, py, drawW, drawH),
            sourceRect, tint, 0f, Vector2.Zero, effect, 0);

        // Shadow
        int shadowW = (int)(drawW * 0.6f);
        float shadowAlpha = _onGround ? 0.3f : 0.15f;
        DrawRect(spriteBatch, (int)_playerPos.X - shadowW / 2, GroundY - 2, shadowW, 4,
            Color.Black * shadowAlpha);
    }

    private void DrawSlashVFX(SpriteBatch spriteBatch)
    {
        if (_slashLife <= 0) return;

        int drawH = (int)(FrameHeight * PlayerScale);
        float alpha = _slashLife / 0.2f;
        float dir = _facingRight ? 1f : -1f;
        var center = _playerPos + new Vector2(dir * 50, -drawH * 0.5f);

        for (int i = 0; i < 5; i++)
        {
            float a = (dir > 0 ? -0.5f : 0.5f) + i * dir * 0.25f;
            float r = 30 + i * 8;
            var p1 = center + new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r);
            int size = 3 - i / 2;
            spriteBatch.Draw(_pixel,
                new Rectangle((int)p1.X, (int)p1.Y, size * PX, size * PX),
                new Color(255, 240, 200) * alpha);
        }
    }

    private void DrawSwords(SpriteBatch spriteBatch)
    {
        int texW = _swardTex.Width;
        int texH = _swardTex.Height;
        var origin = new Vector2(texW / 2f, texH / 2f); // center of image

        foreach (var s in _swords)
        {
            // Trail particles
            float trailAlpha = 0.4f;
            spriteBatch.Draw(_pixel,
                new Rectangle((int)s.Position.X - 1, (int)s.Position.Y - 1, 3, 3),
                new Color(150, 180, 255) * trailAlpha);

            // Draw sword: origin at center, rotation so top (12 o'clock) points in velocity direction
            spriteBatch.Draw(_swardTex, s.Position, null, Color.White,
                s.Rotation, origin, SwordDrawScale, SpriteEffects.None, 0);
        }
    }

    private void DrawAttackProjs(SpriteBatch spriteBatch)
    {
        int texW = _attackTex.Width;
        int texH = _attackTex.Height;
        var origin = new Vector2(texW / 2f, texH / 2f);

        foreach (var p in _attackProjs)
        {
            // Glow behind
            spriteBatch.Draw(_pixel,
                new Rectangle((int)p.Position.X - 6, (int)p.Position.Y - 6, 12, 12),
                new Color(50, 120, 255) * 0.4f);

            spriteBatch.Draw(_attackTex, p.Position, null, Color.White,
                p.Rotation, origin, AttackProjScale, SpriteEffects.None, 0);
        }
    }

    private void DrawEnemies(SpriteBatch spriteBatch)
    {
        int drawW = (int)(FrameWidth * PlayerScale);
        int drawH = (int)(FrameHeight * PlayerScale);
        var sourceRect = new Rectangle(_currentFrame * FrameWidth, 0, FrameWidth, FrameHeight);

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!_enemies[i].IsAlive) continue;

            int ex = (int)_enemyPositions[i].X - drawW / 2;
            int ey = (int)_enemyPositions[i].Y - drawH;

            Color tint = _enemyFlash[i] > 0
                ? Color.Lerp(Color.White, new Color(255, 100, 100), _enemyFlash[i])
                : Color.White;

            var effect = _enemyFacingRight[i] ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(_enemyIdle, new Rectangle(ex, ey, drawW, drawH),
                sourceRect, tint, 0f, Vector2.Zero, effect, 0);

            DrawEnemyHpBar(spriteBatch, (int)_enemyPositions[i].X, ey - 12, _enemies[i]);

            int shadowW = (int)(drawW * 0.5f);
            DrawRect(spriteBatch, (int)_enemyPositions[i].X - shadowW / 2, GroundY - 2, shadowW, 4,
                Color.Black * 0.25f);
        }
    }

    private void DrawHPBar(SpriteBatch spriteBatch, int screenW)
    {
        int hpBarW = 180;
        int hpBarH = 12;
        int hpX = 16;
        int hpY = 16;

        DrawRect(spriteBatch, hpX - 1, hpY - 1, hpBarW + 2, hpBarH + 2, new Color(40, 30, 20));
        DrawRect(spriteBatch, hpX, hpY, hpBarW, hpBarH, new Color(20, 10, 15));
        float hpRatio = (float)_player.Hp / _player.MaxHp;
        if (hpRatio > 0)
        {
            Color hpColor = hpRatio > 0.5f ? new Color(50, 180, 80) :
                            hpRatio > 0.25f ? new Color(200, 170, 40) :
                            new Color(200, 50, 50);
            DrawRect(spriteBatch, hpX, hpY, (int)(hpBarW * hpRatio), hpBarH, hpColor);
        }

        string hpText = $"{_player.Hp}/{_player.MaxHp}";
        var hpSize = _font.MeasureString(hpText);
        spriteBatch.DrawString(_font, hpText,
            new Vector2(hpX + hpBarW + 8, hpY + hpBarH / 2 - hpSize.Y / 2),
            new Color(200, 180, 140));
    }

    private void DrawEnemyHpBar(SpriteBatch spriteBatch, int cx, int y, Character ch)
    {
        int barW = 50;
        int barH = 6;
        int x = cx - barW / 2;
        DrawRect(spriteBatch, x, y, barW, barH, new Color(20, 10, 15));
        float ratio = (float)ch.Hp / ch.MaxHp;
        if (ratio > 0)
            DrawRect(spriteBatch, x, y, (int)(barW * ratio), barH, new Color(200, 50, 50));
    }

    private void DrawRect(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
