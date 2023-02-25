using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Dalamud.Fools23.Objects;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.Fools23;

public enum TgGameState
{
    Init,
    Intro,
}

public class TippyGameScene : Window
{
    private List<TgObject> objects = new();

    private TgTexturePile pile = new();
    private Stopwatch clock = new();

    private bool drawCollision = true;

    public TgGameState State { get; private set; } = TgGameState.Init;

    public long MsSinceStart => this.clock.ElapsedMilliseconds;

    public TippyGameScene()
        : base("Tippy Game", ImGuiWindowFlags.NoResize, true)
    {
        this.Size = new Vector2(800, 400);
        this.SizeCondition = ImGuiCond.Always;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        base.PostDraw();

        ImGui.PopStyleVar(1);
    }

    public override void Draw()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Debug"))
            {
                ImGui.MenuItem("Draw collision", string.Empty, ref this.drawCollision);

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        foreach (var tgObject in this.objects)
        {
            tgObject.Draw();
        }

        // TODO: tick updates at constant rate
        // TODO: check collisions, draw if enabled, call trigger, move out if applicable
    }

    public T? GetObj<T>() where T : TgObject
    {
        return this.objects.First(x => x is T) as T;
    }

    public void UpdateTimeline()
    {
        switch (this.State)
        {
            case TgGameState.Init:
                this.clock.Restart();
                this.objects.Clear();

                this.objects.Add(new TgPlayer());
                this.objects.Add(new TgIntroTippy());

                // Play intro
                this.State = TgGameState.Intro;
                break;
            case TgGameState.Intro:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
