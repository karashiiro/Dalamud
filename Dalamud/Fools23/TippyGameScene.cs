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
    GameOver,
}

public class TippyGameScene : Window
{
    private List<TgObject> objects = new();

    private TgTexturePile pile = new();
    private Stopwatch clock = new();
    private Vector2 viewport = new(800, 400);

    private bool drawCollision = true;

    public TgGameState State { get; private set; } = TgGameState.Init;

    public long MsSinceStart => this.clock.ElapsedMilliseconds;

    public long LastUpdate { get; set; }

    public TippyGameScene()
        : base("Tippy Game", ImGuiWindowFlags.NoResize, true)
    {
        this.Size = this.viewport;
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

        this.UpdateTimeline();

        // Compute time delta in seconds
        var sinceStart = this.MsSinceStart;
        var dt = 0.001f * (sinceStart - this.LastUpdate);
        this.LastUpdate = sinceStart;

        foreach (var tgObject in this.objects)
        {
            if (tgObject.IsInViewport(this.viewport))
            {
                tgObject.Draw();
            }

            tgObject.Update(dt);

            // TODO: This might need to be indexed, but that can come when it comes
            foreach (var otherObject in this.objects.Where(o => !ReferenceEquals(o, tgObject)))
            {
                if (tgObject.DidCollideWith(otherObject))
                {
                    tgObject.OnCollisionTrigger(otherObject, this);
                }
            }
        }
    }

    public void GameOver()
    {
        this.State = TgGameState.GameOver;
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
            case TgGameState.GameOver:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
