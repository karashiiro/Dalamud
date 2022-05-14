using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// Chat events and public helper functions.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IChatHandlers
{
    /// <summary>
    /// Gets the last URL seen in chat.
    /// </summary>
    string? LastLink { get; }
}
