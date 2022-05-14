using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Network;

/// <summary>
/// This interface handles interacting with game network events.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IGameNetwork
{
    /// <summary>
    /// Event that is called when a network message is sent/received.
    /// </summary>
    event GameNetwork.OnNetworkMessageDelegate NetworkMessage;

    /// <summary>
    /// Enable this module.
    /// </summary>
    void Enable();
}
