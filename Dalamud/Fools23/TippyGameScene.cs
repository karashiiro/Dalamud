using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.Fools23;

public class TippyGameScene : Window
{
    private List<TgObject> objects = new();

    private TgTexturePile pile = new();

    private bool drawCollision = true;

    public TippyGameScene()
        : base("Tippy Game", ImGuiWindowFlags.NoResize, true)
    {
        this.Size = new Vector2(800, 400);
        this.SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Debug"))
            {
                ImGui.MenuItem("Draw collision", string.Empty, ref this.drawCollision);
            }
        }

        foreach (var tgObject in this.objects)
        {
            tgObject.Draw();
        }

        // TODO: tick updates at constant rate
        // TODO: check collisions, draw if enabled, call trigger, move out if applicable
    }
}
