using System;
using ImGuiScene;

namespace Dalamud.Fools23;

public class TgTexturePile : IDisposable
{
    public static TgTexturePile Instance { get; private set; }

    public TgTexturePile()
    {
        Instance = this;
    }

    public TextureWrap GetTexture(string name)
    {
        return null;
    }

    public void Dispose()
    {
        Instance = null;
    }
}
