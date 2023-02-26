using System;
using System.Numerics;

namespace Dalamud.Fools23.Objects;

public class TgBullet : TgAnimSpriteObject
{
    public TgBullet(Vector2 origin, Func<float, Vector2> velocityFn)
    {
        this.Position = origin;
        this.CollisionRect = new SimpleRect(0, 0, 3, 3);
        this.VelocityAt = velocityFn;
    }

    private Func<float, Vector2> VelocityAt { get; set; }

    public override void Update(float dt)
    {
        this.Position = this.GetNextPosition(dt);
    }

    private Vector2 GetNextPosition(float dt)
    {
        var dX = this.VelocityAt(dt);
        return this.Position + dX;
    }
}
