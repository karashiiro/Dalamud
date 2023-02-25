using System.Numerics;

namespace Dalamud.Fools23;

public abstract class TgObject
{
    private Vector2 Position => Vector2.Zero;

    private Vector4 CollisionRect => Vector4.Zero;

    private TgCollideType CollisionType => TgCollideType.None;

    public virtual void OnCollisionTrigger()
    {
    }

    public abstract void Draw();

    public abstract void Update(float dt);
}
