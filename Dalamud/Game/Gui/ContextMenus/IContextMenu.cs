using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// Provides an interface to modify context menus.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IContextMenu
{
    /// <summary>
    /// Occurs when a context menu is opened by the game.
    /// </summary>
    event ContextMenuOpenedDelegate? ContextMenuOpened;
}
