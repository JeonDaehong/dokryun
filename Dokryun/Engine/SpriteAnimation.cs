using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public class SpriteAnimation
{
    public Texture2D Texture { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public float FrameDuration { get; set; }
    public bool IsLooping { get; set; } = true;
    public int CurrentFrame { get; private set; }
    public bool IsFinished { get; private set; }

    private readonly int _totalFrames;
    private readonly int _row;
    private float _timer;

    public SpriteAnimation(Texture2D texture, int frameWidth, int frameHeight, int row, int frameCount, float frameDuration)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        _row = row;
        _totalFrames = frameCount;
        FrameDuration = frameDuration;
    }

    public void Update(float deltaTime)
    {
        if (IsFinished) return;

        _timer += deltaTime;
        if (_timer >= FrameDuration)
        {
            _timer -= FrameDuration;
            CurrentFrame++;

            if (CurrentFrame >= _totalFrames)
            {
                if (IsLooping)
                    CurrentFrame = 0;
                else
                {
                    CurrentFrame = _totalFrames - 1;
                    IsFinished = true;
                }
            }
        }
    }

    public void Reset()
    {
        CurrentFrame = 0;
        _timer = 0;
        IsFinished = false;
    }

    public Rectangle GetSourceRect()
    {
        return new Rectangle(CurrentFrame * FrameWidth, _row * FrameHeight, FrameWidth, FrameHeight);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, bool flipX = false, Color? color = null, float scale = 1f)
    {
        var effects = flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(
            Texture,
            position,
            GetSourceRect(),
            color ?? Color.White,
            0f,
            new Vector2(FrameWidth / 2f, FrameHeight / 2f),
            scale,
            effects,
            0f
        );
    }
}
