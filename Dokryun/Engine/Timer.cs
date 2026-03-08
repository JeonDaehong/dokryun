namespace Dokryun.Engine;

public class Timer
{
    public float Duration { get; set; }
    public bool IsFinished => _elapsed >= Duration;
    public float Progress => Duration > 0 ? Math.Min(_elapsed / Duration, 1f) : 1f;

    private float _elapsed;

    public Timer(float duration)
    {
        Duration = duration;
    }

    public void Update(float deltaTime)
    {
        if (!IsFinished)
            _elapsed += deltaTime;
    }

    public void Reset()
    {
        _elapsed = 0;
    }

    public void Reset(float newDuration)
    {
        Duration = newDuration;
        _elapsed = 0;
    }
}
