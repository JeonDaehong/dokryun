using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public struct DamageNumber
{
    public Vector2 Position;
    public string Text;
    public Color Color;
    public float Life;
    public float MaxLife;
    public bool IsCrit;
    public bool IsActive;

    public float Alpha => MaxLife > 0 ? Math.Max(0, Life / MaxLife) : 0;
    public float Progress => MaxLife > 0 ? 1f - (Life / MaxLife) : 1f;
}

public class DamageNumberSystem
{
    private DamageNumber[] _numbers;
    private static Texture2D _pixel;
    private const float FloatSpeed = 60f;

    public DamageNumberSystem(int max = 100)
    {
        _numbers = new DamageNumber[max];
    }

    public void Spawn(Vector2 position, float damage, bool isCrit)
    {
        for (int i = 0; i < _numbers.Length; i++)
        {
            if (!_numbers[i].IsActive)
            {
                float offsetX = (float)(Random.Shared.NextDouble() * 20 - 10);
                _numbers[i] = new DamageNumber
                {
                    Position = position + new Vector2(offsetX, -10),
                    Text = ((int)damage).ToString(),
                    Color = isCrit ? new Color(255, 220, 50) : Color.White,
                    Life = isCrit ? 1.0f : 0.7f,
                    MaxLife = isCrit ? 1.0f : 0.7f,
                    IsCrit = isCrit,
                    IsActive = true
                };
                return;
            }
        }
    }

    public void SpawnText(Vector2 position, string text, Color color, float duration = 0.8f)
    {
        for (int i = 0; i < _numbers.Length; i++)
        {
            if (!_numbers[i].IsActive)
            {
                float offsetX = (float)(Random.Shared.NextDouble() * 20 - 10);
                _numbers[i] = new DamageNumber
                {
                    Position = position + new Vector2(offsetX, -10),
                    Text = text,
                    Color = color,
                    Life = duration,
                    MaxLife = duration,
                    IsCrit = false,
                    IsActive = true
                };
                return;
            }
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < _numbers.Length; i++)
        {
            if (!_numbers[i].IsActive) continue;

            _numbers[i].Life -= deltaTime;
            _numbers[i].Position.Y -= FloatSpeed * deltaTime;

            if (_numbers[i].Life <= 0)
                _numbers[i].IsActive = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        EnsurePixel(spriteBatch.GraphicsDevice);

        for (int i = 0; i < _numbers.Length; i++)
        {
            if (!_numbers[i].IsActive) continue;

            ref var n = ref _numbers[i];
            float scale = n.IsCrit ? 1.5f : 1f;
            // Bounce effect at start
            if (n.Progress < 0.2f)
                scale *= 1f + (1f - n.Progress / 0.2f) * 0.5f;

            var color = n.Color * n.Alpha;
            DrawPixelText(spriteBatch, n.Text, n.Position, color, scale);
        }
    }

    private float Progress(int i) => 1f - (_numbers[i].Life / _numbers[i].MaxLife);

    /// <summary>Simple pixel font rendering (5x7 digits)</summary>
    private static void DrawPixelText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
    {
        float charWidth = 6 * scale;
        float totalWidth = text.Length * charWidth;
        var startPos = position - new Vector2(totalWidth / 2f, 0);

        for (int i = 0; i < text.Length; i++)
        {
            DrawPixelDigit(spriteBatch, text[i], startPos + new Vector2(i * charWidth, 0), color, scale);
        }
    }

    private static readonly bool[][,] DigitPatterns = new bool[][,]
    {
        // 0
        new bool[,] {{true,true,true},{true,false,true},{true,false,true},{true,false,true},{true,true,true}},
        // 1
        new bool[,] {{false,true,false},{true,true,false},{false,true,false},{false,true,false},{true,true,true}},
        // 2
        new bool[,] {{true,true,true},{false,false,true},{true,true,true},{true,false,false},{true,true,true}},
        // 3
        new bool[,] {{true,true,true},{false,false,true},{true,true,true},{false,false,true},{true,true,true}},
        // 4
        new bool[,] {{true,false,true},{true,false,true},{true,true,true},{false,false,true},{false,false,true}},
        // 5
        new bool[,] {{true,true,true},{true,false,false},{true,true,true},{false,false,true},{true,true,true}},
        // 6
        new bool[,] {{true,true,true},{true,false,false},{true,true,true},{true,false,true},{true,true,true}},
        // 7
        new bool[,] {{true,true,true},{false,false,true},{false,false,true},{false,false,true},{false,false,true}},
        // 8
        new bool[,] {{true,true,true},{true,false,true},{true,true,true},{true,false,true},{true,true,true}},
        // 9
        new bool[,] {{true,true,true},{true,false,true},{true,true,true},{false,false,true},{true,true,true}},
    };

    private static readonly Dictionary<char, bool[,]> LetterPatterns = new()
    {
        ['M'] = new bool[,] {{true,false,true},{true,true,true},{true,true,true},{true,false,true},{true,false,true}},
        ['I'] = new bool[,] {{true,true,true},{false,true,false},{false,true,false},{false,true,false},{true,true,true}},
        ['S'] = new bool[,] {{true,true,true},{true,false,false},{true,true,true},{false,false,true},{true,true,true}},
    };

    private static void DrawPixelDigit(SpriteBatch spriteBatch, char c, Vector2 pos, Color color, float scale)
    {
        bool[,] pattern;
        if (c >= '0' && c <= '9')
            pattern = DigitPatterns[c - '0'];
        else if (LetterPatterns.TryGetValue(c, out var lp))
            pattern = lp;
        else
            return;

        for (int y = 0; y < 5; y++)
        for (int x = 0; x < 3; x++)
        {
            if (pattern[y, x])
            {
                spriteBatch.Draw(_pixel,
                    new Rectangle((int)(pos.X + x * scale), (int)(pos.Y + y * scale), (int)scale, (int)scale),
                    color);
            }
        }
    }

    private static void EnsurePixel(GraphicsDevice device)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }
    }
}
