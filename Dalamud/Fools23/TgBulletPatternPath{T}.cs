using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Fools23.MotionContexts;
using Dalamud.Fools23.Objects;

namespace Dalamud.Fools23;

public class TgBulletPatternPath<TContext> : TgBulletPatternPath where TContext : MotionContext, new()
{
    public Func<float, TContext, Vector2> Velocity { get; init; }

    public Func<float, Vector2> Origin { get; init; }

    public float FireInterval { get; init; }

    private float LastFired { get; set; }

    private float Age { get; set; }

    public override IEnumerable<TgBullet> Emit(float dt)
    {
        this.Age += dt;
        if (this.LastFired + this.FireInterval < this.Age)
        {
            this.LastFired = this.Age;
            yield return new TgBullet<TContext>(this.Origin(this.Age), this.Velocity);
        }
    }
}
