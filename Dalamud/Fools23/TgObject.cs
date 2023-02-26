using System.Numerics;

namespace Dalamud.Fools23;

public abstract class TgObject
{
    protected Vector2 Position { get; set; } = Vector2.Zero;

    protected SimpleRect CollisionRect { get; set; } = default;

    protected TgCollideType CollisionType { get; set; } = TgCollideType.None;

    public virtual void OnCollisionTrigger(TgObject target, TippyGameScene scene) { }

    public bool DidCollideWith(TgObject other)
    {
        var thisRect = this.GetCollisionRectWorld();
        var otherRect = other.GetCollisionRectWorld();
        return thisRect.IntersectsWith(otherRect);
    }

    public bool IsInViewport(Vector2 viewport)
    {
        var thisRect = this.GetCollisionRectWorld();
        var viewportRect = new SimpleRect(0, 0, viewport.X, viewport.Y);
        return thisRect.IntersectsWith(viewportRect);
    }

    public abstract void Draw();

    public abstract void Update(float dt);

    private SimpleRect GetCollisionRectWorld()
    {
        // Collision rects are centered on the object's position
        return this.CollisionRect.RelativeTo(this.Position);
    }
}
