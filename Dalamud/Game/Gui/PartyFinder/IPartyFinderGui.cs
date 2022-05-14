using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui.PartyFinder;

/// <summary>
/// This interface handles interacting with the native PartyFinder window.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IPartyFinderGui
{
    /// <summary>
    /// Event fired each time the game receives an individual Party Finder listing.
    /// Cannot modify listings but can hide them.
    /// </summary>
    event PartyFinderGui.PartyFinderListingEventDelegate ReceiveListing;

    /// <summary>
    /// Enables this module.
    /// </summary>
    void Enable();
}
