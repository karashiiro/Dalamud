using System;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using ImGuiScene;

namespace Dalamud.Interface;

/// <summary>
/// Interface responsible for managing elements in the title screen menu.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface ITitleScreenMenu
{
    /// <summary>
    /// Gets the list of entries in the title screen menu.
    /// </summary>
    IReadOnlyList<TitleScreenMenu.TitleScreenMenuEntry> Entries { get; }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    TitleScreenMenu.TitleScreenMenuEntry AddEntry(string text, TextureWrap texture, Action onTriggered);

    /// <summary>
    /// Remove an entry from the title screen menu.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    void RemoveEntry(TitleScreenMenu.TitleScreenMenuEntry entry);
}
