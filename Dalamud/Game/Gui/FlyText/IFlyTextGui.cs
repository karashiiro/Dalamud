using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// This class facilitates interacting with and creating native in-game "fly text".
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IFlyTextGui
{
    /// <summary>
    /// The FlyText event that can be subscribed to.
    /// </summary>
    event FlyTextGui.OnFlyTextCreatedDelegate? FlyTextCreated;

    /// <summary>
    /// Displays a fly text in-game on the local player.
    /// </summary>
    /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
    /// <param name="actorIndex">The index of the actor to place flytext on. Indexing unknown. 1 places flytext on local player.</param>
    /// <param name="val1">Value1 passed to the native flytext function.</param>
    /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
    /// <param name="text1">Text1 passed to the native flytext function.</param>
    /// <param name="text2">Text2 passed to the native flytext function.</param>
    /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
    /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
    public void AddFlyText(
        FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon);
}
