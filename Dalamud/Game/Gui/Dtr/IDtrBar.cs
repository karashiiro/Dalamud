using System;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Interface to the server info bar.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IDtrBar
{
    /// <summary>
    /// Get a DTR bar entry.
    /// This allows you to add your own text, and users to sort it.
    /// </summary>
    /// <param name="title">A user-friendly name for sorting.</param>
    /// <param name="text">The text the entry shows.</param>
    /// <returns>The entry object used to update, hide and remove the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when an entry with the specified title exists.</exception>
    public DtrBarEntry Get(string title, SeString? text = null);
}
