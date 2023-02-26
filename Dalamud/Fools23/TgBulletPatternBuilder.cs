using System;
using System.Collections.Generic;
using System.Numerics;

namespace Dalamud.Fools23;

public class TgBulletPatternBuilder
{
    private readonly IList<TgBulletPatternPath> paths;

    public TgBulletPatternBuilder()
    {
        this.paths = new List<TgBulletPatternPath>();
    }

    public TgBulletPatternBuilder WithPath(Vector2 origin, Func<float, Vector2> velocityFn, float fireInterval)
    {
        this.paths.Add(new TgBulletPatternPath { Origin = origin, FireInterval = fireInterval, Velocity = velocityFn });
        return this;
    }

    public TgBulletPattern Build()
    {
        return new TgBulletPattern(this.paths);
    }

    public static TgBulletPattern Example()
    {
        return new TgBulletPatternBuilder()
               .WithPath(new Vector2(20, 40), dt => dt * new Vector2(5, 5), 1000)
               .WithPath(new Vector2(400, 40), dt => dt * new Vector2(5 + MathF.Cos(dt), 5 + MathF.Sin(dt)), 1000)
               .WithPath(new Vector2(780, 40), dt => dt * new Vector2(-5, 5), 1000)
               .Build();
    }
}
