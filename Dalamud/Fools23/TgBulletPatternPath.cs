using System.Collections.Generic;

using Dalamud.Fools23.Objects;

namespace Dalamud.Fools23;

public abstract class TgBulletPatternPath
{
    public abstract IEnumerable<TgBullet> Emit(float dt);
}
