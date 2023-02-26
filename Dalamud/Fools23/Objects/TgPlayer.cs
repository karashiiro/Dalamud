using System;
using System.Numerics;

using Dalamud.Logging;
using ImGuiNET;

namespace Dalamud.Fools23.Objects;

public class TgPlayer : TgAnimSpriteObject
{
    public TgPlayer()
    {
        this.Position = new Vector2(100, 100);
        this.CollisionRect = new SimpleRect(20, 20, 10, 10);
    }

    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        ImGui.GetWindowDrawList().AddCircleFilled(windowPos + this.Position, 5, ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 1f, 1f)));
    }

    public override void Update(float dt)
    {
    }

    public Vector2 GetNextPosition(float dt)
    {
        PluginLog.Log($"{dt}");
        return this.Position + (new Vector2(0, 5) * dt);
    }

    public override void OnCollisionTrigger(TgObject target, TippyGameScene scene)
    {
        if (target is TgBullet)
        {
            scene.GameOver();
        }
    }
}
