using System;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Fools23.Objects;

public class TgPlayer : TgAnimSpriteObject
{
    private const float TwoPi = 2 * MathF.PI;

    public TgPlayer()
    {
        this.Position = new Vector2(100, 100);
        this.CollisionRect = new SimpleRect(20, 20, 10, 10);
    }

    private float Theta { get; set; }

    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        ImGui.GetWindowDrawList().AddCircleFilled(windowPos + this.Position, 3, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f)));
    }

    public override void Update(float dt)
    {
        // Move in a circle at a rate of 2pi rads per second
        var deltaTheta = TwoPi * dt;
        var theta = this.Theta + deltaTheta;
        while (theta >= TwoPi)
            theta -= TwoPi;
        this.Position += this.GetPosition(theta) - this.GetPosition(this.Theta);
        this.Theta = theta;
    }

    public Vector2 GetPosition(float t)
    {
        // Follow a circular path of radius 20
        var r = 20;
        var x = r * MathF.Cos(t);
        var y = r * MathF.Sin(t);
        return new Vector2(x, y);
    }

    public override void OnCollisionTrigger(TgObject target, TippyGameScene scene)
    {
        if (target is TgBullet)
        {
            scene.GameOver();
        }
    }
}
