using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Entities;
using Dokryun.Systems;
using Dokryun.Dungeon;

namespace Dokryun.Scenes;

public enum GameState
{
    Playing,
    FloorTransition,
    Paused
}

public class GameplayScene : Scene
{
    // Core
    private Player _player;
    private List<Enemy> _enemies;
    private List<Enemy> _enemiesToAdd = new();
    private Camera _camera;
    private Texture2D _pixel;

    // Systems
    private ParticleSystem _particles;
    private DamageNumberSystem _damageNumbers;
    private ProjectileManager _projectiles;
    private AugmentStats _augmentStats;
    private List<DroppedItem> _droppedItems;
    private Inventory _inventory;

    // Dungeon
    private TileMap _tileMap;
    private TileTheme _tileTheme;
    private List<DungeonObject> _dungeonObjects;
    private int _floor;
    private int _totalKills;
    private int _floorKills;
    private int _floorEnemyCount;
    private bool _portalActive;

    // Stage system
    private StageData _currentStage;
    private int _stageIndex;
    private bool _isBossFloor;
    private Enemy _boss;
    private bool _bossDefeated;
    private List<BossPouch> _bossPouches = new();
    private List<Enemy> _bossClones = new();

    // Game state
    private GameState _state;
    private float _hitStopTimer;
    private float _gameTimer;
    private float _floorTransitionTimer;
    private int _attackCounter;

    // Combo system
    private int _comboCount;
    private float _comboTimer;
    private const float ComboWindow = 1.5f;

    // (removed: arrow rain timer)

    // Item pickup notification
    private string _itemPickupText;
    private float _itemPickupTimer;
    private Color _itemPickupColor;

    // Inventory UI
    private bool _inventoryOpen;
    private int _inventorySelectedSlot;
    private bool _inventoryDestroyConfirm;
    private bool _inventoryStatsTab; // false = items, true = stats
    private int _statsScrollOffset;

    // Visual
    private float _screenFlashTimer;
    private Color _screenFlashColor;
    private float _vignetteIntensity;

    // Ghost trail
    private List<(Vector2 pos, float alpha, bool flipX)> _ghostTrails = new();

    // DrawSlash (발도) state
    private bool _drawSlashReady;

    // AfterImage (잔영) delayed slashes
    private List<(Vector2 pos, float aimAngle, float damage, float range, float arc, float knockback, float timer)> _afterImageSlashes = new();

    // GroundCrack (균열) delayed explosions
    private List<(Vector2 pos, float damage, float timer, float maxTimer)> _groundCracks = new();

    // WindBurst (바람) active timer
    private float _windBurstActiveTimer;

    // Synergy notification
    private List<(string text, float timer, Color color)> _synergyNotifications = new();

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _player = new Player();
        if (Game1.SelectedClass == CharacterClass.Swordsman)
        {
            _player.IsSwordsman = true;
            _player.Attack = 10f;
            _player.LoadAnimations(Content.Load<Texture2D>("Sprites/Move"), Content.Load<Texture2D>("Sprites/Idle"), Content.Load<Texture2D>("Sprites/attack"));
        }
        _enemies = new List<Enemy>();
        _camera = new Camera(GraphicsDevice.Viewport) { Zoom = 1.5f };

        _particles = new ParticleSystem(3000);
        _damageNumbers = new DamageNumberSystem();
        _projectiles = new ProjectileManager();
        _augmentStats = new AugmentStats();
        _droppedItems = new List<DroppedItem>();
        _inventory = new Inventory();
        _dungeonObjects = new List<DungeonObject>();

        _floor = 0;
        _totalKills = 0;
        _stageIndex = 0;
        _currentStage = StageData.Stages[_stageIndex];

        // Generate tile theme
        _tileTheme = new TileTheme();
        _tileTheme.Generate(GraphicsDevice, _currentStage.Theme);

        _state = GameState.FloorTransition;
        _floorTransitionTimer = 2f;

