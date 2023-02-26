using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Fools23.Objects;

namespace Dalamud.Fools23;

public class TgBulletPatternPath
{
    public Func<float, Vector2> Velocity { get; init; }

    public Vector2 Origin { get; init; }

    public float FireInterval { get; init; }

    private float LastFired { get; set; }

    private float Age { get; set; }

    public IEnumerable<TgBullet> Emit(float dt)
    {
        this.Age += dt;
        if (this.LastFired + this.FireInterval < this.Age)
        {
            this.LastFired = this.Age;
            yield return new TgBullet(this.Origin, this.Velocity);
        }
    }
}
