using System.Numerics;

namespace Dalamud.Fools23;

public abstract class TgObject
{
    public Vector2 Position => Vector2.Zero;

    public Vector4 CollisionRect => Vector4.Zero;

    public TgCollideType CollisionType => TgCollideType.None;

    public virtual void OnCollisionTrigger()
    {
    }

    public abstract void Draw();

    public abstract void Update(float dt);
}
