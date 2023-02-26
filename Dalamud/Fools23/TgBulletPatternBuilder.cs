using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Fools23.MotionContexts;

namespace Dalamud.Fools23;

public class TgBulletPatternBuilder
{
    private readonly IList<TgBulletPatternPath> paths;

    public TgBulletPatternBuilder()
    {
        this.paths = new List<TgBulletPatternPath>();
    }

    public TgBulletPatternBuilder WithPath<TContext>(Func<float, Vector2> origin, Func<float, TContext, Vector2> velocityFn, float fireInterval) where TContext : MotionContext, new()
    {
        this.paths.Add(new TgBulletPatternPath<TContext> { Origin = origin, FireInterval = fireInterval, Velocity = velocityFn });
        return this;
    }

    public TgBulletPattern Build()
    {
        return new TgBulletPattern(this.paths);
    }

    public static TgBulletPattern Example()
    {
        return new TgBulletPatternBuilder()
               .WithPath<MotionContext.Empty>(_ => new Vector2(20, 40), (dt, _) => dt * new Vector2(5, 5), 1000)
               .WithPath<AngularMotionContext>(
                   _ => new Vector2(400, 40),
                   (dt, ctx) =>
                   {
                       Vector2 Fn(float r, float t) => r * new Vector2(MathF.Cos(t), MathF.Sin(t));

                       // Move in a circle at a rate of 2pi rads per second
                       var circle = 2 * MathF.PI;
                       var radius = 5;
                       var deltaTheta = circle * dt;
                       var theta = ctx.Theta + deltaTheta;
                       while (theta >= circle)
                           theta -= circle;
                       var dX = Fn(radius, theta) - Fn(radius, ctx.Theta);
                       ctx.Theta = theta;
                       return dX;
                   },
                   1000)
               .WithPath<MotionContext.Empty>(_ => new Vector2(780, 40), (dt, _) => dt * new Vector2(-5, 5), 1000)
               .Build();
    }
}
