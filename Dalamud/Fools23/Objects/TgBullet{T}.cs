using System;
using System.Numerics;

using Dalamud.Fools23.MotionContexts;

namespace Dalamud.Fools23.Objects;

public class TgBullet<TContext> : TgBullet where TContext : MotionContext, new()
{
    public TgBullet(Vector2 origin, Func<float, TContext, Vector2> velocityFn)
    {
        this.Position = origin;
        this.CollisionRect = new SimpleRect(0, 0, 3, 3);
        this.VelocityAt = velocityFn;
        this.Context = new TContext();
    }

    private Func<float, TContext, Vector2> VelocityAt { get; set; }

    private TContext Context { get; set; }

    public override void Update(float dt)
    {
        this.Position = this.GetNextPosition(dt);
    }

    private Vector2 GetNextPosition(float dt)
    {
        var dX = this.VelocityAt(dt, this.Context);
        return this.Position + dX;
    }
}
