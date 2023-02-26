using System.Collections.Generic;
using System.Linq;

using Dalamud.Fools23.Objects;

namespace Dalamud.Fools23;

public class TgBulletPattern
{
    private readonly IList<TgBulletPatternPath> paths;

    public TgBulletPattern(IList<TgBulletPatternPath> paths)
    {
        this.paths = paths;
    }

    public IEnumerable<TgBullet> Emit(float dt)
    {
        foreach (var bullet in this.paths.SelectMany(p => p.Emit(dt)))
        {
            yield return bullet;
        }
    }
}
