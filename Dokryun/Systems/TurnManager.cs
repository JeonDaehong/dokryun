using Dokryun.Entities;

namespace Dokryun.Systems;

public enum TurnPhase
{
    PlayerChoose,
    PlayerLunge,    // 플레이어가 적에게 돌진
    PlayerImpact,   // 타격 순간 (히트프리즈)
    PlayerReturn,   // 원래 위치로 복귀
    EnemyAct,
    EnemyLunge,     // 적이 플레이어에게 돌진
    EnemyImpact,
    EnemyReturn,
    TurnEnd
}

public class TurnManager
{
    public TurnPhase Phase { get; private set; } = TurnPhase.PlayerChoose;
    public int TurnCount { get; private set; } = 1;
    public int CurrentEnemyIndex { get; private set; }

    private float _animTimer;

    // Timing constants
    private const float LungeDuration = 0.15f;
    private const float ImpactFreeze = 0.08f;
    private const float ReturnDuration = 0.2f;

    private Character _player;
    private List<Enemy> _enemies;

    // Action tracking
    public string LastActionText { get; private set; } = "";
    public int? SelectedTarget { get; set; }
    public int LastDamage { get; private set; }
    public int LastTargetIndex { get; private set; } = -1;
    public bool LastAttackerIsPlayer { get; private set; }

    // Animation progress (0~1)
    public float AnimProgress => Phase switch
    {
        TurnPhase.PlayerLunge or TurnPhase.EnemyLunge => Math.Min(1f, _animTimer / LungeDuration),
        TurnPhase.PlayerImpact or TurnPhase.EnemyImpact => Math.Min(1f, _animTimer / ImpactFreeze),
        TurnPhase.PlayerReturn or TurnPhase.EnemyReturn => Math.Min(1f, _animTimer / ReturnDuration),
        _ => 0f
    };

    public TurnManager(Character player, List<Enemy> enemies)
    {
        _player = player;
        _enemies = enemies;
    }

    public void PlayerAttack()
    {
        if (Phase != TurnPhase.PlayerChoose) return;

        Enemy target = null;
        int targetIdx = -1;

        if (SelectedTarget.HasValue && SelectedTarget.Value < _enemies.Count
            && _enemies[SelectedTarget.Value].IsAlive)
        {
            targetIdx = SelectedTarget.Value;
            target = _enemies[targetIdx];
        }
        else
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive) { target = _enemies[i]; targetIdx = i; break; }
            }
        }

        if (target == null) return;

        LastTargetIndex = targetIdx;
        LastAttackerIsPlayer = true;
        SelectedTarget = null;

        // Start lunge (damage applied at impact)
        Phase = TurnPhase.PlayerLunge;
        _animTimer = 0f;
    }

    public void Update(float dt)
    {
        _animTimer += dt;

        switch (Phase)
        {
            case TurnPhase.PlayerLunge:
                if (_animTimer >= LungeDuration)
                {
                    // Apply damage at impact
                    var target = _enemies[LastTargetIndex];
                    LastDamage = target.TakeDamage(_player.Atk);
                    LastActionText = $"{_player.Name}의 공격! {target.Name}에게 {LastDamage} 데미지!";
                    Phase = TurnPhase.PlayerImpact;
                    _animTimer = 0f;
                }
                break;

            case TurnPhase.PlayerImpact:
                if (_animTimer >= ImpactFreeze)
                {
                    Phase = TurnPhase.PlayerReturn;
                    _animTimer = 0f;
                }
                break;

            case TurnPhase.PlayerReturn:
                if (_animTimer >= ReturnDuration)
                {
                    if (!_enemies.Exists(e => e.IsAlive))
                    {
                        LastActionText = "전투 승리!";
                        Phase = TurnPhase.TurnEnd;
                        return;
                    }
                    CurrentEnemyIndex = 0;
                    Phase = TurnPhase.EnemyAct;
                }
                break;

            case TurnPhase.EnemyAct:
                while (CurrentEnemyIndex < _enemies.Count && !_enemies[CurrentEnemyIndex].IsAlive)
                    CurrentEnemyIndex++;

                if (CurrentEnemyIndex >= _enemies.Count)
                {
                    TurnCount++;
                    Phase = TurnPhase.PlayerChoose;
                    return;
                }

                LastTargetIndex = CurrentEnemyIndex;
                LastAttackerIsPlayer = false;
                Phase = TurnPhase.EnemyLunge;
                _animTimer = 0f;
                break;

            case TurnPhase.EnemyLunge:
                if (_animTimer >= LungeDuration)
                {
                    var enemy = _enemies[CurrentEnemyIndex];
                    LastDamage = _player.TakeDamage(enemy.Atk);
                    LastActionText = $"{enemy.Name}의 공격! {_player.Name}에게 {LastDamage} 데미지!";
                    Phase = TurnPhase.EnemyImpact;
                    _animTimer = 0f;
                }
                break;

            case TurnPhase.EnemyImpact:
                if (_animTimer >= ImpactFreeze)
                {
                    Phase = TurnPhase.EnemyReturn;
                    _animTimer = 0f;
                }
                break;

            case TurnPhase.EnemyReturn:
                if (_animTimer >= ReturnDuration)
                {
                    if (!_player.IsAlive)
                    {
                        LastActionText = "패배...";
                        Phase = TurnPhase.TurnEnd;
                        return;
                    }
                    CurrentEnemyIndex++;
                    Phase = TurnPhase.EnemyAct;
                    _animTimer = 0f;
                }
                break;
        }
    }
}
