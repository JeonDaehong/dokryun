using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Entities;
using Dokryun.Systems;

namespace Dokryun.Scenes;

// Floating damage number
public struct DamagePopup
{
    public Vector2 Position;
    public int Damage;
    public float Life;      // remaining lifetime
    public float MaxLife;
    public bool IsCrit;
}

// Hit particle
public struct HitParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public Color Tint;
    public int Size;
}

public class GameplayScene : Scene
{
    private Texture2D _background;
    private Texture2D _playerIdle;
    private Texture2D _enemyIdle;
    private Texture2D _pixel;
    private SpriteFont _font;

    private const int FrameCount = 5;
    private const int FrameWidth = 560;
    private const int FrameHeight = 560;
    private const float FrameDuration = 0.15f;

    private int _currentFrame;
    private float _frameTimer;

    private const float PlayerScale = 0.2f;

    // UI constants
    private const int PX = 2;
    private const int UiBarHeight = 220;
    private const int SlotSize = 40 * PX;
    private const int SlotCount = 16;
    private const int SlotsPerRow = 8;

    // Turn system
    private Character _player;
    private List<Enemy> _enemies;
    private TurnManager _turnManager;

    // Input
    private MouseState _prevMouse;


    // === VFX ===
    // Screen shake
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private Random _rng = new();

    // Damage flash
    private float _playerFlash;
    private float[] _enemyFlash;

    // Knockback offsets
    private float[] _enemyKnockback;
    private float _playerKnockback;

    // Damage popups
    private List<DamagePopup> _popups = new();

    // Hit particles
    private List<HitParticle> _particles = new();

    // Track previous HP to detect hits
    private int _prevPlayerHp;
    private int[] _prevEnemyHp;

    // Enemy spawn animation
    private float _spawnTimer;
    private const float SpawnInterval = 0.4f;  // delay between each spawn
    private int _spawnedCount;
    private float[] _enemySpawnScale;           // 0 = invisible, 1 = full size
    private bool _spawning = true;

    protected override void LoadContent()
    {
        _background = Content.Load<Texture2D>("Sprites/stage1_background");
        _playerIdle = Content.Load<Texture2D>("Sprites/player_1_idle");
        _enemyIdle = Content.Load<Texture2D>("Sprites/enemy_1_idle");
        _font = Content.Load<SpriteFont>("Fonts/GameFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _player = new Character { Name = "검사", Hp = 100, MaxHp = 100, Atk = 18, Def = 4 };
        _enemies = new List<Enemy>
        {
            new Enemy("도깨비A", 40, 8, 2, 0),
            new Enemy("도깨비B", 40, 8, 2, 1),
            new Enemy("도깨비C", 40, 8, 2, 2),
        };
        _enemyFlash = new float[_enemies.Count];
        _enemyKnockback = new float[_enemies.Count];
        _prevEnemyHp = _enemies.Select(e => e.Hp).ToArray();
        _prevPlayerHp = _player.Hp;
        _enemySpawnScale = new float[_enemies.Count];
        _spawnedCount = 0;
        _spawnTimer = 0f;
        _spawning = true;

        _turnManager = new TurnManager(_player, _enemies);

        _prevMouse = Mouse.GetState();
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Sprite animation
        _frameTimer += dt;
        if (_frameTimer >= FrameDuration)
        {
            _frameTimer -= FrameDuration;
            _currentFrame = (_currentFrame + 1) % FrameCount;
        }

        // Spawn animation
        UpdateSpawn(dt);

        // VFX updates
        UpdateShake(dt);
        UpdateFlash(dt);
        UpdateKnockback(dt);
        UpdatePopups(dt);
        UpdateParticles(dt);

        // Store previous HP
        _prevPlayerHp = _player.Hp;
        for (int i = 0; i < _enemies.Count; i++)
            _prevEnemyHp[i] = _enemies[i].Hp;

        // Turn system
        _turnManager.Update(dt);

        // Detect damage events (HP decreased after update)
        DetectHits();

        // Input
        var mouse = Mouse.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed;

        if (_turnManager.Phase == TurnPhase.PlayerChoose && !_spawning && clicked)
        {
            var mousePos = new Point(mouse.X, mouse.Y);

            // Click enemy to select
            int screenW = GraphicsDevice.Viewport.Width;
            int screenH = GraphicsDevice.Viewport.Height;
            int drawW = (int)(FrameWidth * PlayerScale);
            int drawH = (int)(FrameHeight * PlayerScale);
            int enemyBaseX = screenW - 100 - drawW;
            int enemySpacing = drawW - 10;

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (!_enemies[i].IsAlive) continue;
                var eRect = new Rectangle(
                    enemyBaseX - i * enemySpacing,
                    screenH - drawH - 315, drawW, drawH);
                if (eRect.Contains(mousePos))
                {
                    _turnManager.SelectedTarget = i;
                    _turnManager.PlayerAttack();
                    break;
                }
            }
        }

        _prevMouse = mouse;
    }