        GenerateFloor();
    }

    private void GenerateFloor()
    {
        _floor++;
        var rng = new Random();

        _isBossFloor = _currentStage.IsBossFloor(_floor);
        _boss = null;
        _bossDefeated = false;

        _tileMap = DungeonGenerator.Generate(_floor, rng, _isBossFloor);
        _tileMap.Theme = _tileTheme;

        _player.Position = _tileMap.PlayerSpawn;
        _player.HP = Math.Min(_player.MaxHP + _augmentStats.MaxHPBonus,
            _player.HP + _player.MaxHP * 0.2f);

        _enemies.Clear();
        _projectiles.Clear();
        _dungeonObjects.Clear();
        _droppedItems.Clear();
        _bossPouches.Clear();
        _bossClones.Clear();

        _floorKills = 0;
        _portalActive = false;

        // Debug: boss skip portal near spawn (only on non-boss floors)
        if (!_isBossFloor)
        {
            var bossPortalPos = _tileMap.PlayerSpawn + new Vector2(40, 0);
            _dungeonObjects.Add(new DungeonObject
            {
                Position = bossPortalPos,
                Type = DungeonObjectType.BossPortal,
                InteractRadius = 30f
            });
        }

        if (_isBossFloor)
        {
            // Boss floor: spawn the boss
            if (_tileMap.BossSpawn.HasValue)
            {
                _boss = new Enemy(_currentStage.BossType, _tileMap.BossSpawn.Value);
                float bossScale = 1f + (_floor - 1) * 0.15f;
                _boss.MaxHP = (int)(_boss.MaxHP * bossScale);
                _boss.HP = _boss.MaxHP;
                _boss.Attack *= bossScale;
                _enemies.Add(_boss);
            }
            _floorEnemyCount = 1;
        }
        else
        {
            // Normal floor: spawn regular enemies
            _floorEnemyCount = _tileMap.EnemySpawns.Count;

            foreach (var spawnPos in _tileMap.EnemySpawns)
            {
                EnemyType type = RollEnemyType(_floor);
                var enemy = new Enemy(type, spawnPos);
                float scale = 1f + (_floor - 1) * 0.12f;
                enemy.MaxHP = (int)(enemy.MaxHP * scale);
                enemy.HP = enemy.MaxHP;
                enemy.Attack *= scale;
                _enemies.Add(enemy);
            }

            // Treasure chests
            foreach (var tpos in _tileMap.TreasurePositions)
            {
                _dungeonObjects.Add(new DungeonObject
                {
                    Position = tpos,
                    Type = DungeonObjectType.TreasureChest
                });
            }

            // Scatter pickups
            int pickupCount = 2 + _floor / 2;
            for (int i = 0; i < pickupCount; i++)
            {
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int tx = rng.Next(_tileMap.Width);
                    int ty = rng.Next(_tileMap.Height);
                    if (_tileMap.IsWalkable(tx, ty))
                    {
                        var wpos = _tileMap.TileToWorld(tx, ty);
                        if (Vector2.Distance(wpos, _tileMap.PlayerSpawn) > 200f)
                        {
                            _dungeonObjects.Add(new DungeonObject
                            {
                                Position = wpos,
                                Type = rng.NextDouble() < 0.6 ? DungeonObjectType.HealthPickup : DungeonObjectType.KiPickup
                            });
                            break;
                        }
                    }
                }
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _gameTimer += dt;

        if (_hitStopTimer > 0)
        {
            _hitStopTimer -= dt;
            _particles.Update(dt * 0.1f);
            return;
        }

        float effectiveDt = dt * _camera.TimeScale;

        if (_screenFlashTimer > 0) _screenFlashTimer -= dt;

        _particles.Update(effectiveDt);
        _damageNumbers.Update(effectiveDt);

        if (_comboTimer > 0)
        {
            _comboTimer -= dt;
            if (_comboTimer <= 0) _comboCount = 0;
        }

        // Ghost trail decay
        for (int i = _ghostTrails.Count - 1; i >= 0; i--)
        {
            var t = _ghostTrails[i];
            t.alpha -= dt * 5f;
            _ghostTrails[i] = t;
            if (t.alpha <= 0) _ghostTrails.RemoveAt(i);
        }

        // Update aim direction
        var mouseWorld = GetMouseWorldPosition();
        var aimDir = mouseWorld - _player.Position;
        if (aimDir.LengthSquared() > 0) aimDir.Normalize();
        _player.AimDirection = aimDir;

        switch (_state)
        {
            case GameState.FloorTransition:
                _floorTransitionTimer -= dt;
                if (_floorTransitionTimer <= 0)
                    _state = GameState.Playing;
                break;
            case GameState.Playing:
                UpdatePlaying(gameTime, effectiveDt);
                break;
        }

        _camera.Follow(_player.Position, dt);
        _camera.Update(dt);
    }

    private void UpdatePlaying(GameTime gameTime, float dt)
    {
        // Inventory toggle
        if (InputManager.IsKeyPressed(Keys.I))
        {
            _inventoryOpen = !_inventoryOpen;
            _inventorySelectedSlot = 0;
            _inventoryDestroyConfirm = false;
            _inventoryStatsTab = false;
            _statsScrollOffset = 0;
        }

        if (_inventoryOpen)
        {
            UpdateInventoryUI();
            return; // Pause gameplay while inventory is open
        }

        _player.Speed = 200f;
        _player.Update(gameTime);

        // Tile collision (swordsman: check around feet, offset +24 down)
        if (_player.IsSwordsman)
        {
            var feetPos = _player.Position + new Vector2(0, 24);
            var resolved = _tileMap.ResolveCollision(feetPos, 36, 38);
            _player.Position = resolved - new Vector2(0, 24);
        }
        else
            _player.Position = _tileMap.ResolveCollision(_player.Position, 24, 24);

        // Ghost trail
        if (_player.Velocity.LengthSquared() > 100)
        {
            float interval = 0.12f;
            if (_gameTimer % interval < dt)
                _ghostTrails.Add((_player.Position, 0.2f, _player.FacingLeft));
        }

        // Dash effects
        if (_player.IsDashing)
        {
            _drawSlashReady = _augmentStats.DrawSlash; // mark ready during dash
            AudioManager.Play("dash", 0.5f, 0.2f, 0.15f);
            var dashColor = new Color(180, 160, 130);
            _particles.EmitTrail(_player.Position, dashColor);
            if (_player.Velocity.LengthSquared() > 100)
                _particles.EmitSpeedLines(_player.Position, Vector2.Normalize(_player.Velocity), dashColor);
        }

        // WindBurst timer
        if (_windBurstActiveTimer > 0)
        {
            _windBurstActiveTimer -= dt;
            _augmentStats.WindBurstTimer = _windBurstActiveTimer;
        }

        // Player attack
        if (InputManager.IsLeftHeld() && _player.CanAttack())
        {
            float effectiveFireRate = _augmentStats.FireRateMultiplier;
            // Berserker resonance: 1.5x if below 40% HP
            if (_augmentStats.SynergyBerserkerResonance && _player.HP < (_player.MaxHP + _augmentStats.MaxHPBonus) * 0.4f)
                effectiveFireRate *= 1.5f;
            // WindBurst active: 2x attack speed
            if (_windBurstActiveTimer > 0)
                effectiveFireRate *= 2f;

            _player.OnAttack(effectiveFireRate);
            _attackCounter++;
            if (_player.IsSwordsman)
            {
                SwordSlash();
                // Combo sound: pitch gets lower per hit
                float comboPitch = _player.ComboStep switch { 1 => -0.2f, 2 => -0.4f, _ => -0.6f };
                AudioManager.Play("arrow_shoot", 0.6f, comboPitch, 0.05f);
            }
            else
            {
                ShootArrow();
                AudioManager.Play("arrow_shoot", 0.5f, 0.15f, 0.05f);
            }
        }

        // (Arrow Rain removed - swordsman only)

        // Update enemies with aggro
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive) continue;
            if (!enemy.IsDeathAnimating)
            {
                float dist = Vector2.Distance(enemy.Position, _player.Position);

                // Aggro check
                if (!enemy.IsAggro && dist < enemy.AggroRange)
                {
                    if (_tileMap.HasLineOfSight(enemy.Position, _player.Position))
                    {
                        enemy.IsAggro = true;
                        // Alert particle
                        _particles.EmitBurst(enemy.Position + new Vector2(0, -15), 4, new Color(255, 200, 50), 60f, 0.2f, 1.5f);
                    }
                }

                if (enemy.IsAggro)
                    UpdateEnemyAI(enemy, dt);
            }
            enemy.Update(gameTime);
            // Tile collision after knockback/movement
            enemy.Position = _tileMap.ResolveCollision(enemy.Position, 20, 20);
        }

        // Add any enemies spawned during update (e.g. boss summons)
        if (_enemiesToAdd.Count > 0)
        {
            _enemies.AddRange(_enemiesToAdd);
            _enemiesToAdd.Clear();
        }

        // Projectiles
        _projectiles.Update(dt);
        HandleProjectileCollisions();
        HandleProjectileWallCollisions();

        // AfterImage delayed slashes
        for (int i = _afterImageSlashes.Count - 1; i >= 0; i--)
        {
            var s = _afterImageSlashes[i];
            s.timer -= dt;
            _afterImageSlashes[i] = s;

            if (s.timer <= 0)
            {
                PerformGhostSlash(s.pos, s.aimAngle, s.damage * 0.5f, s.range, s.arc, s.knockback * 0.5f);
                _afterImageSlashes.RemoveAt(i);
            }
        }

        // GroundCrack delayed explosions
        for (int i = _groundCracks.Count - 1; i >= 0; i--)
        {
            var c = _groundCracks[i];
            c.timer -= dt;
            _groundCracks[i] = c;

            float progress = 1f - (c.timer / c.maxTimer);
            if (_gameTimer % 0.1f < dt)
            {
                _particles.EmitBurst(c.pos, 3, new Color(180, 120, 80) * (0.3f + progress * 0.7f), 30f + progress * 40f, 0.15f, 2f);
            }

            if (c.timer <= 0)
            {
                float synergyMul = _augmentStats.SynergyFocusResonance ? 2f : 1f;
                PerformExplosion(c.pos, c.damage * synergyMul, 60f, new Color(180, 120, 80));
                _camera.Shake(4f, 0.12f);
                AudioManager.Play("explosion", 0.6f, 0.1f);
                _groundCracks.RemoveAt(i);
            }
        }

        // Synergy notifications decay
        for (int i = _synergyNotifications.Count - 1; i >= 0; i--)
        {
            var n = _synergyNotifications[i];
            n.timer -= dt;
            _synergyNotifications[i] = n;
            if (n.timer <= 0) _synergyNotifications.RemoveAt(i);
        }

        // Enemy-player collision (GhostFire)
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive || enemy.IsDead || enemy.IsDeathAnimating) continue;
            float dist = Vector2.Distance(enemy.Position, _player.Position);

            if (enemy.Type == EnemyType.GhostFire)
            {
                if (dist < 20)
                {
                    var kbDir = _player.Position - enemy.Position;
                    TryDamagePlayer(enemy.Attack, kbDir, 200f);
                    _particles.EmitExplosion(enemy.Position, 25, Color.Cyan);
                    _particles.EmitImpactRing(enemy.Position, new Color(100, 220, 255), 35f);
                    enemy.TakeDamage(999);
                    FlashScreen(new Color(100, 200, 255), 0.12f);
                    _hitStopTimer = 0.04f;
                    AudioManager.Play("player_hit", 0.8f);
                    _camera.Shake(3f, 0.1f);
                }
            }
        }

        // Boss pouches (도깨비 주머니)
        for (int i = _bossPouches.Count - 1; i >= 0; i--)
        {
            var pouch = _bossPouches[i];
            bool wasExploded = pouch.HasExploded;
            pouch.Update(dt);
            // Just exploded this frame
            if (pouch.HasExploded && !wasExploded)
            {
                AudioManager.Play("explosion", 0.7f, 0.1f);
                _camera.Shake(4f, 0.12f);
                _particles.EmitExplosion(pouch.Position, 20, new Color(255, 160, 40));
                _particles.EmitImpactRing(pouch.Position, new Color(255, 100, 30), 40f, 16);
                // Cross arm particles
                for (int arm = 0; arm < 4; arm++)
                {
                    var armDir = arm switch { 0 => Vector2.UnitX, 1 => -Vector2.UnitX, 2 => Vector2.UnitY, _ => -Vector2.UnitY };
                    for (int s = 1; s <= 4; s++)
                        _particles.EmitBurst(pouch.Position + armDir * s * 30f, 5, new Color(255, 120, 40), 80f, 0.2f, 2f);
                }
            }
            // Damage player
            if (pouch.HasExploded && pouch.IsInCrossExplosion(_player.Position))
            {
                TryDamagePlayer(pouch.Damage);
                FlashScreen(new Color(255, 100, 30), 0.1f);
                _hitStopTimer = 0.05f;
            }
            if (!pouch.IsActive) _bossPouches.RemoveAt(i);
        }

        // Boss clone cleanup
        for (int i = _bossClones.Count - 1; i >= 0; i--)
        {
            if (_bossClones[i].IsDead || !_bossClones[i].IsActive)
                _bossClones.RemoveAt(i);
        }

        // Remove dead enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var e = _enemies[i];
            if (e.IsDead && e.IsActive && !e.IsDeathAnimating)
            {
                e.StartDeathAnimation();
                _floorKills++;
                _totalKills++;
                _comboCount++;
                _comboTimer = ComboWindow;

                Color deathColor = e.Type switch
                {
                    EnemyType.Soldier => new Color(200, 80, 60),
                    EnemyType.Archer => new Color(255, 180, 80),
                    EnemyType.Warrior => new Color(180, 50, 50),
                    EnemyType.GhostFire => new Color(80, 200, 220),
                    EnemyType.Spearman => new Color(160, 130, 70),
                    EnemyType.ShieldBearer => new Color(140, 130, 100),
                    EnemyType.Assassin => new Color(120, 80, 120),
                    EnemyType.Shaman => new Color(100, 180, 160),
                    EnemyType.FireArcher => new Color(255, 130, 50),
                    EnemyType.PoisonThrower => new Color(100, 200, 80),
                    EnemyType.DarkKnight => new Color(80, 60, 100),
                    EnemyType.Summoner => new Color(160, 100, 220),
                    EnemyType.BladeDancer => new Color(200, 70, 100),
                    EnemyType.ThunderMonk => new Color(100, 100, 220),
                    _ => Color.Red
                };

                int particleCount = 25 + Math.Min(_comboCount * 3, 30);
                _particles.EmitExplosion(e.Position, particleCount, deathColor);
                AudioManager.Play("enemy_die", 0.6f, 0.2f, 0.02f);
                _camera.Shake(1.5f, 0.06f);

                if (_comboCount >= 3)
                {
                    _particles.EmitImpactRing(e.Position, deathColor, 40f);
                    AudioManager.Play("combo", 0.4f + Math.Min(_comboCount * 0.05f, 0.3f), 0f, 0.05f);
                    _camera.ImpactZoom(0.02f);
                }

                // Regular enemies only drop health/ki
                if (Random.Shared.NextDouble() < 0.15)
                {
                    _dungeonObjects.Add(new DungeonObject
                    {
                        Position = e.Position,
                        Type = Random.Shared.NextDouble() < 0.5 ? DungeonObjectType.HealthPickup : DungeonObjectType.KiPickup
                    });
                }
            }

            if (e.IsDeathAnimating && e.DeathTimer <= 0)
            {
                e.IsActive = false;
                _enemies.RemoveAt(i);
            }
        }

        // Boss defeat check
        if (_isBossFloor && _boss != null && _boss.IsDead && !_bossDefeated)
        {
            _bossDefeated = true;
            _particles.EmitExplosion(_boss.Position, 60, new Color(50, 200, 80));
            _particles.EmitImpactRing(_boss.Position, new Color(255, 220, 100), 80f, 30);
            FlashScreen(new Color(255, 220, 100), 0.3f);
            _hitStopTimer = 0.15f;
            AudioManager.Play("boss_roar", 1f, 0f);
            AudioManager.Play("explosion", 0.9f, -0.1f);
            _camera.Shake(8f, 0.25f);
            _camera.ImpactZoom(0.06f);
            _camera.SlowMotion(0.3f, 0.4f);

            // Boss drops 2-3 meteorites (higher rarity)
            int bossDropCount = 2 + (Random.Shared.NextDouble() < 0.4 ? 1 : 0);
            for (int bd = 0; bd < bossDropCount; bd++)
                _droppedItems.Add(DroppedItem.Create(_boss.Position, _floor, true));
        }

        // Portal activation
        if (!_portalActive)
        {
            bool shouldActivate = _isBossFloor
                ? _bossDefeated
                : (_floorEnemyCount > 0 && (float)_floorKills / _floorEnemyCount >= 0.5f);

            if (shouldActivate)
            {
                _portalActive = true;
                var (px, py) = _tileMap.WorldToTile(_tileMap.PortalPosition);
                _tileMap.SetTile(px, py, TileType.Portal);
                FlashScreen(new Color(80, 200, 220), 0.15f);
                _particles.EmitExplosion(_tileMap.PortalPosition, 30, new Color(80, 200, 220));
                _particles.EmitImpactRing(_tileMap.PortalPosition, new Color(80, 200, 220), 50f, 20);
                AudioManager.Play("portal", 0.7f, 0f);
                _itemPickupText = "포탈이 열렸다! (미니맵 확인)";
                _itemPickupTimer = 3f;
                _itemPickupColor = new Color(80, 200, 220);
            }
        }

        // Portal interaction - go directly to next floor
        if (_portalActive && Vector2.Distance(_player.Position, _tileMap.PortalPosition) < 30f)
        {
            if (InputManager.IsKeyPressed(Keys.E))
            {
                _state = GameState.FloorTransition;
                _floorTransitionTimer = 1.5f;
                GenerateFloor();
            }
        }

        // Dungeon objects interaction
        foreach (var obj in _dungeonObjects)
        {
            if (!obj.IsActive) continue;
            obj.Update(dt);

            if (!obj.IsPlayerNear(_player.Position)) continue;

            switch (obj.Type)
            {
                case DungeonObjectType.TreasureChest:
                    if (!obj.IsOpened && InputManager.IsKeyPressed(Keys.E))
                    {
                        obj.IsOpened = true;
                        _particles.EmitExplosion(obj.Position, 15, new Color(200, 170, 50));
                        FlashScreen(new Color(255, 220, 100), 0.1f);
                        AudioManager.Play("pickup", 0.7f, -0.1f);
                        // Treasure drops 1-2 items
                        int itemCount = 1 + (Random.Shared.NextDouble() < 0.3 ? 1 : 0);
                        for (int ic = 0; ic < itemCount; ic++)
                            _droppedItems.Add(DroppedItem.Create(obj.Position, _floor, false));
                    }
                    break;
                case DungeonObjectType.HealthPickup:
                    _player.HP = Math.Min(_player.MaxHP + _augmentStats.MaxHPBonus, _player.HP + 20f);
                    _particles.EmitBurst(obj.Position, 8, new Color(220, 50, 50), 80f, 0.3f, 1.5f);
                    obj.IsActive = false;
                    AudioManager.Play("pickup", 0.5f, 0.1f);
                    break;
                case DungeonObjectType.KiPickup:
                    _player.Ki = Math.Min(_player.MaxKi, _player.Ki + 15f);
                    _particles.EmitBurst(obj.Position, 8, new Color(50, 70, 200), 80f, 0.3f, 1.5f);
                    obj.IsActive = false;
                    AudioManager.Play("pickup", 0.5f, 0.2f);
                    break;
                case DungeonObjectType.BossPortal:
                    if (InputManager.IsKeyPressed(Keys.E))
                    {
                        obj.IsActive = false;
                        _particles.EmitExplosion(obj.Position, 20, new Color(220, 40, 40));
                        FlashScreen(new Color(200, 50, 50), 0.2f);
                        AudioManager.Play("portal", 0.8f, -0.2f);
                        // Jump directly to boss floor
                        _floor = _currentStage.FloorCount - 1;
                        _state = GameState.FloorTransition;
                        _floorTransitionTimer = 1.5f;
                        GenerateFloor();
                        return;
                    }
                    break;
            }
        }

        // Update dropped items
        for (int i = _droppedItems.Count - 1; i >= 0; i--)
        {
            var item = _droppedItems[i];
            item.Update(dt);
            if (item.IsPlayerNear(_player.Position))
            {
                if (_inventory.TryAdd(item.MeteoriteId))
                {
                    // Track old synergy flags before recalculation
                    bool wasFocus = _augmentStats.SynergyFocusResonance;
                    bool wasBerserker = _augmentStats.SynergyBerserkerResonance;

                    _inventory.RecalculateStats(_augmentStats);

                    // Check for newly activated synergies
                    if (!wasFocus && _augmentStats.SynergyFocusResonance)
                    {
                        _synergyNotifications.Add(("[Synergy] Focus Resonance Activated!", 3f, new Color(200, 160, 255)));
                        FlashScreen(new Color(200, 160, 255), 0.15f);
                        _particles.EmitImpactRing(_player.Position, new Color(200, 160, 255), 60f, 20);
                    }
                    if (!wasBerserker && _augmentStats.SynergyBerserkerResonance)
                    {
                        _synergyNotifications.Add(("[Synergy] Berserker Resonance Activated!", 3f, new Color(255, 100, 100)));
                        FlashScreen(new Color(255, 100, 100), 0.15f);
                        _particles.EmitImpactRing(_player.Position, new Color(255, 100, 100), 60f, 20);
                    }

                    _particles.EmitBurst(item.Position, 15, item.MainColor, 150f, 0.3f, 2f);
                    _particles.EmitImpactRing(item.Position, item.GlowColor, 30f, 12);
                    FlashScreen(item.MainColor, 0.1f);
                    AudioManager.Play("pickup", 0.8f, -0.15f);
                    var rarityName = MeteoriteDatabase.RarityName(item.Info.Rarity);
                    _itemPickupText = $"[{rarityName}] {item.Name} - {item.Description}";
                    _itemPickupTimer = 2f;
                    _itemPickupColor = item.MainColor;
                    item.IsActive = false;
                    _droppedItems.RemoveAt(i);
                }
                else
                {
                    // Show appropriate message
                    if (_inventory.Has(item.MeteoriteId) && !item.Info.Stackable)
                    {
                        _itemPickupText = "이미 보유 중인 운석입니다!";
                    }
                    else
                    {
                        _itemPickupText = "인벤토리가 가득 찼습니다! (I키로 관리)";
                    }
                    _itemPickupTimer = 1.5f;
                    _itemPickupColor = new Color(255, 100, 100);
                }
            }
        }

        // Item pickup notification timer
        if (_itemPickupTimer > 0)
            _itemPickupTimer -= dt;

        // Player death
        if (_player.IsDead)
        {
            _particles.EmitExplosion(_player.Position, 50, new Color(200, 160, 80));
            _particles.EmitImpactRing(_player.Position, new Color(255, 200, 100), 60f);
            AudioManager.Play("player_hit", 1f, -0.2f);
            AudioManager.Play("explosion", 0.6f, 0.1f);
            SceneManager.ChangeScene(new DeathScene(_floor, _totalKills));
        }

        float hpRatio = _player.HP / _player.MaxHP;
        _vignetteIntensity = hpRatio < 0.3f ? (1f - hpRatio / 0.3f) * 0.5f : 0f;

        // Ambient particles: falling leaves
        if (_gameTimer % 0.3f < dt)
        {
            float ox = (float)(Random.Shared.NextDouble() * 400 - 200);
            float oy = (float)(Random.Shared.NextDouble() * 300 - 250);
            var leafPos = _player.Position + new Vector2(ox, oy);
            if (_tileMap.IsWalkableWorld(leafPos))
            {
                Color[] leafColors = { new Color(130, 90, 35), new Color(110, 70, 25), new Color(140, 100, 30), new Color(80, 110, 40) };
                var lc = leafColors[Random.Shared.Next(leafColors.Length)];
                float windX = 15f + (float)Random.Shared.NextDouble() * 20f;
                _particles.Emit(leafPos, new Vector2(windX, 20f + (float)Random.Shared.NextDouble() * 10f),
                    lc * 0.6f, 1.5f + (float)Random.Shared.NextDouble() * 1f, 1.5f + (float)Random.Shared.NextDouble() * 1f);
            }
        }
    }

    private void TryDamagePlayer(float rawDmg, Vector2? knockbackDir = null, float knockbackForce = 0f)
    {
        if (_player.IsInvincible) return;

        // Evasion check
        if (_augmentStats.EvasionBonus > 0 && Random.Shared.NextDouble() < _augmentStats.EvasionBonus)
        {
            _damageNumbers.SpawnText(_player.Position, "MISS", new Color(180, 180, 180));
            return;
        }

        // Apply defense (flat reduction)
        float dmg = Math.Max(1, rawDmg - _augmentStats.Defense);
        _player.TakeDamage(dmg);

        if (knockbackDir.HasValue && knockbackForce > 0)
            _player.ApplyKnockback(knockbackDir.Value, knockbackForce);
    }

    private void UpdateEnemyAI(Enemy enemy, float dt)
    {
        float dist = Vector2.Distance(enemy.Position, _player.Position);
        var toPlayer = _player.Position - enemy.Position;

        switch (enemy.Type)
        {
            case EnemyType.Soldier:
                enemy.MoveToward(_player.Position, dt);
                if (dist < enemy.AttackRange + 20 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.4f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    if (dist < enemy.AttackRange + 30)
                    {
                        _projectiles.SpawnMeleeStrike(enemy.Position, enemy.TelegraphDirection, enemy.Attack);
                        _particles.EmitDirectionalSpark(enemy.Position, toPlayer, 4, new Color(255, 100, 60));
                    }
                }
                break;

            case EnemyType.Archer:
                if (dist < 120)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.3f, dt);
                else if (dist > 200)
                    enemy.MoveToward(_player.Position, dt);

                if (enemy.CanAttack() && dist < 280 && _tileMap.HasLineOfSight(enemy.Position, _player.Position))
                {
                    enemy.StartTelegraph(toPlayer, 0.6f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    _projectiles.SpawnArrow(enemy.Position, _player.Position, enemy.Attack);
                    _particles.EmitBurst(enemy.Position, 6, new Color(255, 180, 60), 100f, 0.12f, 1.5f);
                }
                break;

            case EnemyType.Warrior:
                if (dist > 60)
                    enemy.MoveToward(_player.Position, dt);

                if (dist < enemy.AttackRange + 25 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.35f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    if (toPlayer.LengthSquared() > 0)
                    {
                        var lungeDir = Vector2.Normalize(toPlayer);
                        enemy.Position += lungeDir * 30f;
                        _projectiles.SpawnMeleeStrike(enemy.Position, lungeDir, enemy.Attack * 1.5f);
                        _particles.EmitDirectionalSpark(enemy.Position, toPlayer, 8, new Color(200, 40, 40));
                        _particles.EmitImpactRing(enemy.Position, new Color(200, 60, 40), 20f, 10);
                    }
                }
                break;

            case EnemyType.GhostFire:
                float speedMult = 1f + (1f - Math.Max(0, enemy.HP / enemy.MaxHP)) * 0.5f;
                var dir = _player.Position - enemy.Position;
                if (dir.LengthSquared() > 1f)
                {
                    dir.Normalize();
                    enemy.Position += dir * enemy.Speed * speedMult * enemy.SlowMultiplier * dt;
                }
                if (_gameTimer % 0.06f < dt)
                {
                    _particles.Emit(enemy.Position, new Vector2(0, -25) + dir * -20f,
                        new Color(80, 200, 220) * 0.7f, 0.35f, 2.5f);
                }
                break;

            case EnemyType.Spearman:
                enemy.MoveToward(_player.Position, dt);
                if (dist < enemy.AttackRange + 15 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.5f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    if (dist < enemy.AttackRange + 25)
                    {
                        _projectiles.SpawnMeleeStrike(enemy.Position, enemy.TelegraphDirection, enemy.Attack);
                        _particles.EmitDirectionalSpark(enemy.Position, toPlayer, 5, new Color(140, 120, 80));
                    }
                }
                break;

            case EnemyType.ShieldBearer:
                enemy.MoveToward(_player.Position, dt);
                if (dist < enemy.AttackRange + 15 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.6f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    if (dist < enemy.AttackRange + 20)
                    {
                        _projectiles.SpawnMeleeStrike(enemy.Position, enemy.TelegraphDirection, enemy.Attack);
                        _particles.EmitBurst(enemy.Position, 4, new Color(120, 110, 80), 60f, 0.15f, 1f);
                    }
                }
                break;

            case EnemyType.Assassin:
                // Fast approach, quick attack, retreat
                if (dist > 50)
                    enemy.MoveToward(_player.Position, dt);
                else if (dist < 30 && !enemy.IsTelegraphing)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.5f, dt);

                if (dist < enemy.AttackRange + 15 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.2f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.03f && enemy.TelegraphTimer > 0)
                {
                    if (toPlayer.LengthSquared() > 0)
                    {
                        var lungeDir = Vector2.Normalize(toPlayer);
                        enemy.Position += lungeDir * 25f;
                        _projectiles.SpawnMeleeStrike(enemy.Position, lungeDir, enemy.Attack);
                        _particles.EmitDirectionalSpark(enemy.Position, toPlayer, 6, new Color(120, 80, 120));
                    }
                }
                break;

            case EnemyType.Shaman:
                // Keep distance, ranged attack (slow projectile)
                if (dist < 100)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.3f, dt);
                else if (dist > 200)
                    enemy.MoveToward(_player.Position, dt);

                if (enemy.CanAttack() && dist < 250 && _tileMap.HasLineOfSight(enemy.Position, _player.Position))
                {
                    enemy.StartTelegraph(toPlayer, 0.8f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    _projectiles.SpawnArrow(enemy.Position, _player.Position, enemy.Attack);
                    _particles.EmitBurst(enemy.Position, 8, new Color(120, 200, 180), 80f, 0.2f, 1.5f);
                }
                break;

            case EnemyType.FireArcher:
                if (dist < 130)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.3f, dt);
                else if (dist > 220)
                    enemy.MoveToward(_player.Position, dt);

                if (enemy.CanAttack() && dist < 300 && _tileMap.HasLineOfSight(enemy.Position, _player.Position))
                {
                    enemy.StartTelegraph(toPlayer, 0.5f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    _projectiles.SpawnArrow(enemy.Position, _player.Position, enemy.Attack);
                    _particles.EmitBurst(enemy.Position, 6, new Color(255, 120, 30), 100f, 0.15f, 1.5f);
                }
                break;

            case EnemyType.PoisonThrower:
                if (dist < 80)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.4f, dt);
                else if (dist > 160)
                    enemy.MoveToward(_player.Position, dt);

                if (enemy.CanAttack() && dist < 200 && _tileMap.HasLineOfSight(enemy.Position, _player.Position))
                {
                    enemy.StartTelegraph(toPlayer, 0.7f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    _projectiles.SpawnArrow(enemy.Position, _player.Position, enemy.Attack);
                    _particles.EmitBurst(enemy.Position, 8, new Color(80, 200, 60), 80f, 0.2f, 2f);
                }
                break;

            case EnemyType.DarkKnight:
                if (dist > 50)
                    enemy.MoveToward(_player.Position, dt);

                if (dist < enemy.AttackRange + 20 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.4f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    if (toPlayer.LengthSquared() > 0)
                    {
                        var lungeDir = Vector2.Normalize(toPlayer);
                        enemy.Position += lungeDir * 20f;
                        _projectiles.SpawnMeleeStrike(enemy.Position, lungeDir, enemy.Attack * 1.3f);
                        _particles.EmitDirectionalSpark(enemy.Position, toPlayer, 10, new Color(80, 60, 100));
                        _particles.EmitImpactRing(enemy.Position, new Color(60, 40, 80), 25f, 8);
                    }
                }
                break;

            case EnemyType.Summoner:
                // Keep far, ranged attack
                if (dist < 150)
                    enemy.MoveToward(enemy.Position + (enemy.Position - _player.Position) * 0.3f, dt);
                else if (dist > 280)
                    enemy.MoveToward(_player.Position, dt);

                if (enemy.CanAttack() && dist < 320)
                {
                    enemy.StartTelegraph(toPlayer, 1.0f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    _projectiles.SpawnArrow(enemy.Position, _player.Position, enemy.Attack);
                    _particles.EmitExplosion(enemy.Position, 10, new Color(160, 100, 220));
                }
                break;

            case EnemyType.BladeDancer:
                enemy.MoveToward(_player.Position, dt);
                if (dist < enemy.AttackRange + 20 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.25f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.03f && enemy.TelegraphTimer > 0)
                {
                    // AoE spin attack - hit in all directions
                    for (int a = 0; a < 4; a++)
                    {
                        float angle = a * MathF.PI / 2f;
                        var spinDir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                        _projectiles.SpawnMeleeStrike(enemy.Position, spinDir, enemy.Attack);
                    }
                    _particles.EmitImpactRing(enemy.Position, new Color(200, 60, 100), 30f, 12);
                }
                break;

            case EnemyType.ThunderMonk:
                if (dist > 80)
                    enemy.MoveToward(_player.Position, dt);

                if (dist < enemy.AttackRange + 20 && enemy.CanAttack())
                {
                    enemy.StartTelegraph(toPlayer, 0.8f);
                    enemy.OnAttack();
                }
                if (enemy.IsTelegraphing && enemy.TelegraphTimer < 0.05f && enemy.TelegraphTimer > 0)
                {
                    // Lightning AoE
                    if (dist < enemy.AttackRange + 40)
                    {
                        _projectiles.SpawnMeleeStrike(enemy.Position, toPlayer.LengthSquared() > 0 ? Vector2.Normalize(toPlayer) : Vector2.UnitX, enemy.Attack);
                        _particles.EmitExplosion(enemy.Position, 15, new Color(130, 130, 255));
                        _particles.EmitImpactRing(enemy.Position, new Color(100, 100, 255), 40f, 16);
                    }
                }
                if (_gameTimer % 0.15f < dt)
                {
                    float lx = enemy.Position.X + Random.Shared.Next(-15, 15);
                    float ly = enemy.Position.Y + Random.Shared.Next(-15, 15);
                    _particles.Emit(new Vector2(lx, ly), Vector2.Zero, new Color(130, 130, 255) * 0.5f, 0.2f, 1.5f);
                }
                break;

            case EnemyType.DokkaebiKing:
                if (enemy == _boss)
                    UpdateBossAI(enemy, dt, dist, toPlayer);
                else
                    UpdateCloneBossAI(enemy, dt, dist, toPlayer);
                break;
        }
    }

    private void UpdateBossAI(Enemy boss, float dt, float dist, Vector2 toPlayer)
    {
        var rng = Random.Shared;

        // Phase-dependent speed boost
        if (boss.BossPhase >= 2)
            boss.Speed = boss.BaseSpeed * 1.5f;
        else if (boss.BossPhase >= 1)
            boss.Speed = boss.BaseSpeed * 1.2f;

        // Boss aura particles
        if (_gameTimer % 0.08f < dt)
        {
            float aAngle = (float)rng.NextDouble() * MathHelper.TwoPi;
            var auraPos = boss.Position + new Vector2(MathF.Cos(aAngle), MathF.Sin(aAngle)) * 25f;
            var auraColor = boss.BossPhase >= 2 ? new Color(255, 80, 40) : new Color(80, 200, 80);
            _particles.Emit(auraPos, new Vector2(0, -30), auraColor * 0.5f, 0.3f, 2f);
        }

        // State machine
        switch (boss.BossState)
        {
            case BossAttackState.Idle:
                UpdateBossIdle(boss, dt, dist, toPlayer, rng);
                break;

            // === 1. 도깨비 주머니 (공중 점프 → 주머니 투척) ===
            case BossAttackState.PouchJump:
                UpdateBossPouchJump(boss, dt);
                break;
            case BossAttackState.PouchThrowing:
                UpdateBossPouchThrowing(boss, dt, rng);
                break;
            case BossAttackState.PouchLanding:
                UpdateBossPouchLanding(boss, dt);
                break;

            // === 2. 방망이 돌진 ===
            case BossAttackState.ClubCharge:
                UpdateBossClubCharge(boss, dt, toPlayer);
                break;
            case BossAttackState.ClubCharging:
                UpdateBossClubCharging(boss, dt);
                break;

            // === 3. 회전 공격 ===
            case BossAttackState.SpinAttack:
                UpdateBossSpinStart(boss, dt);
                break;
            case BossAttackState.Spinning:
                UpdateBossSpinning(boss, dt);
                break;

            // === 4. 연속 지진 밟기 ===
            case BossAttackState.Stomp:
                UpdateBossStompStart(boss, dt);
                break;
            case BossAttackState.Stomping:
                UpdateBossStomping(boss, dt, rng);
                break;

            // === 5. 포효 흡인 ===
            case BossAttackState.RoarPull:
                UpdateBossRoarStart(boss, dt);
                break;
            case BossAttackState.Roaring:
                UpdateBossRoaring(boss, dt);
                break;

            // === 6. 분신 ===
            case BossAttackState.ShadowClone:
                UpdateBossShadowClone(boss, dt, rng);
                break;

            // === 궁극기: 불비 ===
            case BossAttackState.UltFireRain:
                UpdateBossUltFireRainStart(boss, dt);
                break;
            case BossAttackState.UltFireRaining:
                UpdateBossUltFireRaining(boss, dt, rng);
                break;

            // === 궁극기: 거대 운석 ===
            case BossAttackState.UltMeteor:
                UpdateBossUltMeteorStart(boss, dt);
                break;
            case BossAttackState.UltMeteorFall:
                UpdateBossUltMeteorFall(boss, dt);
                break;

            // === 궁극기: 광폭화 ===
            case BossAttackState.UltBerserk:
                UpdateBossUltBerserkStart(boss, dt);
                break;
            case BossAttackState.UltBerserking:
                UpdateBossUltBerserking(boss, dt, rng);
                break;

            // === 궁극기: 암흑파 ===
            case BossAttackState.UltDarkWave:
                UpdateBossUltDarkWaveStart(boss, dt);
                break;
            case BossAttackState.UltDarkWaving:
                UpdateBossUltDarkWaving(boss, dt, rng);
                break;
        }
    }

    // --- Boss Idle: move toward player & pick next attack ---
    private void UpdateBossIdle(Enemy boss, float dt, float dist, Vector2 toPlayer, Random rng)
    {
        if (dist > 60)
            boss.MoveToward(_player.Position, dt);

        // Telegraph melee hit (must check before cooldown gate)
        if (boss.IsTelegraphing && boss.TelegraphTimer < 0.05f && boss.TelegraphTimer > 0)
        {
            _projectiles.SpawnMeleeStrike(boss.Position, boss.TelegraphDirection, boss.Attack);
            _particles.EmitExplosion(boss.Position + boss.TelegraphDirection * 30f, 15, new Color(100, 200, 80));
            _particles.EmitImpactRing(boss.Position, new Color(80, 160, 60), 50f, 16);
            AudioManager.Play("boss_stomp", 0.7f, 0.1f);
            _camera.DirectionalShake(boss.TelegraphDirection, 4f, 0.1f);
        }

        if (boss.BossAttackCooldown > 0) return;

        // Melee club smash if close
        if (dist < boss.AttackRange + 30)
        {
            float telegraphTime = boss.BossPhase >= 2 ? 0.35f : 0.6f;
            boss.StartTelegraph(toPlayer, telegraphTime);
            boss.OnAttack();
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 1.5f : 2.5f;
            return;
        }

        // Pick a special attack
        int maxAttacks = boss.BossPhase >= 2 ? 10 : (boss.BossPhase >= 1 ? 8 : 4);
        int pick = rng.Next(maxAttacks);
        // Avoid repeating same attack
        if (pick == boss.BossAttackIndex) pick = (pick + 1) % maxAttacks;
        boss.BossAttackIndex = pick;

        switch (pick)
        {
            case 0: // 도깨비 주머니
                boss.BossState = BossAttackState.PouchJump;
                boss.BossAirOrigin = boss.Position;
                boss.BossStateTimer = 0.5f;
                boss.BossPouchCount = boss.BossPhase >= 2 ? 10 : (boss.BossPhase >= 1 ? 7 : 4);
                _particles.EmitBurst(boss.Position, 10, new Color(50, 140, 70), 150f, 0.3f, 2f);
                break;
            case 1: // 방망이 돌진
                boss.BossState = BossAttackState.ClubCharge;
                boss.BossStateTimer = 0.6f;
                boss.BossChargeDir = toPlayer.LengthSquared() > 0 ? Vector2.Normalize(toPlayer) : Vector2.UnitX;
                break;
            case 2: // 도깨비불 소환
                BossSummonGhosts(boss, rng);
                boss.BossAttackCooldown = boss.BossPhase >= 2 ? 2.5f : 5f;
                break;
            case 3: // 방망이 투척 (부채꼴)
                BossClubThrow(boss, toPlayer);
                boss.BossAttackCooldown = boss.BossPhase >= 2 ? 2f : 4f;
                break;
            case 4: // 회전 공격
                boss.BossState = BossAttackState.SpinAttack;
                boss.BossStateTimer = 0.4f;
                boss.BossSpinAngle = 0;
                break;
            case 5: // 연속 지진 밟기
                boss.BossState = BossAttackState.Stomp;
                boss.BossStateTimer = 0.3f;
                boss.BossStompCount = boss.BossPhase >= 2 ? 8 : 4;
                break;
            case 6: // 포효 흡인 + 분신
                if (boss.BossPhase >= 2 && _bossClones.Count == 0)
                {
                    boss.BossState = BossAttackState.ShadowClone;
                    boss.BossStateTimer = 0.8f;
                }
                else
                {
                    boss.BossState = BossAttackState.RoarPull;
                    boss.BossStateTimer = 0.5f;
                }
                break;
            case 7: // 궁극기: 거대 운석
                boss.BossState = BossAttackState.UltMeteor;
                boss.BossStateTimer = 1.0f;
                boss.BossUltTarget = _player.Position;
                break;
            case 8: // 궁극기: 암흑파
                boss.BossState = BossAttackState.UltDarkWave;
                boss.BossStateTimer = 0.5f;
                boss.BossUltSubCount = 0;
                break;
            case 9: // 궁극기: 불비
                boss.BossState = BossAttackState.UltFireRain;
                boss.BossStateTimer = 0.6f;
                boss.BossUltSubCount = 0;
                break;
        }
    }

    // === 1. 도깨비 주머니 ===
    private void UpdateBossPouchJump(Enemy boss, float dt)
    {
        // Rising into the air (visual: move up)
        boss.Position = Vector2.Lerp(boss.Position, boss.BossAirOrigin + new Vector2(0, -80), dt * 4f);
        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.PouchThrowing;
            boss.BossStateTimer = 0.25f * boss.BossPouchCount;
        }
        _particles.EmitTrail(boss.Position, new Color(50, 140, 70));
    }

    private void UpdateBossPouchThrowing(Enemy boss, float dt, Random rng)
    {
        // Throw pouches at intervals
        float interval = 0.25f;
        float elapsed = interval * boss.BossPouchCount - boss.BossStateTimer;
        int thrown = (int)(elapsed / interval);

        if (thrown > 0 && _bossPouches.Count < thrown + (_bossPouches.Count - boss.BossPouchCount + boss.BossPouchCount))
        {
            // Throw one pouch each interval
            if ((int)((elapsed - dt) / interval) < thrown)
            {
                // Random position near player
                float ox = (float)(rng.NextDouble() * 200 - 100);
                float oy = (float)(rng.NextDouble() * 200 - 100);
                var targetPos = _player.Position + new Vector2(ox, oy);
                if (_tileMap.IsWalkableWorld(targetPos))
                {
                    float fuseTime = boss.BossPhase >= 2 ? 2f : 3f;
                    float crossLen = boss.BossPhase >= 2 ? 150f : 120f;
                    _bossPouches.Add(new BossPouch
                    {
                        Position = targetPos,
                        Timer = fuseTime,
                        MaxTimer = fuseTime,
                        Damage = boss.Attack * 1.2f,
                        CrossLength = crossLen,
                        CrossWidth = 22f
                    });
                    // Throw trail
                    _particles.EmitBurst(targetPos, 6, new Color(120, 80, 40), 60f, 0.2f, 1.5f);
                    _particles.EmitBurst(boss.Position, 3, new Color(200, 160, 60), 80f, 0.15f, 1.5f);
                }
            }
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.PouchLanding;
            boss.BossStateTimer = 0.4f;
        }
    }

    private void UpdateBossPouchLanding(Enemy boss, float dt)
    {
        // Fall back down
        boss.Position = Vector2.Lerp(boss.Position, boss.BossAirOrigin, dt * 6f);
        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 1.5f : 3f;
            // Landing shockwave
            _particles.EmitImpactRing(boss.Position, new Color(80, 160, 60), 35f, 12);
            AudioManager.Play("boss_stomp", 0.6f, 0.1f);
            _camera.Shake(3f, 0.08f);
            // Small AoE damage on landing
            if (Vector2.Distance(_player.Position, boss.Position) < 50f)
            {
                TryDamagePlayer(boss.Attack * 0.5f);
            }
        }
    }

    // === 2. 방망이 돌진 ===
    private void UpdateBossClubCharge(Enemy boss, float dt, Vector2 toPlayer)
    {
        // Telegraph: flash red, face player
        if (toPlayer.LengthSquared() > 0)
            boss.BossChargeDir = Vector2.Normalize(toPlayer);

        float warningPulse = MathF.Sin(boss.BossStateTimer * 20f);
        if (warningPulse > 0)
            _particles.EmitBurst(boss.Position + boss.BossChargeDir * 30f, 2, new Color(255, 50, 30), 40f, 0.1f, 2f);

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.ClubCharging;
            boss.BossStateTimer = boss.BossPhase >= 2 ? 0.8f : 0.6f;
            AudioManager.Play("boss_stomp", 0.8f, 0.05f);
            _camera.Shake(5f, 0.15f);
        }
    }

    private void UpdateBossClubCharging(Enemy boss, float dt)
    {
        // Rush forward swinging club
        float chargeSpeed = boss.BossPhase >= 2 ? 450f : 350f;
        boss.Position += boss.BossChargeDir * chargeSpeed * dt;
        boss.Position = _tileMap.ResolveCollision(boss.Position, 44, 48);

        // Continuous damage trail
        if (_gameTimer % 0.06f < dt)
        {
            _projectiles.SpawnMeleeStrike(boss.Position, boss.BossChargeDir, boss.Attack * 0.7f);
            _particles.EmitDirectionalSpark(boss.Position, boss.BossChargeDir, 4, new Color(130, 90, 40), 200f);
            _particles.EmitTrail(boss.Position, new Color(160, 110, 50));
        }

        // Hit player directly
        if (Vector2.Distance(_player.Position, boss.Position) < 40f)
        {
            var knockDir = _player.Position - boss.Position;
            if (knockDir.LengthSquared() > 0) knockDir = Vector2.Normalize(knockDir);
            TryDamagePlayer(boss.Attack * 1.3f, knockDir, 200f);
            _player.Position += knockDir * 60f;
            FlashScreen(new Color(255, 80, 40), 0.12f);
            _hitStopTimer = 0.06f;
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 1.5f : 2.5f;
            // End slam
            _particles.EmitExplosion(boss.Position, 12, new Color(130, 90, 40));
        }
    }

    // === 3. 회전 공격 (360도 방망이 회전) ===
    private void UpdateBossSpinStart(Enemy boss, float dt)
    {
        // Wind up
        _particles.EmitBurst(boss.Position, 3, new Color(50, 140, 70), 50f, 0.1f, 2f);
        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Spinning;
            boss.BossStateTimer = boss.BossPhase >= 2 ? 2.5f : 1.8f;
            boss.BossSpinAngle = 0;
        }
    }

    private void UpdateBossSpinning(Enemy boss, float dt)
    {
        // Spin and move toward player
        float spinSpeed = boss.BossPhase >= 2 ? 14f : 10f;
        boss.BossSpinAngle += spinSpeed * dt;

        // Move toward player slowly while spinning
        var toP = _player.Position - boss.Position;
        if (toP.LengthSquared() > 100)
        {
            if (toP.LengthSquared() > 0) toP = Vector2.Normalize(toP);
            boss.Position += toP * (boss.Speed * 1.3f) * dt;
            boss.Position = _tileMap.ResolveCollision(boss.Position, 44, 48);
        }

        // Emit projectiles in spinning pattern
        if (_gameTimer % 0.12f < dt)
        {
            var spinDir = new Vector2(MathF.Cos(boss.BossSpinAngle), MathF.Sin(boss.BossSpinAngle));
            var clubTip = boss.Position + spinDir * 45f;
            _projectiles.SpawnMeleeStrike(clubTip, spinDir, boss.Attack * 0.6f);
            _particles.EmitDirectionalSpark(clubTip, spinDir, 3, new Color(130, 90, 40), 150f);

            // Second arm (opposite side)
            var spinDir2 = -spinDir;
            var clubTip2 = boss.Position + spinDir2 * 45f;
            _projectiles.SpawnMeleeStrike(clubTip2, spinDir2, boss.Attack * 0.4f);
        }

        // Spin visual
        if (_gameTimer % 0.05f < dt)
        {
            var trailDir = new Vector2(MathF.Cos(boss.BossSpinAngle), MathF.Sin(boss.BossSpinAngle));
            _particles.EmitTrail(boss.Position + trailDir * 40f, new Color(160, 110, 50));
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 2f : 3.5f;
            // Dizzy particles
            _particles.EmitImpactRing(boss.Position, new Color(200, 200, 80), 30f, 12);
        }
    }

    // === 4. 연속 지진 밟기 ===
    private void UpdateBossStompStart(Enemy boss, float dt)
    {
        _particles.EmitBurst(boss.Position, 2, new Color(200, 100, 40), 30f, 0.1f, 3f);
        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Stomping;
            boss.BossStateTimer = 0.4f;
        }
    }

    private void UpdateBossStomping(Enemy boss, float dt, Random rng)
    {
        if (boss.BossStateTimer <= 0 && boss.BossStompCount > 0)
        {
            boss.BossStompCount--;
            boss.BossStateTimer = boss.BossPhase >= 2 ? 0.3f : 0.45f;

            // Teleport near player then stomp
            float ox = (float)(rng.NextDouble() * 80 - 40);
            float oy = (float)(rng.NextDouble() * 80 - 40);
            var stompTarget = _player.Position + new Vector2(ox, oy);
            if (_tileMap.IsWalkableWorld(stompTarget))
                boss.Position = stompTarget;

            // Shockwave
            float radius = boss.BossPhase >= 2 ? 70f : 55f;
            _particles.EmitImpactRing(boss.Position, new Color(180, 120, 40), radius, 16);
            _particles.EmitExplosion(boss.Position, 10, new Color(140, 90, 30));
            AudioManager.Play("boss_stomp", 0.75f, 0.15f, 0.08f);
            _camera.Shake(4f, 0.08f);

            // Ring damage
            if (Vector2.Distance(_player.Position, boss.Position) < radius)
            {
                TryDamagePlayer(boss.Attack * 0.8f);
                FlashScreen(new Color(180, 120, 40), 0.08f);
            }

            // Spawn crack projectiles outward
            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.TwoPi * i / 6f + (float)rng.NextDouble() * 0.3f;
                var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                _projectiles.Spawn(boss.Position + d * 15f, d * 180f, boss.Attack * 0.4f, false,
                    new Color(140, 100, 40), 6f, 0.6f);
            }
        }

        if (boss.BossStompCount <= 0 && boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 2f : 4f;
        }
    }

    // === 5. 포효 흡인 ===
    private void UpdateBossRoarStart(Enemy boss, float dt)
    {
        // Inhale particles
        for (int i = 0; i < 3; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            float radius = 100f + (float)Random.Shared.NextDouble() * 80f;
            var pPos = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            var vel = (boss.Position - pPos) * 3f;
            _particles.Emit(pPos, vel, new Color(200, 255, 200) * 0.5f, 0.3f, 2f);
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Roaring;
            boss.BossStateTimer = boss.BossPhase >= 2 ? 2.5f : 1.8f;
            FlashScreen(new Color(80, 200, 80), 0.15f);
        }
    }

    private void UpdateBossRoaring(Enemy boss, float dt)
    {
        // Pull player toward boss
        float pullForce = boss.BossPhase >= 2 ? 180f : 120f;
        var pullDir = boss.Position - _player.Position;
        float pullDist = pullDir.Length();
        if (pullDist > 10f && pullDist < 250f)
        {
            pullDir /= pullDist;
            _player.Position += pullDir * pullForce * dt;
            if (_player.IsSwordsman)
            {
                var feetPos2 = _player.Position + new Vector2(0, 24);
                var resolved2 = _tileMap.ResolveCollision(feetPos2, 36, 38);
                _player.Position = resolved2 - new Vector2(0, 24);
            }
            else
                _player.Position = _tileMap.ResolveCollision(_player.Position, 24, 24);
        }

        // Visual: suction lines
        if (_gameTimer % 0.08f < dt)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            float r = 80f + (float)Random.Shared.NextDouble() * 100f;
            var linePos = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
            _particles.Emit(linePos, (boss.Position - linePos) * 2.5f, new Color(150, 255, 150) * 0.4f, 0.25f, 1.5f);
        }

        // Damage if too close
        if (Vector2.Distance(_player.Position, boss.Position) < 45f && _gameTimer % 0.5f < dt)
        {
            TryDamagePlayer(boss.Attack * 0.6f);
        }

        // Periodically emit shockwave rings
        if (_gameTimer % 0.4f < dt)
        {
            _particles.EmitImpactRing(boss.Position, new Color(80, 200, 80), 60f, 16);
            for (int i = 0; i < 8; i++)
            {
                float a = MathHelper.TwoPi * i / 8f;
                var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
                _projectiles.Spawn(boss.Position + d * 20f, d * 150f, boss.Attack * 0.3f, false,
                    new Color(80, 200, 100), 5f, 0.8f);
            }
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = boss.BossPhase >= 2 ? 2f : 4f;
            // End roar explosion
            _particles.EmitExplosion(boss.Position, 25, new Color(100, 255, 100));
        }
    }

    // === 6. 분신 (Phase 2 only) ===
    private void UpdateBossShadowClone(Enemy boss, float dt, Random rng)
    {
        if (boss.BossStateTimer <= 0)
        {
            // Spawn 2 clones around boss
            int cloneCount = 2;
            for (int i = 0; i < cloneCount; i++)
            {
                float angle = MathHelper.TwoPi * i / cloneCount + (float)rng.NextDouble();
                var spawnPos = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 70f;
                if (_tileMap.IsWalkableWorld(spawnPos))
                {
                    var clone = new Enemy(EnemyType.DokkaebiKing, spawnPos);
                    clone.MaxHP = boss.MaxHP * 0.15f;
                    clone.HP = clone.MaxHP;
                    clone.Attack = boss.Attack * 0.5f;
                    clone.Speed = boss.Speed * 1.2f;
                    clone.IsAggro = true;
                    clone.BossPhase = 0; // clones don't have phases
                    _enemiesToAdd.Add(clone);
                    _bossClones.Add(clone);
                    _particles.EmitExplosion(spawnPos, 15, new Color(50, 140, 70));
                    _particles.EmitImpactRing(spawnPos, new Color(80, 200, 80), 30f, 12);
                }
            }

            FlashScreen(new Color(50, 180, 80), 0.15f);
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = 5f;
        }
        else
        {
            // Gathering energy visual
            float pulse = MathF.Sin(boss.BossStateTimer * 15f);
            if (pulse > 0)
                _particles.EmitBurst(boss.Position, 4, new Color(50, 180, 70), 60f, 0.15f, 2f);
        }
    }

    // ==========================================
    // === 궁극기 1: 불비 (Fire Rain) ===
    // 맵 전체에 불기둥이 내려오는 광역 공격
    // ==========================================
    private void UpdateBossUltFireRainStart(Enemy boss, float dt)
    {
        // Charge-up: boss glows red, warning particles
        for (int i = 0; i < 5; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            float r = 30f + (float)Random.Shared.NextDouble() * 50f;
            var p = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
            _particles.Emit(p, (boss.Position - p) * 3f, new Color(255, 100, 30) * 0.7f, 0.3f, 3f);
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.UltFireRaining;
            boss.BossStateTimer = 4f; // 4 seconds of fire rain
            boss.BossUltSubCount = 0;
            FlashScreen(new Color(255, 80, 30), 0.2f);
            _particles.EmitExplosion(boss.Position, 30, new Color(255, 100, 30));
            AudioManager.Play("boss_roar", 0.9f, 0.1f);
            AudioManager.Play("boss_phase", 0.6f, 0f);
            _camera.Shake(6f, 0.3f);
            _camera.ImpactZoom(0.04f);
        }
    }

    private void UpdateBossUltFireRaining(Enemy boss, float dt, Random rng)
    {
        // Every 0.15s spawn a fire pillar at a random position near player
        float interval = 0.15f;
        if (_gameTimer % interval < dt)
        {
            float ox = (float)(rng.NextDouble() * 300 - 150);
            float oy = (float)(rng.NextDouble() * 300 - 150);
            var target = _player.Position + new Vector2(ox, oy);

            // Warning marker
            _particles.EmitImpactRing(target, new Color(255, 100, 30) * 0.5f, 25f, 8);

            // Fire pillar projectile (falls from above)
            for (int i = 0; i < 3; i++)
            {
                float spread = (float)(rng.NextDouble() * 12 - 6);
                _projectiles.Spawn(target + new Vector2(spread, -200 + spread), new Vector2(0, 350f),
                    boss.Attack * 0.5f, false, new Color(255, 120 + rng.Next(80), 30), 8f, 0.7f);
            }

            boss.BossUltSubCount++;
        }

        // Boss aura during ultimate
        if (_gameTimer % 0.05f < dt)
        {
            float angle = (float)rng.NextDouble() * MathHelper.TwoPi;
            _particles.Emit(boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 20f,
                new Vector2(0, -80), new Color(255, 80, 20) * 0.6f, 0.3f, 3f);
        }

        // Screen tint effect
        if (_gameTimer % 0.3f < dt)
            FlashScreen(new Color(255, 60, 20), 0.04f);

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = 3f;
            _particles.EmitExplosion(boss.Position, 20, new Color(255, 150, 50));
        }
    }

    // ==========================================
    // === 궁극기 2: 거대 운석 (Meteor Strike) ===
    // 중앙으로 모이는 에너지 후 대폭발
    // ==========================================
    private void UpdateBossUltMeteorStart(Enemy boss, float dt)
    {
        // Boss teleports to center and charges energy
        var mapCenter = _tileMap.BossSpawn ?? boss.Position;
        boss.Position = Vector2.Lerp(boss.Position, mapCenter, dt * 3f);

        // Warning: expanding ring + energy gathering
        float chargeProgress = 1f - (boss.BossStateTimer / 1.0f);
        for (int i = 0; i < 6; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            float r = 120f * (1f - chargeProgress) + 20f;
            var p = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
            _particles.Emit(p, (boss.Position - p) * 4f, new Color(255, 200, 50) * 0.8f, 0.25f, 3f);
        }

        // Pulsing warning
        if (_gameTimer % 0.2f < dt)
            _particles.EmitImpactRing(boss.Position, new Color(255, 180, 50), 80f * chargeProgress + 20f, 16);

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.UltMeteorFall;
            boss.BossStateTimer = 0.5f;
            FlashScreen(new Color(255, 220, 100), 0.3f);
        }
    }

    private void UpdateBossUltMeteorFall(Enemy boss, float dt)
    {
        if (boss.BossStateTimer <= 0)
        {
            // MASSIVE explosion
            float radius = 130f;
            _particles.EmitExplosion(boss.Position, 60, new Color(255, 200, 50));
            _particles.EmitImpactRing(boss.Position, new Color(255, 180, 30), radius, 32);
            _particles.EmitImpactRing(boss.Position, new Color(255, 100, 20), radius * 0.7f, 24);
            _particles.EmitExplosion(boss.Position, 40, new Color(255, 100, 30));

            // Shockwave projectiles in all directions
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                _projectiles.Spawn(boss.Position + d * 25f, d * 200f, boss.Attack * 0.8f, false,
                    new Color(255, 160, 40), 10f, 1.2f);
            }

            // Damage everything in radius
            float dist = Vector2.Distance(_player.Position, boss.Position);
            if (dist < radius)
            {
                float falloff = 1f - (dist / radius) * 0.5f;
                var kbDir = _player.Position - boss.Position;
                TryDamagePlayer(boss.Attack * 2f * falloff, kbDir, 400f);
            }

            _hitStopTimer = 0.15f;
            FlashScreen(Color.White, 0.25f);
            AudioManager.Play("explosion", 1f, -0.15f);
            AudioManager.Play("boss_stomp", 0.9f, -0.2f);
            _camera.Shake(10f, 0.3f);
            _camera.ImpactZoom(0.06f);

            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = 3.5f;
        }
        else
        {
            // Charging flash
            float pulse = MathF.Sin(boss.BossStateTimer * 30f);
            if (pulse > 0)
                _particles.EmitBurst(boss.Position, 8, new Color(255, 200, 60), 100f, 0.1f, 4f);
        }
    }

    // ==========================================
    // === 궁극기 3: 광폭화 (Berserk) ===
    // 연속 돌진 + 회전 콤보 (Phase 2 전환 시 자동)
    // ==========================================
    private void UpdateBossUltBerserkStart(Enemy boss, float dt)
    {
        // Red flash charge-up
        for (int i = 0; i < 4; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            var p = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 35f;
            _particles.Emit(p, (boss.Position - p) * 3f, new Color(255, 30, 30) * 0.8f, 0.3f, 3f);
        }

        if (boss.BossStateTimer <= 0)
        {
            AudioManager.Play("boss_roar", 1f, -0.1f);
            AudioManager.Play("boss_phase", 0.7f, 0f);
            _camera.Shake(7f, 0.3f);
            _camera.ImpactZoom(0.05f);
            _camera.SlowMotion(0.4f, 0.25f);
            boss.BossState = BossAttackState.UltBerserking;
            boss.BossStateTimer = 0.5f;
            boss.BossUltSubCount = 0;
            FlashScreen(new Color(255, 30, 30), 0.25f);
            _particles.EmitExplosion(boss.Position, 25, new Color(255, 50, 30));
        }
    }

    private void UpdateBossUltBerserking(Enemy boss, float dt, Random rng)
    {
        int maxDashes = 5;

        if (boss.BossStateTimer <= 0 && boss.BossUltSubCount < maxDashes)
        {
            // Dash toward player
            var toP = _player.Position - boss.Position;
            if (toP.LengthSquared() > 0) toP = Vector2.Normalize(toP);
            boss.BossChargeDir = toP;
            boss.BossStateTimer = 0.35f;
            boss.BossUltSubCount++;

            _particles.EmitDirectionalSpark(boss.Position, toP, 8, new Color(255, 50, 30), 250f);
            FlashScreen(new Color(255, 50, 30), 0.06f);
        }

        if (boss.BossUltSubCount <= maxDashes && boss.BossStateTimer > 0)
        {
            // Rush movement
            float chargeSpeed = 500f;
            boss.Position += boss.BossChargeDir * chargeSpeed * dt;
            boss.Position = _tileMap.ResolveCollision(boss.Position, 44, 48);

            // Damage trail
            if (_gameTimer % 0.06f < dt)
            {
                _projectiles.SpawnMeleeStrike(boss.Position, boss.BossChargeDir, boss.Attack * 0.6f);
                _particles.EmitTrail(boss.Position, new Color(255, 50, 30));
            }

            // Hit player
            if (Vector2.Distance(_player.Position, boss.Position) < 40f)
            {
                var kbDir = _player.Position - boss.Position;
                TryDamagePlayer(boss.Attack * 1.2f, kbDir, 300f);
                _hitStopTimer = 0.05f;
            }
        }

        if (boss.BossUltSubCount >= maxDashes && boss.BossStateTimer <= 0)
        {
            // End with a spin attack
            boss.BossState = BossAttackState.SpinAttack;
            boss.BossStateTimer = 0.3f;
            boss.BossSpinAngle = 0;
            boss.BossAttackCooldown = 0;
            _particles.EmitExplosion(boss.Position, 20, new Color(255, 80, 40));
        }
    }

    // ==========================================
    // === 궁극기 4: 암흑파 (Dark Wave) ===
    // 전방위 파동을 연속으로 방출
    // ==========================================
    private void UpdateBossUltDarkWaveStart(Enemy boss, float dt)
    {
        // Dark energy gathering
        for (int i = 0; i < 3; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathHelper.TwoPi;
            float r = 60f + (float)Random.Shared.NextDouble() * 40f;
            var p = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
            _particles.Emit(p, (boss.Position - p) * 2.5f, new Color(80, 30, 120) * 0.7f, 0.3f, 2.5f);
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.UltDarkWaving;
            boss.BossStateTimer = 3.5f;
            boss.BossUltSubCount = 0;
            FlashScreen(new Color(60, 20, 100), 0.2f);
            AudioManager.Play("boss_roar", 0.8f, -0.15f);
            _camera.Shake(5f, 0.2f);
            _camera.ImpactZoom(0.03f);
        }
    }

    private void UpdateBossUltDarkWaving(Enemy boss, float dt, Random rng)
    {
        // Emit wave rings every 0.5s
        float interval = 0.5f;
        if (_gameTimer % interval < dt && boss.BossUltSubCount < 7)
        {
            boss.BossUltSubCount++;

            // Ring of projectiles
            int count = 12 + boss.BossUltSubCount * 2;
            float angleOffset = boss.BossUltSubCount * 0.3f; // Each wave rotated slightly
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + angleOffset;
                var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                float speed = 160f + boss.BossUltSubCount * 15f;
                _projectiles.Spawn(boss.Position + d * 20f, d * speed, boss.Attack * 0.5f, false,
                    new Color(100, 40, 160), 7f, 1.5f);
            }

            _particles.EmitImpactRing(boss.Position, new Color(120, 50, 180), 40f + boss.BossUltSubCount * 8f, 20);
            FlashScreen(new Color(80, 30, 140), 0.06f);
            _hitStopTimer = 0.03f;
        }

        // Dark aura
        if (_gameTimer % 0.04f < dt)
        {
            float angle = (float)rng.NextDouble() * MathHelper.TwoPi;
            _particles.Emit(boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 15f,
                new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 40f,
                new Color(100, 40, 160) * 0.5f, 0.2f, 2f);
        }

        if (boss.BossStateTimer <= 0)
        {
            boss.BossState = BossAttackState.Idle;
            boss.BossAttackCooldown = 3f;
            _particles.EmitExplosion(boss.Position, 25, new Color(120, 50, 180));
        }
    }

    // === Helper: Ghost summon (기존 패턴 유지) ===
    private void BossSummonGhosts(Enemy boss, Random rng)
    {
        int summonCount = boss.BossPhase >= 2 ? 4 : (boss.BossPhase >= 1 ? 3 : 2);
        for (int i = 0; i < summonCount; i++)
        {
            float angle = MathHelper.TwoPi * i / summonCount + (float)rng.NextDouble() * 0.5f;
            var spawnPos = boss.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 60f;
            if (_tileMap.IsWalkableWorld(spawnPos))
            {
                var ghost = new Enemy(EnemyType.GhostFire, spawnPos);
                ghost.IsAggro = true;
                _enemiesToAdd.Add(ghost);
                _particles.EmitBurst(spawnPos, 10, new Color(80, 200, 220), 120f, 0.3f, 2f);
            }
        }
        _particles.EmitImpactRing(boss.Position, new Color(50, 140, 70), 40f, 12);
        FlashScreen(new Color(50, 140, 70), 0.08f);
    }

    // === Helper: Club throw spread (기존 패턴 강화) ===
    private void BossClubThrow(Enemy boss, Vector2 toPlayer)
    {
        var throwDir = toPlayer;
        if (throwDir.LengthSquared() > 0) throwDir = Vector2.Normalize(throwDir);
        int count = boss.BossPhase >= 2 ? 7 : (boss.BossPhase >= 1 ? 5 : 3);
        float spread = boss.BossPhase >= 2 ? 0.5f : 0.3f;
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
            float angle = MathF.Atan2(throwDir.Y, throwDir.X) + t * spread * 2f;
            var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float speed = 220f + Random.Shared.Next(60);
            _projectiles.Spawn(boss.Position + d * 20f, d * speed, boss.Attack * 0.7f, false,
                new Color(130, 90, 40), 10f, 2.5f);
        }
        _particles.EmitDirectionalSpark(boss.Position, throwDir, 10, new Color(130, 90, 40), 200f);
    }

    // Simple AI for boss clones: chase + melee only
    private void UpdateCloneBossAI(Enemy clone, float dt, float dist, Vector2 toPlayer)
    {
        if (dist > 50)
            clone.MoveToward(_player.Position, dt);

        // Melee telegraph
        if (clone.IsTelegraphing && clone.TelegraphTimer < 0.05f && clone.TelegraphTimer > 0)
        {
            _projectiles.SpawnMeleeStrike(clone.Position, clone.TelegraphDirection, clone.Attack);
            _particles.EmitExplosion(clone.Position + clone.TelegraphDirection * 20f, 8, new Color(80, 160, 60));
        }

        if (dist < clone.AttackRange + 20 && clone.CanAttack())
        {
            clone.StartTelegraph(toPlayer, 0.5f);
            clone.OnAttack();
        }

        // Clone flicker effect (semi-transparent look)
        if (_gameTimer % 0.15f < dt)
            _particles.Emit(clone.Position, new Vector2(0, -20), new Color(50, 140, 70) * 0.3f, 0.2f, 1.5f);
    }

    private void ShootArrow()
    {
        // Archer removed - swordsman only
    }

    private void SwordSlash()
    {
        var mouseWorld = GetMouseWorldPosition();
        var aimDir = mouseWorld - _player.Position;
        if (aimDir.LengthSquared() > 0) aimDir.Normalize();
        float aimAngle = MathF.Atan2(aimDir.Y, aimDir.X);

        // Combo scaling: 약(1)=1x, 중(2)=1.3x, 강(3)=2x
        int combo = Math.Clamp(_player.ComboStep, 1, 3);
        float comboDmgMul = combo switch { 1 => 1f, 2 => 1.2f, _ => 1.5f };
        float comboRangeMul = combo switch { 1 => 0.87f, 2 => 1f, _ => 1.27f };
        float comboArcMul = combo switch { 1 => 0.8f, 2 => 1f, _ => 1.28f };
        float comboKnockMul = combo switch { 1 => 0.8f, 2 => 1f, _ => 1.5f };

        // DrawSlash (발도): after dash, x2 damage and x2 range
        float drawSlashMul = 1f;
        if (_drawSlashReady)
        {
            drawSlashMul = 2f;
            _drawSlashReady = false;
            _particles.EmitImpactRing(_player.Position, new Color(255, 220, 100), 50f, 16);
            FlashScreen(new Color(255, 220, 100), 0.08f);
        }

        float baseDamage = (_player.Attack + _augmentStats.AttackBonus) * comboDmgMul * drawSlashMul;
        // Berserker resonance
        if (_augmentStats.SynergyBerserkerResonance && _player.HP < (_player.MaxHP + _augmentStats.MaxHPBonus) * 0.4f)
            baseDamage *= 1.5f;

        float range = _augmentStats.SwordRange * comboRangeMul * drawSlashMul;
        float arc = _augmentStats.SwordArc * comboArcMul;
        float knockback = _augmentStats.SwordKnockback * comboKnockMul;

        // Slash particles (more on stronger hits)
        int slashParticles = combo switch { 1 => 8, 2 => 12, _ => 20 };
        for (int i = 0; i < slashParticles; i++)
        {
            float t = (float)i / slashParticles;
            float angle = aimAngle - arc / 2f + arc * t;
            float dist = range * (0.6f + 0.4f * t);
            var pos = _player.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (40f + combo * 20f);
            var slashColor = combo == 3 ? new Color(255, 240, 180) : new Color(255, 245, 220);
            _particles.Emit(pos, vel, slashColor, 0.15f + 0.05f * t, 2f + combo);
        }

        // Damage enemies in arc (generous: matches visual crescent)
        int hitCount = 0;
        float enemyRadius = 15f; // approximate enemy body radius
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            var toEnemy = enemy.Position - _player.Position;
            float dist = toEnemy.Length();
            // Include enemies whose body overlaps the arc range
            if (dist > range + enemyRadius) continue;

            float enemyAngle = MathF.Atan2(toEnemy.Y, toEnemy.X);
            float angleDiff = MathHelper.WrapAngle(enemyAngle - aimAngle);
            // Add angular tolerance based on enemy size at distance
            float angleMargin = dist > 1f ? MathF.Atan2(enemyRadius, dist) : 0.5f;
            if (MathF.Abs(angleDiff) > arc / 2f + angleMargin) continue;

            bool isCrit = Random.Shared.NextDouble() < (_player.CritRate + _augmentStats.CritRateBonus);
            float dmg = baseDamage * (isCrit ? _augmentStats.CritDamageMultiplier : 1f);

            enemy.TakeDamage(dmg);
            enemy.ApplyKnockback(toEnemy, knockback);
            hitCount++;

            _damageNumbers.Spawn(enemy.Position, dmg, isCrit);
            _particles.EmitBurst(enemy.Position, combo == 3 ? 15 : 6, new Color(255, 220, 150), 80f + combo * 30f, 0.2f, 2f + combo);

            // Hit stop on crit or 3rd combo
            if (isCrit || combo == 3)
                _hitStopTimer = combo == 3 ? 0.06f : 0.04f;

            // Life steal
            float totalLifeSteal = _augmentStats.LifeSteal;
            if (totalLifeSteal > 0)
                _player.HP = Math.Min(_player.MaxHP + _augmentStats.MaxHPBonus, _player.HP + dmg * totalLifeSteal);

            // ExplosiveFlame (폭발): fire explosion on hit
            if (_augmentStats.ExplosiveFlame)
            {
                PerformExplosion(enemy.Position, dmg * 0.3f, 45f, new Color(255, 120, 40));
                _particles.EmitBurst(enemy.Position, 8, new Color(255, 100, 30), 100f, 0.2f, 3f);
            }

            // CritLightning (번개): chain lightning on crit
            if (isCrit && _augmentStats.CritLightning)
            {
                PerformChainLightning(enemy, dmg * 0.5f, 3);
                AudioManager.Play("lightning", 0.5f, 0.1f, 0.08f);
            }
        }

        // 3rd combo: screen shake + impact zoom
        if (combo == 3 && hitCount > 0)
        {
            _camera.Shake(5f, 0.15f);
            _camera.ImpactZoom(0.03f);
        }

        // CrescentWave (초승): 3rd hit fires a crescent projectile
        if (combo == 3 && _augmentStats.CrescentWave && hitCount > 0)
        {
            var proj = _projectiles.SpawnPlayerArrow(
                _player.Position + aimDir * 25f, aimDir,
                baseDamage * 0.8f, 300f, 8f, new Color(220, 240, 255));
            proj.PierceRemaining = 5;
            proj.Life = 0.8f;
            _particles.EmitDirectionalSpark(_player.Position + aimDir * 20f, aimDir, 10, new Color(200, 230, 255), 200f);
        }

        // GroundCrack (균열): 3rd hit creates delayed explosion
        if (combo == 3 && _augmentStats.GroundCrack && hitCount > 0)
        {
            var crackPos = _player.Position + aimDir * range * 0.6f;
            float crackDmg = baseDamage;
            _groundCracks.Add((crackPos, crackDmg, 0.8f, 0.8f));
            _particles.EmitBurst(crackPos, 6, new Color(180, 120, 80), 40f, 0.15f, 2f);
        }

        // WindBurst (바람): 3rd hit activates 5s attack speed buff
        if (combo == 3 && _augmentStats.WindBurst && hitCount > 0)
        {
            float windDuration = 5f;
            if (_augmentStats.SynergyBerserkerResonance && _player.HP < (_player.MaxHP + _augmentStats.MaxHPBonus) * 0.4f)
                windDuration *= 1.5f;
            _windBurstActiveTimer = windDuration;
            _particles.EmitImpactRing(_player.Position, new Color(150, 230, 200), 40f, 12);
        }

        // AfterImage (잔영): schedule ghost slash 0.2s later
        if (_augmentStats.AfterImage && hitCount > 0)
        {
            float afterDmg = baseDamage;
            if (_augmentStats.SynergyFocusResonance) afterDmg *= 2f;
            _afterImageSlashes.Add((_player.Position, aimAngle, afterDmg, range, arc, knockback, 0.2f));
            _particles.EmitBurst(_player.Position, 4, new Color(180, 150, 255) * 0.5f, 40f, 0.15f, 2f);
        }

        // Combo counter
        if (hitCount > 0)
        {
            _comboCount += hitCount;
            _comboTimer = 1.5f;
        }
    }

    private void PerformGhostSlash(Vector2 pos, float aimAngle, float damage, float range, float arc, float knockback)
    {
        // Visual: ghost arc
        for (int i = 0; i < 16; i++)
        {
            float t = (float)i / 16;
            float angle = aimAngle - arc / 2f + arc * t;
            float dist = range * (0.5f + 0.5f * t);
            var p = pos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
            _particles.Emit(p, new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 30f,
                new Color(180, 150, 255) * 0.6f, 0.2f, 3f);
        }
        _particles.EmitImpactRing(pos, new Color(180, 150, 255) * 0.4f, range * 0.7f, 12);

        // Damage enemies
        float enemyRadius = 15f;
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            var toEnemy = enemy.Position - pos;
            float dist2 = toEnemy.Length();
            if (dist2 > range + enemyRadius) continue;

            float enemyAngle = MathF.Atan2(toEnemy.Y, toEnemy.X);
            float angleDiff = MathHelper.WrapAngle(enemyAngle - aimAngle);
            float angleMargin = dist2 > 1f ? MathF.Atan2(enemyRadius, dist2) : 0.5f;
            if (MathF.Abs(angleDiff) > arc / 2f + angleMargin) continue;

            bool isCrit = Random.Shared.NextDouble() < (_player.CritRate + _augmentStats.CritRateBonus);
            float dmg = damage * (isCrit ? _augmentStats.CritDamageMultiplier : 1f);

            enemy.TakeDamage(dmg);
            enemy.ApplyKnockback(toEnemy, knockback);
            _damageNumbers.Spawn(enemy.Position, dmg, isCrit);
            _particles.EmitBurst(enemy.Position, 6, new Color(180, 150, 255), 80f, 0.15f, 2f);
        }

        AudioManager.Play("arrow_shoot", 0.4f, -0.3f, 0.05f);
    }

    private void PerformArrowRain()
    {
        // Archer removed - swordsman only
    }

    private void HandleProjectileWallCollisions()
    {
        foreach (var proj in _projectiles.Projectiles)
        {
            if (!proj.IsActive) continue;
            if (!_tileMap.IsWalkableWorld(proj.Position))
            {
                if (proj.Explosive)
                    PerformExplosion(proj.Position, proj.Damage * proj.ExplosionDamageRatio, proj.ExplosionRadius, proj.Color);
                proj.IsActive = false;
            }
        }
    }

    private void HandleProjectileCollisions()
    {
        // Homing update
        foreach (var proj in _projectiles.Projectiles)
        {
            if (!proj.IsActive || !proj.IsPlayerOwned || !proj.Homing) continue;

            Enemy nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsActive || enemy.IsDead) continue;
                float d = Vector2.Distance(proj.Position, enemy.Position);
                if (d < nearestDist && d < 200f)
                {
                    nearest = enemy;
                    nearestDist = d;
                }
            }
            if (nearest != null)
            {
                var toTarget = nearest.Position - proj.Position;
                if (toTarget.LengthSquared() > 0) toTarget.Normalize();
                var currentDir = proj.Velocity;
                if (currentDir.LengthSquared() > 0) currentDir.Normalize();
                var newDir = Vector2.Lerp(currentDir, toTarget, proj.HomingStrength * 0.02f);
                if (newDir.LengthSquared() > 0) newDir.Normalize();
                float speed = proj.Velocity.Length();
                proj.Velocity = newDir * speed;
            }
        }

        // Flame trail
        foreach (var proj in _projectiles.Projectiles)
        {
            if (!proj.IsActive || !proj.FlameTrail) continue;
            if (proj.TrailTimer > 0.03f)
            {
                proj.TrailTimer = 0;
                _particles.Emit(proj.Position, new Vector2(0, -20),
                    new Color(255, 120, 30) * 0.7f, 0.2f, 2f + (float)Random.Shared.NextDouble() * 1.5f);
            }
        }

        foreach (var proj in _projectiles.Projectiles)
        {
            if (!proj.IsActive) continue;

            if (proj.IsPlayerOwned)
            {
                foreach (var enemy in _enemies)
                {
                    if (!enemy.IsActive || enemy.IsDead) continue;
                    if (!proj.Bounds.Intersects(enemy.Bounds)) continue;

                    bool isCrit = Random.Shared.NextDouble() < (_player.CritRate + _augmentStats.CritRateBonus);
                    float dmg = proj.Damage * (isCrit ? _augmentStats.CritDamageMultiplier : 1f);

                    enemy.TakeDamage(dmg);
                    _damageNumbers.Spawn(enemy.Position, dmg, isCrit);

                    var hitDir = enemy.Position - proj.Position;
                    _particles.EmitDirectionalSpark(enemy.Position, hitDir, isCrit ? 12 : 6, proj.Color);
                    _particles.EmitBurst(enemy.Position, isCrit ? 10 : 4, proj.Color, isCrit ? 220f : 120f);
                    enemy.ApplyKnockback(hitDir, isCrit ? 200f : 80f);

                    if (isCrit)
                    {
                        _particles.EmitImpactRing(enemy.Position, new Color(255, 230, 100), 20f, 12);
                        _hitStopTimer = 0.03f;
                        AudioManager.Play("hit_crit", 0.7f, 0.1f, 0.04f);
                        _camera.DirectionalShake(hitDir, 3f, 0.08f);
                        _camera.ImpactZoom(0.015f);
                    }
                    else
                    {
                        AudioManager.Play("hit_normal", 0.45f, 0.2f, 0.03f);
                        _camera.DirectionalShake(hitDir, 1f, 0.04f);
                    }

                    if (proj.FrostEffect)
                    {
                        enemy.ApplySlow(proj.FrostSlow, 2f);
                        _particles.EmitBurst(enemy.Position, 6, new Color(100, 200, 255), 80f, 0.3f, 1.5f);
                        AudioManager.Play("frost", 0.4f, 0.15f, 0.1f);
                    }

                    if (proj.ChainLightning && proj.ChainCount > 0)
                    {
                        PerformChainLightning(enemy, proj.Damage * proj.ChainDamage, proj.ChainCount);
                        AudioManager.Play("lightning", 0.5f, 0.1f, 0.08f);
                    }

                    if (proj.Explosive)
                    {
                        PerformExplosion(enemy.Position, proj.Damage * proj.ExplosionDamageRatio, proj.ExplosionRadius, proj.Color);
                        AudioManager.Play("explosion", 0.6f, 0.15f, 0.06f);
                        _camera.Shake(3f, 0.1f);
                    }

                    if (_augmentStats.LifeSteal > 0)
                        _player.HP = Math.Min(_player.MaxHP + _augmentStats.MaxHPBonus, _player.HP + dmg * _augmentStats.LifeSteal);

                    if (proj.PierceRemaining > 0)
                    {
                        proj.PierceRemaining--;
                        proj.Damage *= 0.8f;
                        break;
                    }

                    if (proj.BounceRemaining > 0)
                    {
                        proj.BounceRemaining--;
                        var newTarget = FindNearestEnemy(enemy.Position, 150f, enemy);
                        if (newTarget != null)
                        {
                            var bounceDir = newTarget.Position - enemy.Position;
                            if (bounceDir.LengthSquared() > 0) bounceDir.Normalize();
                            proj.Position = enemy.Position;
                            proj.Velocity = bounceDir * proj.Velocity.Length();
                            proj.Life = 1f;
                            _particles.EmitBurst(enemy.Position, 4, proj.Color, 100f, 0.1f, 1.5f);
                        }
                        else
                        {
                            proj.IsActive = false;
                        }
                        break;
                    }

                    proj.IsActive = false;
                    break;
                }
            }
            else
            {
                if (proj.Bounds.Intersects(_player.Bounds) && !_player.IsInvincible)
                {
                    var hitDir = _player.Position - proj.Position;
                    TryDamagePlayer(proj.Damage, hitDir, 180f);
                    _particles.EmitBurst(_player.Position, 12, new Color(255, 150, 50), 200f);
                    _particles.EmitDirectionalSpark(_player.Position, hitDir, 8, new Color(255, 100, 40));
                    _hitStopTimer = 0.03f;
                    FlashScreen(new Color(255, 50, 50), 0.06f);
                    AudioManager.Play("player_hit", 0.8f, 0.05f);
                    _camera.DirectionalShake(hitDir, 4f, 0.1f);
                    _camera.ImpactZoom(0.02f);
                    proj.IsActive = false;
                }
            }
        }
    }

    private void PerformChainLightning(Enemy source, float damage, int chainCount)
    {
        var current = source;
        for (int i = 0; i < chainCount; i++)
        {
            var next = FindNearestEnemy(current.Position, 120f, current);
            if (next == null) break;

            next.TakeDamage(damage);
            _damageNumbers.Spawn(next.Position, damage, false);

            var start = current.Position;
            var end = next.Position;
            int segments = 6;
            for (int s = 0; s < segments; s++)
            {
                float t = s / (float)segments;
                var pos = Vector2.Lerp(start, end, t);
                pos += new Vector2((float)Random.Shared.NextDouble() * 10 - 5, (float)Random.Shared.NextDouble() * 10 - 5);
                _particles.Emit(pos, Vector2.Zero, new Color(130, 180, 255), 0.15f, 2f);
            }
            _particles.EmitBurst(next.Position, 6, new Color(130, 180, 255), 120f, 0.15f, 1.5f);

            damage *= 0.7f;
            current = next;
        }
    }

    private void PerformExplosion(Vector2 position, float damage, float radius, Color color)
    {
        _particles.EmitExplosion(position, 20, color);
        _particles.EmitImpactRing(position, color, radius, 20);

        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive || enemy.IsDead) continue;
            if (Vector2.Distance(enemy.Position, position) < radius)
            {
                enemy.TakeDamage(damage);
                _damageNumbers.Spawn(enemy.Position, damage, false);
                enemy.ApplyKnockback(enemy.Position - position, 150f);
            }
        }
    }

    private Enemy FindNearestEnemy(Vector2 position, float maxDist, Enemy exclude)
    {
        Enemy nearest = null;
        float nearestDist = maxDist;
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive || enemy.IsDead || enemy == exclude) continue;
            float d = Vector2.Distance(position, enemy.Position);
            if (d < nearestDist)
            {
                nearest = enemy;
                nearestDist = d;
            }
        }
        return nearest;
    }

    private void DrawItemPickupNotification(SpriteBatch spriteBatch)
    {
        float alpha = Math.Min(1f, _itemPickupTimer);
        float slideUp = (1f - alpha) * 20f;
        float scale = 0.9f;
        string text = SanitizeForFont(Fonts.Game, _itemPickupText);
        var size = Fonts.Game.MeasureString(text);
        var pos = new Vector2(Game1.ScreenWidth / 2f - size.X * scale / 2f, 90 - slideUp);

        // Background
        int boxW = (int)(size.X * scale) + 30;
        int boxH = 34;
        spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 15, (int)pos.Y - 6, boxW, boxH),
            new Color(10, 8, 5) * alpha * 0.9f);
        DrawRectOutline(spriteBatch, new Rectangle((int)pos.X - 15, (int)pos.Y - 6, boxW, boxH),
            _itemPickupColor * alpha * 0.7f, 1);

        // Shadow
        spriteBatch.DrawString(Fonts.Game, text, pos + new Vector2(1, 1), new Color(0, 0, 0) * alpha * 0.8f,
            0, Vector2.Zero, scale, SpriteEffects.None, 0);
        spriteBatch.DrawString(Fonts.Game, text, pos, _itemPickupColor * alpha,
            0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private static EnemyType RollEnemyType(int floor)
    {
        int roll = Random.Shared.Next(100);
        if (floor <= 2)
        {
            // Tier 1: Soldier, Archer
            return roll < 65 ? EnemyType.Soldier : EnemyType.Archer;
        }
        if (floor <= 4)
        {
            // Tier 1+2: Soldier, Archer, Warrior, GhostFire, Spearman, ShieldBearer
            if (roll < 20) return EnemyType.Soldier;
            if (roll < 35) return EnemyType.Archer;
            if (roll < 50) return EnemyType.Warrior;
            if (roll < 60) return EnemyType.GhostFire;
            if (roll < 75) return EnemyType.Spearman;
            if (roll < 85) return EnemyType.ShieldBearer;
            return roll < 93 ? EnemyType.Spearman : EnemyType.Warrior;
        }
        if (floor <= 6)
        {
            // Tier 1+2+3
            if (roll < 8) return EnemyType.Soldier;
            if (roll < 16) return EnemyType.Archer;
            if (roll < 24) return EnemyType.Warrior;
            if (roll < 30) return EnemyType.GhostFire;
            if (roll < 38) return EnemyType.Spearman;
            if (roll < 44) return EnemyType.ShieldBearer;
            if (roll < 54) return EnemyType.Assassin;
            if (roll < 62) return EnemyType.Shaman;
            if (roll < 72) return EnemyType.FireArcher;
            if (roll < 82) return EnemyType.PoisonThrower;
            return roll < 91 ? EnemyType.Assassin : EnemyType.FireArcher;
        }
        // Floor 7+: Tier 1+2+3+4 (Tier 4 dominant)
        if (roll < 5) return EnemyType.Soldier;
        if (roll < 10) return EnemyType.Archer;
        if (roll < 15) return EnemyType.Warrior;
        if (roll < 18) return EnemyType.Spearman;
        if (roll < 21) return EnemyType.ShieldBearer;
        if (roll < 28) return EnemyType.Assassin;
        if (roll < 34) return EnemyType.Shaman;
        if (roll < 40) return EnemyType.FireArcher;
        if (roll < 46) return EnemyType.PoisonThrower;
        if (roll < 58) return EnemyType.DarkKnight;
        if (roll < 68) return EnemyType.Summoner;
        if (roll < 80) return EnemyType.BladeDancer;
        return EnemyType.ThunderMonk;
    }

    private void FlashScreen(Color color, float duration)
    {
        _screenFlashColor = color;
        _screenFlashTimer = Math.Max(_screenFlashTimer, duration);
    }

    private Vector2 GetMouseWorldPosition()
    {
        var mouseScreen = InputManager.MousePosition;
        var inverseTransform = Matrix.Invert(_camera.GetTransform());
        return Vector2.Transform(mouseScreen, inverseTransform);
    }

    // ===================== DRAWING =====================

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(20, 16, 12));

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetTransform());

        // Draw tile map
        var cameraBounds = GetCameraBounds();
        _tileMap.Draw(spriteBatch, _pixel, cameraBounds, _gameTimer);

        // Dungeon objects
        foreach (var obj in _dungeonObjects)
            obj.Draw(spriteBatch, _pixel);

        // Ghost trails
        foreach (var ghost in _ghostTrails)
        {
            var ghostColor = new Color(180, 160, 130) * ghost.alpha * 0.4f;
            spriteBatch.Draw(_pixel, new Rectangle((int)ghost.pos.X - 10, (int)ghost.pos.Y - 14, 20, 28), ghostColor);
        }

        // Enemy attack telegraphs
        DrawEnemyTelegraphs(spriteBatch);

        // 2.5D Y-sort
        var drawOrder = new List<Entity> { _player };
        drawOrder.AddRange(_enemies.Where(e => e.IsActive));
        drawOrder.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        foreach (var entity in drawOrder)
        {
            if (entity is not Player)
                DrawShadow(spriteBatch, entity.Position, 10);
            entity.Draw(spriteBatch);
        }

        // Aim line
        DrawAimLine(spriteBatch);

        // Interact prompt
        DrawInteractPrompt(spriteBatch);

        // Boss pouches
        foreach (var pouch in _bossPouches)
            pouch.Draw(spriteBatch, _pixel);

        // Draw dropped items
        foreach (var item in _droppedItems)
            item.Draw(spriteBatch, _pixel);

        _projectiles.Draw(spriteBatch, _pixel);
        _particles.Draw(spriteBatch);
        _damageNumbers.Draw(spriteBatch);

        spriteBatch.End();

        // Screen space UI
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawHUD(spriteBatch);

        if (_state == GameState.FloorTransition)
            DrawFloorTransition(spriteBatch);

        // Item pickup notification
        if (_itemPickupTimer > 0 && _itemPickupText != null)
            DrawItemPickupNotification(spriteBatch);

        // Synergy notifications
        for (int i = 0; i < _synergyNotifications.Count; i++)
        {
            var notif = _synergyNotifications[i];
            float alpha = Math.Min(1f, notif.timer);
            float slideUp = (1f - alpha) * 20f;
            float scale = 1f;
            string text = SanitizeForFont(Fonts.Game, notif.text);
            var size = Fonts.Game.MeasureString(text);
            var pos = new Vector2(Game1.ScreenWidth / 2f - size.X * scale / 2f, 130 + i * 35 - slideUp);

            int boxW = (int)(size.X * scale) + 30;
            int boxH = 34;
            spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 15, (int)pos.Y - 6, boxW, boxH),
                new Color(10, 8, 5) * alpha * 0.9f);
            DrawRectOutline(spriteBatch, new Rectangle((int)pos.X - 15, (int)pos.Y - 6, boxW, boxH),
                notif.color * alpha * 0.7f, 1);

            spriteBatch.DrawString(Fonts.Game, text, pos + new Vector2(1, 1), new Color(0, 0, 0) * alpha * 0.8f,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.DrawString(Fonts.Game, text, pos, notif.color * alpha,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        // WindBurst active indicator
        if (_windBurstActiveTimer > 0)
        {
            string windText = SanitizeForFont(Fonts.Game, $"[Wind] {_windBurstActiveTimer:F1}s");
            var windSize = Fonts.Game.MeasureString(windText);
            var windPos = new Vector2(Game1.ScreenWidth / 2f - windSize.X * 0.8f / 2f, 60);
            spriteBatch.DrawString(Fonts.Game, windText, windPos + new Vector2(1, 1), new Color(0, 0, 0) * 0.6f,
                0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            spriteBatch.DrawString(Fonts.Game, windText, windPos, new Color(150, 230, 200),
                0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }

        if (_comboCount >= 2 && _comboTimer > 0)
            DrawCombo(spriteBatch);

        if (_screenFlashTimer > 0)
        {
            float flashAlpha = Math.Min(1f, _screenFlashTimer * 4f);
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), _screenFlashColor * flashAlpha);
        }

        if (_vignetteIntensity > 0)
            DrawVignette(spriteBatch, _vignetteIntensity);

        // Inventory overlay
        if (_inventoryOpen)
            DrawInventoryUI(spriteBatch);

        spriteBatch.End();
    }

    private Rectangle GetCameraBounds()
    {
        var inverse = Matrix.Invert(_camera.GetTransform());
        var topLeft = Vector2.Transform(Vector2.Zero, inverse);
        var bottomRight = Vector2.Transform(new Vector2(Game1.ScreenWidth, Game1.ScreenHeight), inverse);
        return new Rectangle((int)topLeft.X, (int)topLeft.Y,
            (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
    }

    private void DrawAimLine(SpriteBatch spriteBatch)
    {
        if (_state != GameState.Playing) return;
        var dir = _player.AimDirection;
        if (dir.LengthSquared() < 0.1f) return;

        for (int i = 3; i < 12; i++)
        {
            if (i % 2 == 0) continue;
            float dist = 20f + i * 8f;
            var pos = _player.Position + dir * dist;
            if (!_tileMap.IsWalkableWorld(pos)) break;
            float alpha = 0.15f * (1f - i / 12f);
            spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 1, (int)pos.Y - 1, 2, 2),
                new Color(200, 190, 150) * alpha);
        }
    }

    private void DrawInteractPrompt(SpriteBatch spriteBatch)
    {
        if (_state != GameState.Playing) return;

        // Treasure chests
        foreach (var obj in _dungeonObjects)
        {
            if (!obj.IsActive || obj.IsOpened) continue;
            if (obj.Type != DungeonObjectType.TreasureChest) continue;
            if (!obj.IsPlayerNear(_player.Position)) continue;

            float bob = MathF.Sin(_gameTimer * 3f) * 2f;
            var promptPos = obj.Position + new Vector2(0, -24 + bob);
            // "E" key indicator
            spriteBatch.Draw(_pixel, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(40, 35, 25));
            DrawRectOutline(spriteBatch, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(200, 170, 50), 1);
        }

        // Portal
        if (_portalActive && Vector2.Distance(_player.Position, _tileMap.PortalPosition) < 40f)
        {
            float bob = MathF.Sin(_gameTimer * 3f) * 2f;
            var promptPos = _tileMap.PortalPosition + new Vector2(0, -30 + bob);
            spriteBatch.Draw(_pixel, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(20, 40, 45));
            DrawRectOutline(spriteBatch, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(80, 200, 220), 1);
        }
    }

    private void DrawEnemyTelegraphs(SpriteBatch spriteBatch)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive || !enemy.IsTelegraphing) continue;

            float progress = 1f - (enemy.TelegraphTimer / enemy.TelegraphDuration);
            float alpha = 0.3f + progress * 0.5f;
            float pulse = MathF.Sin(_gameTimer * 20f) * 0.2f;
            alpha += pulse;

            bool isRanged = enemy.Type is EnemyType.Archer or EnemyType.Shaman or EnemyType.FireArcher
                or EnemyType.PoisonThrower or EnemyType.Summoner;

            if (isRanged)
            {
                var dir = enemy.TelegraphDirection;
                Color rangedColor = enemy.Type switch
                {
                    EnemyType.FireArcher => new Color(255, 120, 30),
                    EnemyType.PoisonThrower => new Color(80, 200, 60),
                    EnemyType.Shaman => new Color(100, 200, 180),
                    EnemyType.Summoner => new Color(160, 100, 220),
                    _ => new Color(255, 60, 40)
                };
                var telegraphColor = rangedColor * alpha;
                float lineLen = 280f;

                for (int i = 0; i < 20; i++)
                {
                    float t = i / 20f;
                    float d = t * lineLen;
                    var pos = enemy.Position + dir * d;
                    if (!_tileMap.IsWalkableWorld(pos)) break;
                    float segAlpha = alpha * (1f - t * 0.5f);
                    int sz = i < 3 ? 3 : 2;
                    spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - sz / 2, (int)pos.Y - sz / 2, sz, sz),
                        telegraphColor * segAlpha);
                }

                var warnColor = rangedColor * alpha;
                spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 1, (int)enemy.Position.Y - 22, 3, 8), warnColor);
                spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 1, (int)enemy.Position.Y - 12, 3, 3), warnColor);
            }
            else if (enemy.Type != EnemyType.GhostFire)
            {
                Color meleeColor = enemy.Type switch
                {
                    EnemyType.Warrior or EnemyType.DarkKnight => new Color(255, 40, 30),
                    EnemyType.Assassin => new Color(200, 100, 200),
                    EnemyType.BladeDancer => new Color(220, 80, 120),
                    EnemyType.ThunderMonk => new Color(130, 130, 255),
                    _ => new Color(255, 120, 60)
                };
                var telegraphColor = meleeColor * alpha;

                spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 1, (int)enemy.Position.Y - 22, 3, 8), telegraphColor);
                spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 1, (int)enemy.Position.Y - 12, 3, 3), telegraphColor);

                var dir = enemy.TelegraphDirection;
                float indicLen = enemy.Type is EnemyType.Warrior or EnemyType.DarkKnight or EnemyType.Spearman ? 35f : 20f;
                if (enemy.Type == EnemyType.BladeDancer) indicLen = 25f;
                var endPos = enemy.Position + dir * indicLen;

                for (int i = 0; i < 6; i++)
                {
                    float t = i / 6f;
                    var pos = Vector2.Lerp(enemy.Position, endPos, t);
                    int sz = (int)(4 * (1f - t));
                    if (sz < 1) sz = 1;
                    spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - sz / 2, (int)pos.Y - sz / 2, sz, sz),
                        telegraphColor * (1f - t * 0.5f));
                }
            }
        }
    }

    private void DrawShadow(SpriteBatch spriteBatch, Vector2 pos, int radius)
    {
        spriteBatch.Draw(_pixel,
            new Rectangle((int)(pos.X - radius), (int)(pos.Y + 8), radius * 2, radius / 2),
            new Color(0, 0, 0) * 0.3f);
    }

    private void DrawCombo(SpriteBatch spriteBatch)
    {
        float alpha = Math.Min(1f, _comboTimer / 0.3f);
        float scale = 0.8f + _comboCount * 0.05f;
        scale = Math.Min(scale, 1.4f);
        float pulse = MathF.Sin(_gameTimer * 8f) * 0.1f;
        scale += pulse;

        string comboText = $"{_comboCount} 연타";
        var size = Fonts.Game.MeasureString(comboText);
        var pos = new Vector2(Game1.ScreenWidth / 2f - size.X * scale / 2f, 60);

        Color comboColor;
        if (_comboCount >= 10) comboColor = new Color(255, 50, 50);
        else if (_comboCount >= 7) comboColor = new Color(255, 180, 50);
        else if (_comboCount >= 4) comboColor = new Color(255, 220, 100);
        else comboColor = new Color(220, 200, 160);

        spriteBatch.DrawString(Fonts.Game, comboText, pos + new Vector2(2, 2),
            new Color(0, 0, 0) * alpha * 0.5f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        spriteBatch.DrawString(Fonts.Game, comboText, pos,
            comboColor * alpha, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private void DrawHUD(SpriteBatch spriteBatch)
    {
        int m = 16;

        spriteBatch.DrawString(Fonts.Game, "체력", new Vector2(m, m - 2), new Color(180, 40, 40), 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        DrawResourceBar(spriteBatch, m + 32, m,
            _player.HP, _player.MaxHP + _augmentStats.MaxHPBonus,
            new Color(180, 40, 40), new Color(220, 60, 60), new Color(40, 12, 12), 190, 14);
        string hpText = $"{(int)_player.HP}/{(int)(_player.MaxHP + _augmentStats.MaxHPBonus)}";
        spriteBatch.DrawString(Fonts.Game, hpText, new Vector2(m + 36, m - 1), new Color(220, 200, 180), 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);

        spriteBatch.DrawString(Fonts.Game, "기력", new Vector2(m, m + 18), new Color(50, 70, 180), 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        DrawResourceBar(spriteBatch, m + 32, m + 20,
            _player.Ki, _player.MaxKi,
            new Color(50, 70, 180), new Color(70, 100, 220), new Color(12, 12, 40), 140, 10);

        // Dash charges
        int dashY = m + 36;
        for (int i = 0; i < Player.MaxDashCharges; i++)
        {
            bool filled = i < _player.DashCharges;
            var dashColor = filled ? new Color(200, 180, 120) : new Color(50, 45, 35);
            spriteBatch.Draw(_pixel, new Rectangle(m + i * 14, dashY, 10, 10), dashColor);
            DrawRectOutline(spriteBatch, new Rectangle(m + i * 14, dashY, 10, 10), new Color(100, 90, 70), 1);
        }
        spriteBatch.DrawString(Fonts.Game, "회피", new Vector2(m + Player.MaxDashCharges * 14 + 4, dashY - 1), new Color(150, 130, 100), 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

        DrawFloorIndicator(spriteBatch);
        DrawKillCounter(spriteBatch);
        DrawCollectedItemIcons(spriteBatch);
        DrawMinimap(spriteBatch);

        // Boss HP bar
        if (_isBossFloor && _boss != null && !_bossDefeated)
            DrawBossHPBar(spriteBatch);
    }

    private void DrawResourceBar(SpriteBatch spriteBatch, int x, int y, float current, float max,
        Color barColor, Color highlight, Color bgColor, int width, int height)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), bgColor);
        float ratio = Math.Clamp(current / max, 0, 1);
        int fillW = (int)(width * ratio);
        spriteBatch.Draw(_pixel, new Rectangle(x, y, fillW, height), barColor);
        spriteBatch.Draw(_pixel, new Rectangle(x, y, fillW, 2), highlight);
        DrawRectOutline(spriteBatch, new Rectangle(x, y, width, height), new Color(80, 70, 55), 1);
        for (int i = 1; i < 4; i++)
            spriteBatch.Draw(_pixel, new Rectangle(x + width * i / 4, y, 1, height), new Color(0, 0, 0) * 0.3f);
    }

    private void DrawBossHPBar(SpriteBatch spriteBatch)
    {
        int barW = 500;
        int barH = 20;
        int x = (Game1.ScreenWidth - barW) / 2;
        int y = Game1.ScreenHeight - 55;

        // Outer frame with decorative corners
        spriteBatch.Draw(_pixel, new Rectangle(x - 4, y - 4, barW + 8, barH + 8), new Color(15, 12, 8));

        // Background
        spriteBatch.Draw(_pixel, new Rectangle(x - 2, y - 2, barW + 4, barH + 4), new Color(30, 22, 15));

        // HP fill
        float ratio = _boss.MaxHP > 0 ? Math.Clamp(_boss.HP / _boss.MaxHP, 0, 1) : 0;
        var hpColor = ratio > 0.5f ? new Color(50, 180, 70) : ratio > 0.25f ? new Color(200, 160, 40) : new Color(220, 40, 30);

        // Phase 2: pulsing HP bar
        if (_boss.BossPhase >= 2)
        {
            float pulse = MathF.Sin(_gameTimer * 6f) * 0.15f + 0.85f;
            hpColor = Color.Lerp(hpColor, new Color(255, 60, 30), 0.3f) * pulse;
        }

        int fillW = (int)(barW * ratio);
        spriteBatch.Draw(_pixel, new Rectangle(x, y, fillW, barH), hpColor);

        // Highlight
        spriteBatch.Draw(_pixel, new Rectangle(x, y, fillW, 3), Color.Lerp(hpColor, Color.White, 0.4f));
        // Bottom shadow
        spriteBatch.Draw(_pixel, new Rectangle(x, y + barH - 2, fillW, 2), hpColor * 0.5f);

        // Phase markers at 60% and 30%
        int mark60 = x + (int)(barW * 0.6f);
        int mark30 = x + (int)(barW * 0.3f);
        spriteBatch.Draw(_pixel, new Rectangle(mark60, y - 2, 2, barH + 4), new Color(200, 200, 200) * 0.4f);
        spriteBatch.Draw(_pixel, new Rectangle(mark30, y - 2, 2, barH + 4), new Color(255, 100, 50) * 0.5f);

        // Border
        var borderColor = _boss.BossPhase >= 2 ? new Color(200, 60, 40) :
                          _boss.BossPhase >= 1 ? new Color(150, 120, 50) : new Color(100, 80, 50);
        DrawRectOutline(spriteBatch, new Rectangle(x - 4, y - 4, barW + 8, barH + 8), borderColor, 1);
        DrawRectOutline(spriteBatch, new Rectangle(x - 2, y - 2, barW + 4, barH + 4), borderColor * 0.7f, 1);

        // Decorative corner dots
        int dotSize = 4;
        spriteBatch.Draw(_pixel, new Rectangle(x - 6, y - 6, dotSize, dotSize), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(x + barW + 2, y - 6, dotSize, dotSize), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(x - 6, y + barH + 2, dotSize, dotSize), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(x + barW + 2, y + barH + 2, dotSize, dotSize), borderColor);

        // Boss name
        string bossName = SanitizeForFont(Fonts.Game, _currentStage.BossName);
        var nameSize = Fonts.Game.MeasureString(bossName);
        float nameScale = 0.7f;
        var namePos = new Vector2(Game1.ScreenWidth / 2f - nameSize.X * nameScale / 2f, y - 26);
        // Shadow
        spriteBatch.DrawString(Fonts.Game, bossName, namePos + new Vector2(1, 1),
            new Color(10, 8, 5), 0, Vector2.Zero, nameScale, SpriteEffects.None, 0);
        var nameColor = _boss.BossPhase >= 2 ? new Color(255, 120, 80) :
                        _boss.BossPhase >= 1 ? new Color(255, 220, 120) : new Color(220, 190, 120);
        spriteBatch.DrawString(Fonts.Game, bossName, namePos,
            nameColor, 0, Vector2.Zero, nameScale, SpriteEffects.None, 0);

        // Phase text
        if (_boss.BossPhase >= 1)
        {
            string phaseText = _boss.BossPhase >= 2 ? "광폭화" : "2단계";
            var phaseSize = Fonts.Game.MeasureString(phaseText);
            float phaseScale = 0.5f;
            float phaseAlpha = MathF.Sin(_gameTimer * 4f) * 0.2f + 0.8f;
            var phaseColor = _boss.BossPhase >= 2 ? new Color(255, 80, 40) * phaseAlpha : new Color(200, 200, 80) * phaseAlpha;
            spriteBatch.DrawString(Fonts.Game, phaseText,
                new Vector2(Game1.ScreenWidth / 2f + nameSize.X * nameScale / 2f + 8, y - 22),
                phaseColor, 0, Vector2.Zero, phaseScale, SpriteEffects.None, 0);
        }
    }

    private void DrawFloorIndicator(SpriteBatch spriteBatch)
    {
        int cx = Game1.ScreenWidth / 2;
        int y = 10;
        int frameW = 160;
        int frameH = 26;
        var frameColor = new Color(80, 65, 45);
        var rect = new Rectangle(cx - frameW / 2, y, frameW, frameH);
        spriteBatch.Draw(_pixel, rect, new Color(20, 16, 12) * 0.8f);
        DrawRectOutline(spriteBatch, rect, frameColor, 1);

        spriteBatch.Draw(_pixel, new Rectangle(cx - frameW / 2 - 20, y + frameH / 2, 18, 1), frameColor * 0.6f);
        spriteBatch.Draw(_pixel, new Rectangle(cx + frameW / 2 + 2, y + frameH / 2, 18, 1), frameColor * 0.6f);

        string floorText = $"지하 {_floor}층";
        var floorSize = Fonts.Game.MeasureString(floorText);
        spriteBatch.DrawString(Fonts.Game, floorText,
            new Vector2(cx - floorSize.X * 0.7f / 2f, y + 2),
            new Color(220, 190, 120), 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        // Kill progress bar
        int barW = (int)(frameW * 0.7f);
        int barX = cx - barW / 2;
        int barY = y + frameH - 6;
        float progress = _floorEnemyCount > 0 ? Math.Min(1f, (float)_floorKills / _floorEnemyCount) : 0;
        spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barW, 3), new Color(30, 25, 18));
        var barColor = _portalActive ? new Color(80, 200, 220) : new Color(220, 190, 100);
        spriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(barW * progress), 3), barColor);
    }

    private void DrawKillCounter(SpriteBatch spriteBatch)
    {
        string killText = $"처치 {_totalKills}";
        var killSize = Fonts.Game.MeasureString(killText);
        spriteBatch.DrawString(Fonts.Game, killText,
            new Vector2(Game1.ScreenWidth - killSize.X * 0.7f - 16, 14),
            new Color(150, 130, 100), 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
    }

    private void DrawCollectedItemIcons(SpriteBatch spriteBatch)
    {
        int x = 16;
        int y = Game1.ScreenHeight - 42;
        int iconSize = 24;
        int gap = 4;

        var items = _inventory.GetSortedItems();

        // Background bar
        if (items.Count > 0)
        {
            int barW = items.Count * (iconSize + gap) + 8;
            spriteBatch.Draw(_pixel, new Rectangle(x - 4, y - 4, barW, iconSize + 8), new Color(0, 0, 0) * 0.4f);
        }

        for (int i = 0; i < items.Count && i < 20; i++)
        {
            var (id, count) = items[i];
            var info = MeteoriteDatabase.Get(id);
            int ix = x + i * (iconSize + gap);
            var rect = new Rectangle(ix, y, iconSize, iconSize);
            spriteBatch.Draw(_pixel, rect, new Color(20, 16, 12));
            var rarityColor = MeteoriteDatabase.RarityColor(info.Rarity);
            DrawRectOutline(spriteBatch, rect, rarityColor * 0.9f, 1);
            spriteBatch.Draw(_pixel, new Rectangle(ix + 4, y + 4, iconSize - 8, iconSize - 8), info.MainColor * 0.6f);
            spriteBatch.Draw(_pixel, new Rectangle(ix + 7, y + 7, iconSize - 14, iconSize - 14), Color.White * 0.25f);

            // Stack count with shadow
            if (count > 1)
            {
                string countStr = count.ToString();
                spriteBatch.DrawString(Fonts.Game, countStr,
                    new Vector2(ix + iconSize - 7, y + iconSize - 12) + Vector2.One,
                    new Color(0, 0, 0) * 0.8f, 0, Vector2.Zero, 0.4f, SpriteEffects.None, 0);
                spriteBatch.DrawString(Fonts.Game, countStr,
                    new Vector2(ix + iconSize - 7, y + iconSize - 12),
                    Color.White, 0, Vector2.Zero, 0.4f, SpriteEffects.None, 0);
            }
        }

        // Slot count + hint
        string slotText = SanitizeForFont(Fonts.Game, $"{_inventory.UsedSlots}/{Inventory.MaxSlots}  [I] 인벤토리");
        DrawTextWithShadow(spriteBatch, Fonts.Game, slotText,
            new Vector2(x, y - 18), new Color(180, 170, 140), 0.5f);
    }

    private void DrawMinimap(SpriteBatch spriteBatch)
    {
        int mapSize = 100;
        int margin = 12;
        int x = Game1.ScreenWidth - mapSize - margin;
        int y = Game1.ScreenHeight - mapSize - margin;

        var mapArea = new Rectangle(x, y, mapSize, mapSize);
        var enemyPositions = _enemies.Where(e => e.IsActive && !e.IsDead).Select(e => e.Position).ToList();
        _tileMap.DrawMinimap(spriteBatch, _pixel, mapArea, _player.Position, enemyPositions, _gameTimer);
    }

    private void DrawFloorTransition(SpriteBatch spriteBatch)
    {
        float alpha = Math.Min(1f, _floorTransitionTimer);
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), new Color(0, 0, 0) * alpha * 0.4f);

        var shadowColor = new Color(20, 15, 8) * alpha;

        // Stage name (small, above)
        if (_floor <= 1 || _isBossFloor)
        {
            string stageRegion = SanitizeForFont(Fonts.Game, _currentStage.Region);
            var regionSize = Fonts.Game.MeasureString(stageRegion);
            var regionPos = new Vector2(Game1.ScreenWidth / 2f - regionSize.X * 0.7f / 2f, Game1.ScreenHeight / 2f - 50);
            spriteBatch.DrawString(Fonts.Game, stageRegion, regionPos, new Color(160, 140, 100) * alpha,
                0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }

        // Floor text
        string floorText = _isBossFloor
            ? SanitizeForFont(Fonts.Title, _currentStage.BossName)
            : $"지하 {_floor}층";
        var textColor = _isBossFloor ? new Color(255, 80, 50) * alpha : new Color(220, 190, 120) * alpha;
        var size = Fonts.Title.MeasureString(floorText);
        float scaleAnim = 1f + (1f - alpha) * 0.3f;

        var pos = new Vector2(Game1.ScreenWidth / 2f - size.X * scaleAnim / 2f, Game1.ScreenHeight / 2f - size.Y * scaleAnim / 2f - 10);
        spriteBatch.DrawString(Fonts.Title, floorText, pos + new Vector2(2, 2), shadowColor, 0, Vector2.Zero, scaleAnim, SpriteEffects.None, 0);
        spriteBatch.DrawString(Fonts.Title, floorText, pos, textColor, 0, Vector2.Zero, scaleAnim, SpriteEffects.None, 0);

        // Stage subtitle on first floor
        if (_floor <= 1)
        {
            string stageName = SanitizeForFont(Fonts.Game, _currentStage.Name);
            var nameSize = Fonts.Game.MeasureString(stageName);
            var namePos = new Vector2(Game1.ScreenWidth / 2f - nameSize.X * 0.6f / 2f, Game1.ScreenHeight / 2f + 25);
            spriteBatch.DrawString(Fonts.Game, stageName, namePos, new Color(180, 160, 110) * alpha * 0.8f,
                0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        }
    }


    private void DrawVignette(SpriteBatch spriteBatch, float intensity)
    {
        float pulse = MathF.Sin(_gameTimer * 4f) * 0.15f;
        intensity += pulse;
        intensity = Math.Clamp(intensity, 0, 0.8f);

        var color = new Color(100, 10, 10) * intensity;
        int border = 80;
        for (int i = 0; i < border; i++)
        {
            float a = (1f - i / (float)border) * intensity;
            spriteBatch.Draw(_pixel, new Rectangle(0, i, Game1.ScreenWidth, 1), color * a);
            spriteBatch.Draw(_pixel, new Rectangle(0, Game1.ScreenHeight - i, Game1.ScreenWidth, 1), color * a);
        }
        for (int i = 0; i < border; i++)
        {
            float a = (1f - i / (float)border) * intensity * 0.5f;
            spriteBatch.Draw(_pixel, new Rectangle(i, 0, 1, Game1.ScreenHeight), color * a);
            spriteBatch.Draw(_pixel, new Rectangle(Game1.ScreenWidth - i, 0, 1, Game1.ScreenHeight), color * a);
        }
    }

    private static string SanitizeForFont(SpriteFont font, string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (font.Characters.Contains(c) || c == '\n' || c == '\r')
                sb.Append(c);
        }
        return sb.ToString();
    }

    private void DrawWrappedText(SpriteBatch spriteBatch, SpriteFont font, string text, int x, int y, int maxWidth, Color color, float scale)
    {
        text = SanitizeForFont(font, text);
        float lineHeight = font.LineSpacing * scale;
        float currentX = 0;
        float currentY = 0;

        foreach (char c in text)
        {
            string ch = c.ToString();
            var charSize = font.MeasureString(ch) * scale;
            if (currentX + charSize.X > maxWidth && c != ' ')
            {
                currentX = 0;
                currentY += lineHeight;
            }
            spriteBatch.DrawString(font, ch, new Vector2(x + currentX, y + currentY), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            currentX += charSize.X;
        }
    }

    private void DrawStatsTab(SpriteBatch spriteBatch, int panelX, int startY, int panelW, int maxH)
    {
        var s = _augmentStats;
        var statLines = new List<(string category, string name, string value, Color color)>
        {
            ("기본", "체력 (HP)", $"{_player.HP:F0} / {_player.MaxHP + s.MaxHPBonus:F0}", new Color(220, 80, 80)),
            ("기본", "기력 (Ki)", $"{_player.Ki:F0} / {_player.MaxKi:F0}", new Color(80, 100, 220)),
            ("기본", "이동속도", $"{200f:F0}", new Color(180, 255, 200)),
            ("", "", "", Color.Transparent), // separator
            ("공격", "공격력", $"{_player.Attack + s.AttackBonus:F1}", new Color(255, 200, 120)),
            ("공격", "공격력 보너스", $"+{s.AttackBonus:F1}", new Color(220, 180, 140)),
            ("공격", "공격속도 배율", $"{s.FireRateMultiplier:F2}x", new Color(200, 240, 180)),
            ("공격", "검 사거리", $"{s.SwordRange:F0}", new Color(200, 220, 255)),
            ("공격", "검 호 (Arc)", $"{s.SwordArc:F2}", new Color(200, 200, 180)),
            ("공격", "넉백", $"{s.SwordKnockback:F0}", new Color(180, 220, 255)),
            ("공격", "치명타율", $"{(s.CritRateBonus + _player.CritRate) * 100:F1}%", new Color(255, 220, 80)),
            ("공격", "치명타 피해", $"{s.CritDamageMultiplier:F1}x", new Color(255, 200, 60)),
            ("", "", "", Color.Transparent),
            ("방어", "방어력", $"{s.Defense:F1}", new Color(160, 170, 200)),
            ("방어", "최대 HP 보너스", $"+{s.MaxHPBonus:F0}", new Color(100, 255, 120)),
            ("방어", "생명력 흡수", $"{s.LifeSteal * 100:F1}%", new Color(255, 100, 100)),
            ("방어", "회피율", $"{s.EvasionBonus * 100:F1}%", new Color(200, 200, 230)),
            ("", "", "", Color.Transparent),
            ("특수효과", "잔상", s.AfterImage ? "O" : "X", s.AfterImage ? new Color(180, 180, 220) : new Color(120, 110, 90)),
            ("특수효과", "초승달 파동", s.CrescentWave ? "O" : "X", s.CrescentWave ? new Color(255, 240, 180) : new Color(120, 110, 90)),
            ("특수효과", "발도", s.DrawSlash ? "O" : "X", s.DrawSlash ? new Color(255, 220, 100) : new Color(120, 110, 90)),
            ("특수효과", "지면 균열", s.GroundCrack ? "O" : "X", s.GroundCrack ? new Color(200, 160, 80) : new Color(120, 110, 90)),
            ("특수효과", "폭염", s.ExplosiveFlame ? "O" : "X", s.ExplosiveFlame ? new Color(255, 130, 60) : new Color(120, 110, 90)),
            ("특수효과", "치명 번개", s.CritLightning ? "O" : "X", s.CritLightning ? new Color(160, 160, 255) : new Color(120, 110, 90)),
            ("특수효과", "바람 돌풍", s.WindBurst ? "O" : "X", s.WindBurst ? new Color(160, 220, 200) : new Color(120, 110, 90)),
            ("", "", "", Color.Transparent),
            ("시너지", "집중 공명", s.SynergyFocusResonance ? "O" : "X", s.SynergyFocusResonance ? new Color(255, 200, 100) : new Color(120, 110, 90)),
            ("시너지", "광전사 공명", s.SynergyBerserkerResonance ? "O" : "X", s.SynergyBerserkerResonance ? new Color(255, 80, 80) : new Color(120, 110, 90)),
        };

        int lineH = 20;
        int visibleLines = (maxH - 10) / lineH;
        int maxScroll = Math.Max(0, statLines.Count - visibleLines);
        if (_statsScrollOffset > maxScroll) _statsScrollOffset = maxScroll;

        int colW = (panelW - 40) / 2;
        int x1 = panelX + 20;
        int x2 = panelX + 20 + colW;

        // Background
        spriteBatch.Draw(_pixel, new Rectangle(x1 - 4, startY, panelW - 32, maxH), new Color(12, 10, 6));
        DrawRectOutline(spriteBatch, new Rectangle(x1 - 4, startY, panelW - 32, maxH), new Color(60, 50, 35), 1);

        string lastCategory = "";
        int drawn = 0;
        for (int i = _statsScrollOffset; i < statLines.Count && drawn < visibleLines; i++)
        {
            var (cat, name, value, color) = statLines[i];
            int ly = startY + 6 + drawn * lineH;

            if (cat == "" && name == "")
            {
                // Separator
                spriteBatch.Draw(_pixel, new Rectangle(x1, ly + 8, panelW - 44, 1), new Color(60, 50, 35) * 0.5f);
                drawn++;
                continue;
            }

            // Column layout: left half and right half
            int colIndex = drawn < visibleLines / 2 ? 0 : 1;
            // Actually just use single column for simplicity with scroll
            int lx = x1 + 8;

            // Category header
            if (cat != lastCategory)
            {
                DrawTextWithShadow(spriteBatch, Fonts.Game, SanitizeForFont(Fonts.Game, $"[{cat}]"),
                    new Vector2(lx, ly), new Color(200, 180, 100), 0.55f);
                lastCategory = cat;
            }

            // Stat name
            string nameStr = SanitizeForFont(Fonts.Game, name);
            DrawTextWithShadow(spriteBatch, Fonts.Game, nameStr,
                new Vector2(lx + 100, ly), new Color(200, 195, 175), 0.55f);

            // Stat value
            string valStr = SanitizeForFont(Fonts.Game, value);
            DrawTextWithShadow(spriteBatch, Fonts.Game, valStr,
                new Vector2(lx + 360, ly), color, 0.55f);

            drawn++;
        }

        // Scroll indicator
        if (statLines.Count > visibleLines)
        {
            int scrollBarH = maxH - 12;
            int thumbH = Math.Max(20, scrollBarH * visibleLines / statLines.Count);
            int thumbY = startY + 6 + (int)((scrollBarH - thumbH) * ((float)_statsScrollOffset / maxScroll));
            spriteBatch.Draw(_pixel, new Rectangle(panelX + panelW - 30, startY + 6, 4, scrollBarH), new Color(30, 25, 15));
            spriteBatch.Draw(_pixel, new Rectangle(panelX + panelW - 30, thumbY, 4, thumbH), new Color(120, 100, 60));
        }
    }

    private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int t)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), color);
    }

    // ===== Inventory UI =====
    private void UpdateInventoryUI()
    {
        // Tab switch
        if (InputManager.IsKeyPressed(Keys.Tab))
        {
            _inventoryStatsTab = !_inventoryStatsTab;
            _statsScrollOffset = 0;
        }

        if (_inventoryStatsTab)
        {
            // Stats tab: scroll up/down
            if (InputManager.IsKeyPressed(Keys.Up) || InputManager.IsKeyPressed(Keys.W))
                _statsScrollOffset = Math.Max(0, _statsScrollOffset - 1);
            if (InputManager.IsKeyPressed(Keys.Down) || InputManager.IsKeyPressed(Keys.S))
                _statsScrollOffset++;
        }
        else
        {
            // Items tab
            var items = _inventory.GetSortedItems();
            int itemCount = items.Count;

            if (InputManager.IsKeyPressed(Keys.Up) || InputManager.IsKeyPressed(Keys.W))
            {
                _inventorySelectedSlot = Math.Max(0, _inventorySelectedSlot - 1);
                _inventoryDestroyConfirm = false;
            }
            if (InputManager.IsKeyPressed(Keys.Down) || InputManager.IsKeyPressed(Keys.S))
            {
                _inventorySelectedSlot = Math.Min(itemCount - 1, _inventorySelectedSlot + 1);
                _inventoryDestroyConfirm = false;
            }

            if (itemCount > 0 && _inventorySelectedSlot < itemCount)
            {
                if (InputManager.IsKeyPressed(Keys.X))
                {
                    if (_inventoryDestroyConfirm)
                    {
                        var (id, _) = items[_inventorySelectedSlot];
                        _inventory.RemoveAll(id);
                        _inventory.RecalculateStats(_augmentStats);
                        _inventoryDestroyConfirm = false;
                        if (_inventorySelectedSlot >= _inventory.GetSortedItems().Count)
                            _inventorySelectedSlot = Math.Max(0, _inventory.GetSortedItems().Count - 1);
                    }
                    else
                    {
                        _inventoryDestroyConfirm = true;
                    }
                }
            }
        }

        if (InputManager.IsKeyPressed(Keys.Escape))
        {
            _inventoryOpen = false;
        }
    }

    private void DrawTextWithShadow(SpriteBatch sb, SpriteFont font, string text, Vector2 pos, Color color, float scale)
    {
        sb.DrawString(font, text, pos + new Vector2(1, 1), new Color(0, 0, 0) * 0.7f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private void DrawInventoryUI(SpriteBatch spriteBatch)
    {
        // Dim background
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
            new Color(0, 0, 0) * 0.8f);

        int panelW = 820;
        int panelH = 580;
        int panelX = (Game1.ScreenWidth - panelW) / 2;
        int panelY = (Game1.ScreenHeight - panelH) / 2;

        // Panel background with layered borders
        spriteBatch.Draw(_pixel, new Rectangle(panelX - 2, panelY - 2, panelW + 4, panelH + 4), new Color(60, 50, 30));
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(20, 16, 10));
        DrawRectOutline(spriteBatch, new Rectangle(panelX, panelY, panelW, panelH), new Color(120, 100, 60), 2);

        // Tab buttons
        int tabW = 120;
        int tabH = 28;
        int tabY = panelY + 10;
        // Items tab
        var itemTabColor = !_inventoryStatsTab ? new Color(60, 50, 30) : new Color(30, 25, 15);
        spriteBatch.Draw(_pixel, new Rectangle(panelX + 16, tabY, tabW, tabH), itemTabColor);
        DrawRectOutline(spriteBatch, new Rectangle(panelX + 16, tabY, tabW, tabH),
            !_inventoryStatsTab ? new Color(200, 180, 100) : new Color(80, 70, 45), 1);
        string itemTabText = SanitizeForFont(Fonts.Game, "아이템");
        DrawTextWithShadow(spriteBatch, Fonts.Game, itemTabText,
            new Vector2(panelX + 40, tabY + 4),
            !_inventoryStatsTab ? new Color(255, 240, 180) : new Color(140, 130, 100), 0.65f);
        // Stats tab
        var statsTabColor = _inventoryStatsTab ? new Color(60, 50, 30) : new Color(30, 25, 15);
        spriteBatch.Draw(_pixel, new Rectangle(panelX + 16 + tabW + 4, tabY, tabW, tabH), statsTabColor);
        DrawRectOutline(spriteBatch, new Rectangle(panelX + 16 + tabW + 4, tabY, tabW, tabH),
            _inventoryStatsTab ? new Color(200, 180, 100) : new Color(80, 70, 45), 1);
        string statsTabText = SanitizeForFont(Fonts.Game, "스탯");
        DrawTextWithShadow(spriteBatch, Fonts.Game, statsTabText,
            new Vector2(panelX + 16 + tabW + 30, tabY + 4),
            _inventoryStatsTab ? new Color(255, 240, 180) : new Color(140, 130, 100), 0.65f);
        // Tab hint
        string tabHint = SanitizeForFont(Fonts.Game, "[TAB] 전환");
        DrawTextWithShadow(spriteBatch, Fonts.Game, tabHint,
            new Vector2(panelX + 16 + (tabW + 4) * 2 + 10, tabY + 6),
            new Color(120, 110, 80), 0.5f);

        // Divider under tabs
        spriteBatch.Draw(_pixel, new Rectangle(panelX + 16, panelY + 42, panelW - 32, 1), new Color(100, 80, 50) * 0.6f);

        // Slot count
        string slotInfo = SanitizeForFont(Fonts.Game, $"{_inventory.UsedSlots}/{Inventory.MaxSlots} 슬롯");
        DrawTextWithShadow(spriteBatch, Fonts.Game, slotInfo,
            new Vector2(panelX + panelW - 160, tabY + 4),
            new Color(200, 190, 150), 0.65f);

        int listY = panelY + 52;

        if (_inventoryStatsTab)
        {
            DrawStatsTab(spriteBatch, panelX, listY, panelW, panelH - 88);
        }
        else
        {

        var items = _inventory.GetSortedItems();
        int listX = panelX + 20;
        int itemH = 34;
        int listW = 380;

        // List background
        spriteBatch.Draw(_pixel, new Rectangle(listX - 4, listY - 4, listW + 8, panelH - 100), new Color(12, 10, 6));
        DrawRectOutline(spriteBatch, new Rectangle(listX - 4, listY - 4, listW + 8, panelH - 100), new Color(60, 50, 35), 1);

        // Item list (left side)
        for (int i = 0; i < items.Count; i++)
        {
            var (id, count) = items[i];
            var info = MeteoriteDatabase.Get(id);
            int iy = listY + i * itemH;
            bool selected = i == _inventorySelectedSlot;

            // Selection highlight
            if (selected)
            {
                spriteBatch.Draw(_pixel, new Rectangle(listX, iy, listW, itemH - 2),
                    new Color(70, 55, 30));
                // Selection indicator
                spriteBatch.Draw(_pixel, new Rectangle(listX, iy, 4, itemH - 2),
                    new Color(255, 220, 120));
            }

            // Rarity color bar
            var rarityColor = MeteoriteDatabase.RarityColor(info.Rarity);
            if (!selected)
                spriteBatch.Draw(_pixel, new Rectangle(listX, iy, 3, itemH - 2), rarityColor * 0.7f);

            // Icon background
            spriteBatch.Draw(_pixel, new Rectangle(listX + 10, iy + 4, 22, 22), new Color(30, 25, 18));
            DrawRectOutline(spriteBatch, new Rectangle(listX + 10, iy + 4, 22, 22), rarityColor * 0.5f, 1);
            // Icon
            spriteBatch.Draw(_pixel, new Rectangle(listX + 14, iy + 8, 14, 14), info.MainColor * 0.7f);
            spriteBatch.Draw(_pixel, new Rectangle(listX + 17, iy + 11, 8, 8), Color.White * 0.35f);

            // Name
            string name = SanitizeForFont(Fonts.Game, info.Name);
            DrawTextWithShadow(spriteBatch, Fonts.Game, name,
                new Vector2(listX + 40, iy + 5),
                selected ? new Color(255, 245, 210) : new Color(210, 200, 180),
                0.65f);

            // Count for stackables
            if (info.Stackable && count > 1)
            {
                string countStr = $"x{count}";
                DrawTextWithShadow(spriteBatch, Fonts.Game, countStr,
                    new Vector2(listX + listW - 50, iy + 5),
                    new Color(220, 220, 160), 0.6f);
            }

            // Non-stackable indicator
            if (!info.Stackable)
            {
                spriteBatch.Draw(_pixel, new Rectangle(listX + listW - 20, iy + 10, 8, 8), rarityColor * 0.4f);
            }
        }

        if (items.Count == 0)
        {
            string empty = SanitizeForFont(Fonts.Game, "운석이 없습니다");
            DrawTextWithShadow(spriteBatch, Fonts.Game, empty,
                new Vector2(listX + 60, listY + 40),
                new Color(140, 120, 90), 0.7f);
        }

        // Right panel: selected item details
        int detailX = panelX + listW + 48;
        int detailY = listY;
        int detailW = panelW - listW - 68;

        // Detail panel background
        spriteBatch.Draw(_pixel, new Rectangle(detailX - 8, detailY - 4, detailW + 16, 240), new Color(25, 20, 14));
        DrawRectOutline(spriteBatch, new Rectangle(detailX - 8, detailY - 4, detailW + 16, 240), new Color(80, 65, 40), 1);

        if (items.Count > 0 && _inventorySelectedSlot < items.Count)
        {
            var (selId, selCount) = items[_inventorySelectedSlot];
            var selInfo = MeteoriteDatabase.Get(selId);
            var selRarityColor = MeteoriteDatabase.RarityColor(selInfo.Rarity);

            // Large icon
            spriteBatch.Draw(_pixel, new Rectangle(detailX + 4, detailY + 10, 32, 32), new Color(15, 12, 8));
            DrawRectOutline(spriteBatch, new Rectangle(detailX + 4, detailY + 10, 32, 32), selRarityColor * 0.6f, 1);
            spriteBatch.Draw(_pixel, new Rectangle(detailX + 10, detailY + 16, 20, 20), selInfo.MainColor * 0.8f);
            spriteBatch.Draw(_pixel, new Rectangle(detailX + 14, detailY + 20, 12, 12), Color.White * 0.4f);

            // Name
            string selName = SanitizeForFont(Fonts.Game, selInfo.Name);
            DrawTextWithShadow(spriteBatch, Fonts.Game, selName,
                new Vector2(detailX + 44, detailY + 10),
                selRarityColor, 0.85f);

            // Rarity label
            string rarityStr = SanitizeForFont(Fonts.Game, $"[{MeteoriteDatabase.RarityName(selInfo.Rarity)}] {(selInfo.Stackable ? "중첩 가능" : "고유")}");
            DrawTextWithShadow(spriteBatch, Fonts.Game, rarityStr,
                new Vector2(detailX + 44, detailY + 38),
                new Color(180, 170, 140), 0.6f);

            // Divider
            spriteBatch.Draw(_pixel, new Rectangle(detailX, detailY + 62, detailW, 1), new Color(80, 65, 40) * 0.5f);

            // Description
            string desc = SanitizeForFont(Fonts.Game, selInfo.Description);
            DrawTextWithShadow(spriteBatch, Fonts.Game, desc,
                new Vector2(detailX + 8, detailY + 72),
                new Color(240, 230, 200), 0.7f);

            if (selInfo.Stackable && selCount > 1)
            {
                string stackStr = SanitizeForFont(Fonts.Game, $"보유: {selCount}개");
                DrawTextWithShadow(spriteBatch, Fonts.Game, stackStr,
                    new Vector2(detailX + 8, detailY + 104),
                    new Color(220, 210, 170), 0.65f);
            }

            // Destroy prompt
            int destroyY = detailY + 195;
            spriteBatch.Draw(_pixel, new Rectangle(detailX, destroyY - 4, detailW, 1), new Color(80, 65, 40) * 0.4f);

            if (_inventoryDestroyConfirm)
            {
                spriteBatch.Draw(_pixel, new Rectangle(detailX, destroyY, detailW, 28), new Color(80, 20, 15));
                string confirm = SanitizeForFont(Fonts.Game, "X를 다시 눌러 파괴 확인");
                DrawTextWithShadow(spriteBatch, Fonts.Game, confirm,
                    new Vector2(detailX + 8, destroyY + 4),
                    new Color(255, 120, 80), 0.65f);
            }
            else
            {
                string destroyHint = SanitizeForFont(Fonts.Game, "[X] 파괴");
                DrawTextWithShadow(spriteBatch, Fonts.Game, destroyHint,
                    new Vector2(detailX + 8, destroyY + 4),
                    new Color(180, 130, 100), 0.6f);
            }
        }

        // Synergies panel
        int synY = detailY + 254;
        var activeSynergies = SynergySystem.GetActiveSynergies(_inventory);

        spriteBatch.Draw(_pixel, new Rectangle(detailX - 8, synY - 8, detailW + 16, panelH - 100 - (synY - listY) + 4), new Color(25, 20, 14));
        DrawRectOutline(spriteBatch, new Rectangle(detailX - 8, synY - 8, detailW + 16, panelH - 100 - (synY - listY) + 4), new Color(80, 65, 40), 1);

        string synTitle = SanitizeForFont(Fonts.Game, "활성 시너지");
        DrawTextWithShadow(spriteBatch, Fonts.Game, synTitle,
            new Vector2(detailX, synY),
            new Color(255, 230, 120), 0.7f);
        synY += 28;

        if (activeSynergies.Count == 0)
        {
            string noSyn = SanitizeForFont(Fonts.Game, "없음");
            DrawTextWithShadow(spriteBatch, Fonts.Game, noSyn,
                new Vector2(detailX + 8, synY),
                new Color(130, 110, 80), 0.6f);
        }
        else
        {
            foreach (var syn in activeSynergies)
            {
                string synName = SanitizeForFont(Fonts.Game, $"★ {syn.Name}");
                DrawTextWithShadow(spriteBatch, Fonts.Game, synName,
                    new Vector2(detailX + 8, synY),
                    syn.Color, 0.65f);
                synY += 22;
                string synDesc = SanitizeForFont(Fonts.Game, $"  {syn.Description}");
                DrawTextWithShadow(spriteBatch, Fonts.Game, synDesc,
                    new Vector2(detailX + 8, synY),
                    new Color(200, 190, 160), 0.55f);
                synY += 22;
            }
        }

        } // end items tab

        // Controls hint bar at bottom
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY + panelH - 36, panelW, 36), new Color(30, 25, 16));
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY + panelH - 36, panelW, 1), new Color(100, 80, 50) * 0.5f);
        string hints = _inventoryStatsTab
            ? SanitizeForFont(Fonts.Game, "[W/S] 스크롤    [TAB] 아이템    [I / ESC] 닫기")
            : SanitizeForFont(Fonts.Game, "[W/S] 선택    [X] 파괴    [TAB] 스탯    [I / ESC] 닫기");
        var hintsSize = Fonts.Game.MeasureString(hints);
        DrawTextWithShadow(spriteBatch, Fonts.Game, hints,
            new Vector2(panelX + panelW / 2f - hintsSize.X * 0.6f / 2f, panelY + panelH - 28),
            new Color(180, 170, 140), 0.6f);
    }
}
