using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Entities;

public abstract class Entity
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsActive { get; set; } = true;
    public float Depth => Position.Y; // 2.5D: Y축 기준 깊이 정렬

    public Rectangle Bounds { get; protected set; }

    public virtual void Update(GameTime gameTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }

    public bool CollidesWith(Entity other)
    {
        return Bounds.Intersects(other.Bounds);
    }

    protected void UpdateBounds(int width, int height)
    {
        Bounds = new Rectangle(
            (int)(Position.X - width / 2f),
            (int)(Position.Y - height / 2f),
            width,
            height
        );
    }
}