    private void DetectHits()
    {
        int screenW = GraphicsDevice.Viewport.Width;
        int screenH = GraphicsDevice.Viewport.Height;
        int drawW = (int)(FrameWidth * PlayerScale);
        int drawH = (int)(FrameHeight * PlayerScale);

        // Player got hit
        if (_player.Hp < _prevPlayerHp)
        {
            int dmg = _prevPlayerHp - _player.Hp;
            _playerFlash = 1f;
            _playerKnockback = 30f;
            TriggerShake(6f, 0.2f);

            var playerCenter = new Vector2(100 + drawW / 2, screenH - drawH - 315 + drawH / 2);
            SpawnPopup(playerCenter, dmg, false);
            SpawnHitParticles(playerCenter, 8, new Color(255, 80, 80));
        }

        // Enemies got hit
        int enemyBaseX = screenW - 100 - drawW;
        int enemySpacing = drawW - 10;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].Hp < _prevEnemyHp[i])
            {
                int dmg = _prevEnemyHp[i] - _enemies[i].Hp;
                _enemyFlash[i] = 1f;
                _enemyKnockback[i] = 25f;
                TriggerShake(8f, 0.25f);

                var enemyCenter = new Vector2(
                    enemyBaseX - i * enemySpacing + drawW / 2,
                    screenH - drawH - 315 + drawH / 2);
                SpawnPopup(enemyCenter, dmg, dmg >= 15);
                SpawnHitParticles(enemyCenter, 12, new Color(255, 200, 50));
            }
        }
    }

    // === VFX Methods ===

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
            {
                _shakeDuration = 0;
                _shakeIntensity = 0;
            }
        }
    }

    private Vector2 GetShakeOffset()
    {
        if (_shakeDuration <= 0) return Vector2.Zero;
        float fade = 1f - (_shakeTimer / _shakeDuration);
        float x = (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity * fade;
        float y = (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity * fade;
        return new Vector2(x, y);
    }

    private void UpdateFlash(float dt)
    {
        if (_playerFlash > 0) _playerFlash = Math.Max(0, _playerFlash - dt * 5f);
        for (int i = 0; i < _enemyFlash.Length; i++)
            if (_enemyFlash[i] > 0) _enemyFlash[i] = Math.Max(0, _enemyFlash[i] - dt * 5f);
    }

    private void UpdateKnockback(float dt)
    {
        float recovery = 120f * dt;
        if (_playerKnockback > 0) _playerKnockback = Math.Max(0, _playerKnockback - recovery);
        for (int i = 0; i < _enemyKnockback.Length; i++)
            if (_enemyKnockback[i] > 0) _enemyKnockback[i] = Math.Max(0, _enemyKnockback[i] - recovery);
    }

    private void SpawnPopup(Vector2 pos, int damage, bool isCrit)
    {
        _popups.Add(new DamagePopup
        {
            Position = pos + new Vector2(_rng.Next(-15, 15), -30),
            Damage = damage,
            Life = 1.2f,
            MaxLife = 1.2f,
            IsCrit = isCrit
        });
    }

    private void UpdatePopups(float dt)
    {
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Life -= dt;
            p.Position -= new Vector2(0, 50 * dt); // float upward
            if (p.Life <= 0)
                _popups.RemoveAt(i);
            else
                _popups[i] = p;
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
                Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                Life = 0.3f + (float)_rng.NextDouble() * 0.3f,
                Tint = Color.Lerp(baseColor, Color.White, (float)_rng.NextDouble() * 0.5f),
                Size = PX * (_rng.Next(1, 4))
            });
        }
    }

    private void UpdateSpawn(float dt)
    {
        if (!_spawning) return;

        _spawnTimer += dt;

        // Spawn next enemy when timer passes interval
        while (_spawnedCount < _enemies.Count && _spawnTimer >= SpawnInterval * (_spawnedCount + 1))
        {
            int idx = _spawnedCount;
            _spawnedCount++;

            // Spawn particles (smoke poof)
            int screenW = GraphicsDevice.Viewport.Width;
            int drawW = (int)(FrameWidth * PlayerScale);
            int drawH = (int)(FrameHeight * PlayerScale);
            int enemyBaseX = screenW - 100 - drawW;
            int enemySpacing = drawW - 10;
            int screenH = GraphicsDevice.Viewport.Height;

            var center = new Vector2(
                enemyBaseX - idx * enemySpacing + drawW / 2,
                screenH - drawH - 315 + drawH / 2);

            // Smoke particles
            for (int i = 0; i < 16; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                float speed = 40 + (float)_rng.NextDouble() * 120;
                _particles.Add(new HitParticle
                {
                    Position = center + new Vector2(_rng.Next(-10, 10), _rng.Next(-10, 10)),
                    Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 40),
                    Life = 0.4f + (float)_rng.NextDouble() * 0.3f,
                    Tint = Color.Lerp(new Color(180, 170, 160), new Color(100, 90, 80), (float)_rng.NextDouble()),
                    Size = PX * _rng.Next(2, 5)
                });
            }

            TriggerShake(4f, 0.15f);
        }

        // Animate scale for spawned enemies
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (i < _spawnedCount)
            {
                _enemySpawnScale[i] = Math.Min(1f, _enemySpawnScale[i] + dt * 8f);
                // Overshoot bounce effect
                float timeSinceSpawn = _spawnTimer - SpawnInterval * (i + 1);
                if (timeSinceSpawn >= 0 && timeSinceSpawn < 0.15f)
                {
                    float bounce = (float)Math.Sin(timeSinceSpawn / 0.15f * Math.PI) * 0.2f;
                    _enemySpawnScale[i] = Math.Min(1f + bounce, _enemySpawnScale[i] + bounce);
                }
            }
        }

        // All spawned and settled
        if (_spawnedCount >= _enemies.Count && _enemySpawnScale[_enemies.Count - 1] >= 0.99f)
        {
            for (int i = 0; i < _enemies.Count; i++)
                _enemySpawnScale[i] = 1f;
            _spawning = false;
        }
    }

    private void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= dt;
            p.Position += p.Velocity * dt;
            p.Velocity *= 0.92f; // friction
            p.Velocity += new Vector2(0, 300 * dt); // gravity
            if (p.Life <= 0)
                _particles.RemoveAt(i);
            else
                _particles[i] = p;
        }
    }

    // === Lunge offset calculation ===
    private Vector2 GetPlayerLungeOffset()
    {
        var phase = _turnManager.Phase;
        if (phase != TurnPhase.PlayerLunge && phase != TurnPhase.PlayerImpact && phase != TurnPhase.PlayerReturn)
            return Vector2.Zero;

        int targetIdx = _turnManager.LastTargetIndex;
        if (targetIdx < 0) return Vector2.Zero;

        int screenW = GraphicsDevice.Viewport.Width;
        int drawW = (int)(FrameWidth * PlayerScale);
        int enemyBaseX = screenW - 100 - drawW;
        int enemySpacing = drawW - 10;
        int targetX = enemyBaseX - targetIdx * enemySpacing;
        float lungeX = (targetX - 100) * 0.6f; // lunge 60% toward target

        float t = _turnManager.AnimProgress;
        return phase switch
        {
            TurnPhase.PlayerLunge => new Vector2(EaseOutQuad(t) * lungeX, -EaseOutQuad(t) * 10),
            TurnPhase.PlayerImpact => new Vector2(lungeX, -10),
            TurnPhase.PlayerReturn => new Vector2((1f - EaseInQuad(t)) * lungeX, -(1f - EaseInQuad(t)) * 10),
            _ => Vector2.Zero
        };
    }

    private Vector2 GetEnemyLungeOffset(int index)
    {
        var phase = _turnManager.Phase;
        if (_turnManager.LastTargetIndex != index) return Vector2.Zero;
        if (phase != TurnPhase.EnemyLunge && phase != TurnPhase.EnemyImpact && phase != TurnPhase.EnemyReturn)
            return Vector2.Zero;

        int screenW = GraphicsDevice.Viewport.Width;
        int drawW = (int)(FrameWidth * PlayerScale);
        int enemyBaseX = screenW - 100 - drawW;
        int enemySpacing = drawW - 10;
        int enemyX = enemyBaseX - index * enemySpacing;
        float lungeX = (100 - enemyX) * 0.5f; // lunge 50% toward player

        float t = _turnManager.AnimProgress;
        return phase switch
        {
            TurnPhase.EnemyLunge => new Vector2(EaseOutQuad(t) * lungeX, -EaseOutQuad(t) * 8),
            TurnPhase.EnemyImpact => new Vector2(lungeX, -8),
            TurnPhase.EnemyReturn => new Vector2((1f - EaseInQuad(t)) * lungeX, -(1f - EaseInQuad(t)) * 8),
            _ => Vector2.Zero
        };
    }

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInQuad(float t) => t * t;

    // === Draw ===

    public override void Draw(SpriteBatch spriteBatch)
    {
        int screenW = GraphicsDevice.Viewport.Width;
        int screenH = GraphicsDevice.Viewport.Height;
        var shake = GetShakeOffset();

        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateTranslation(shake.X, shake.Y, 0));

        // Background
        spriteBatch.Draw(_background, new Rectangle(0, 0, screenW, screenH), Color.White);

        // Player
        var sourceRect = new Rectangle(_currentFrame * FrameWidth, 0, FrameWidth, FrameHeight);
        int drawWidth = (int)(FrameWidth * PlayerScale);
        int drawHeight = (int)(FrameHeight * PlayerScale);

        var playerLunge = GetPlayerLungeOffset();
        int playerX = 100 + (int)playerLunge.X - (int)_playerKnockback;
        int playerY = screenH - drawHeight - 315 + (int)playerLunge.Y;

        Color playerTint = _playerFlash > 0 ? Color.Lerp(Color.White, new Color(255, 100, 100), _playerFlash) : Color.White;
        if (_player.IsAlive)
        {
            spriteBatch.Draw(_playerIdle, new Rectangle(playerX, playerY, drawWidth, drawHeight),
                sourceRect, playerTint);
            DrawHpBar(spriteBatch, playerX + drawWidth / 2, playerY + drawHeight + 5, _player, new Color(50, 180, 80));
        }

        // Enemies
        var enemySourceRect = new Rectangle(_currentFrame * FrameWidth, 0, FrameWidth, FrameHeight);
        int enemySpacingVal = drawWidth - 10;
        int enemyBaseX = screenW - 100 - drawWidth;

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!_enemies[i].IsAlive) continue;
            if (_enemySpawnScale[i] <= 0.01f) continue; // not spawned yet

            float spawnScale = _enemySpawnScale[i];

            var lunge = GetEnemyLungeOffset(i);
            int baseEx = enemyBaseX - i * enemySpacingVal;
            int baseEy = screenH - drawHeight - 315;
            int ex = baseEx + (int)lunge.X + (int)_enemyKnockback[i];
            int ey = baseEy + (int)lunge.Y;

            Color tint = _enemyFlash[i] > 0 ? Color.Lerp(Color.White, new Color(255, 100, 100), _enemyFlash[i]) : Color.White;

            // Selection indicator
            if (_turnManager.SelectedTarget == i && !_spawning)
                DrawPixelRect(spriteBatch, ex + drawWidth / 2 - 6 * PX, ey - 20, 12 * PX, 3 * PX, Color.Yellow);

            // Apply spawn scale (grow from center-bottom)
            int scaledW = (int)(drawWidth * spawnScale);
            int scaledH = (int)(drawHeight * spawnScale);
            int drawX = ex + (drawWidth - scaledW) / 2;
            int drawY = ey + (drawHeight - scaledH); // anchor bottom

            spriteBatch.Draw(_enemyIdle, new Rectangle(drawX, drawY, scaledW, scaledH),
                enemySourceRect, tint);

            if (spawnScale >= 0.9f)
                DrawHpBar(spriteBatch, ex + drawWidth / 2, ey + drawHeight + 5, _enemies[i], new Color(200, 50, 50));
        }

        // Hit particles
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
            // Early pop: scale up
            float lifeRatio = 1f - (pop.Life / pop.MaxLife);
            if (lifeRatio < 0.1f) scale *= (0.5f + lifeRatio / 0.1f * 0.5f);

            string dmgText = pop.Damage.ToString();
            Color dmgColor = pop.IsCrit ? new Color(255, 220, 50) : Color.White;

            var sz = _font.MeasureString(dmgText);
            var pos = pop.Position - sz * scale / 2f;

            // Shadow
            spriteBatch.DrawString(_font, dmgText, pos + new Vector2(PX, PX),
                new Color(0, 0, 0, (int)(180 * alpha)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, dmgText, pos,
                dmgColor * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        spriteBatch.End();

        // UI (no shake)
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        int uiY = screenH - UiBarHeight;
        DrawPixelRect(spriteBatch, 0, uiY, screenW, UiBarHeight, new Color(12, 6, 18, 230));
        DrawPixelRect(spriteBatch, 0, uiY, screenW, 2 * PX, new Color(65, 45, 30));
        DrawPixelRect(spriteBatch, 0, uiY + 2 * PX, screenW, 1 * PX, new Color(95, 70, 45));
        DrawPixelRect(spriteBatch, 0, uiY + 3 * PX, screenW, 1 * PX, new Color(45, 30, 20));

        // Item Slots
        DrawItemSlots(spriteBatch, screenW, uiY);

        spriteBatch.End();
    }

    // === UI Drawing ===

    private void DrawHpBar(SpriteBatch spriteBatch, int cx, int y, Character ch, Color fillColor)
    {
        int barW = 30 * PX;
        int barH = 4 * PX;
        int x = cx - barW / 2;

        DrawPixelRect(spriteBatch, x, y, barW, barH, new Color(20, 10, 15));
        float ratio = (float)ch.Hp / ch.MaxHp;
        int fillW = (int)(barW * ratio);
        if (fillW > 0)
            DrawPixelRect(spriteBatch, x, y, fillW, barH, fillColor);
        DrawPixelRect(spriteBatch, x, y, barW, PX, new Color(80, 60, 50));
        DrawPixelRect(spriteBatch, x, y + barH - PX, barW, PX, new Color(40, 30, 25));
        DrawPixelRect(spriteBatch, x, y, PX, barH, new Color(80, 60, 50));
        DrawPixelRect(spriteBatch, x + barW - PX, y, PX, barH, new Color(40, 30, 25));

    }

    private void DrawItemSlots(SpriteBatch spriteBatch, int screenW, int uiY)
    {
        int gap = 2 * PX;
        int totalWidth = SlotsPerRow * SlotSize + (SlotsPerRow - 1) * gap;
        int totalHeight = 2 * SlotSize + gap;
        int startX = screenW / 2 - totalWidth / 2;
        int startY = uiY + (UiBarHeight - totalHeight) / 2;

        int fp = 4 * PX;
        DrawPixelRect(spriteBatch, startX - fp + PX * 2, startY - fp + PX * 2,
            totalWidth + fp * 2, totalHeight + fp * 2, new Color(0, 0, 0, 140));
        DrawPixelRect(spriteBatch, startX - fp, startY - fp,
            totalWidth + fp * 2, totalHeight + fp * 2, new Color(50, 35, 25));
        DrawPixelRect(spriteBatch, startX - fp, startY - fp, totalWidth + fp * 2, 2 * PX, new Color(90, 70, 50));
        DrawPixelRect(spriteBatch, startX - fp, startY - fp, 2 * PX, totalHeight + fp * 2, new Color(90, 70, 50));
        DrawPixelRect(spriteBatch, startX - fp, startY + totalHeight + fp - 2 * PX, totalWidth + fp * 2, 2 * PX, new Color(25, 18, 12));
        DrawPixelRect(spriteBatch, startX + totalWidth + fp - 2 * PX, startY - fp, 2 * PX, totalHeight + fp * 2, new Color(25, 18, 12));
        DrawPixelRect(spriteBatch, startX - PX * 2, startY - PX * 2,
            totalWidth + PX * 4, totalHeight + PX * 4, new Color(20, 14, 10));

        Color stud = new Color(120, 95, 60);
        int ss = 3 * PX;
        DrawPixelRect(spriteBatch, startX - fp + PX, startY - fp + PX, ss, ss, stud);
        DrawPixelRect(spriteBatch, startX + totalWidth + fp - PX - ss, startY - fp + PX, ss, ss, stud);
        DrawPixelRect(spriteBatch, startX - fp + PX, startY + totalHeight + fp - PX - ss, ss, ss, stud);
        DrawPixelRect(spriteBatch, startX + totalWidth + fp - PX - ss, startY + totalHeight + fp - PX - ss, ss, ss, stud);

        for (int i = 0; i < SlotCount; i++)
        {
            int col = i % SlotsPerRow;
            int row = i / SlotsPerRow;
            int x = startX + col * (SlotSize + gap);
            int y = startY + row * (SlotSize + gap);

            DrawPixelRect(spriteBatch, x, y, SlotSize, SlotSize, new Color(10, 7, 14));
            int m = 2 * PX;
            DrawPixelRect(spriteBatch, x + m, y + m, SlotSize - m * 2, SlotSize - m * 2, new Color(18, 13, 25));
            DrawPixelRect(spriteBatch, x, y, SlotSize, PX, new Color(70, 55, 40));
            DrawPixelRect(spriteBatch, x, y, PX, SlotSize, new Color(70, 55, 40));
            DrawPixelRect(spriteBatch, x, y + SlotSize - PX, SlotSize, PX, new Color(22, 15, 10));
            DrawPixelRect(spriteBatch, x + SlotSize - PX, y, PX, SlotSize, new Color(22, 15, 10));
            DrawPixelRect(spriteBatch, x + PX, y + PX, SlotSize - 2 * PX, PX, new Color(6, 4, 10));
            DrawPixelRect(spriteBatch, x + PX, y + PX, PX, SlotSize - 2 * PX, new Color(6, 4, 10));
            DrawPixelRect(spriteBatch, x + PX, y + SlotSize - 2 * PX, SlotSize - 2 * PX, PX, new Color(38, 30, 45));
            DrawPixelRect(spriteBatch, x + SlotSize - 2 * PX, y + PX, PX, SlotSize - 2 * PX, new Color(38, 30, 45));

            Color rivet = new Color(110, 90, 60);
            DrawPixelRect(spriteBatch, x, y, PX, PX, rivet);
            DrawPixelRect(spriteBatch, x + SlotSize - PX, y, PX, PX, rivet);
            DrawPixelRect(spriteBatch, x, y + SlotSize - PX, PX, PX, rivet);
            DrawPixelRect(spriteBatch, x + SlotSize - PX, y + SlotSize - PX, PX, PX, rivet);
        }
    }

    private void DrawPixelRect(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
