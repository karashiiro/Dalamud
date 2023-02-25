using ImGuiNET;

namespace Dalamud.Fools23.Objects;

public class TgAnimSpriteObject : TgObject
{
    public override void Draw()
    {
        ImGui.SetCursorPos(this.Position);
        ImGui.Text("O");
    }

    public override void Update(float dt)
    {
        
    }
}
