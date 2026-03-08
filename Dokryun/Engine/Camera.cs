using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1f;
    public float SmoothSpeed { get; set; } = 8f;

    private readonly Viewport _viewport;
    private Vector2 _targetPosition;
    private float _shakeAmount;
    private float _shakeTimer;

    // Impact zoom
    private float _baseZoom = 1.5f;
    private float _zoomPunch;
    private float _zoomPunchDecay = 12f;

    // Directional shake
    private Vector2 _shakeDirection;
    private bool _directionalShake;

    // Slow motion
    public float TimeScale { get; private set; } = 1f;
    private float _slowMoTimer;
    private float _slowMoTarget = 1f;

    public Camera(Viewport viewport)
    {
        _viewport = viewport;
    }

    public void Follow(Vector2 target, float deltaTime)
    {
        _targetPosition = target;
        Position = Vector2.Lerp(Position, _targetPosition, SmoothSpeed * deltaTime);
    }

    public void Shake(float amount, float duration)
    {
        _shakeAmount = Math.Max(_shakeAmount, amount);
        _shakeTimer = Math.Max(_shakeTimer, duration);
        _directionalShake = false;
    }

    /// <summary>방향성 흔들림 - 타격 방향으로 강하게 흔들림</summary>
    public void DirectionalShake(Vector2 direction, float amount, float duration)
    {
        if (direction.LengthSquared() > 0) direction.Normalize();
        _shakeDirection = direction;
        _shakeAmount = Math.Max(_shakeAmount, amount);
        _shakeTimer = Math.Max(_shakeTimer, duration);
        _directionalShake = true;
    }

    /// <summary>임팩트 줌 - 타격 시 카메라 살짝 확대</summary>
    public void ImpactZoom(float punch)
    {
        _zoomPunch = Math.Max(_zoomPunch, punch);
    }

    /// <summary>슬로우 모션</summary>
    public void SlowMotion(float timeScale, float duration)
    {
        _slowMoTarget = timeScale;
        _slowMoTimer = duration;
        TimeScale = timeScale;
    }

    public void Update(float deltaTime)
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= deltaTime;
            _shakeAmount *= 0.9f; // decay shake
        }
        else
        {
            _shakeAmount = 0;
        }

        // Zoom punch decay
        if (_zoomPunch > 0.001f)
            _zoomPunch = MathHelper.Lerp(_zoomPunch, 0, _zoomPunchDecay * deltaTime);
        else
            _zoomPunch = 0;

        // Slow motion
        if (_slowMoTimer > 0)
        {
            _slowMoTimer -= deltaTime;
            if (_slowMoTimer <= 0)
                _slowMoTarget = 1f;
        }
        TimeScale = MathHelper.Lerp(TimeScale, _slowMoTarget, 10f * deltaTime);
    }

    public Matrix GetTransform()
    {
        var offset = Vector2.Zero;
        if (_shakeTimer > 0 && _shakeAmount > 0.1f)
        {
            if (_directionalShake)
            {
                float t = _shakeTimer > 0 ? 1f : 0f;
                offset = _shakeDirection * _shakeAmount * t +
                    new Vector2(
                        (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeAmount * 0.3f,
                        (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeAmount * 0.3f
                    );
            }
            else
            {
                offset = new Vector2(
                    (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeAmount,
                    (float)(Random.Shared.NextDouble() * 2 - 1) * _shakeAmount
                );
            }
        }

        float currentZoom = _baseZoom + _zoomPunch;

        return Matrix.CreateTranslation(
                   new Vector3(-Position.X + offset.X, -Position.Y + offset.Y, 0)) *
               Matrix.CreateScale(currentZoom) *
               Matrix.CreateTranslation(
                   new Vector3(_viewport.Width / 2f, _viewport.Height / 2f, 0));
    }
}
