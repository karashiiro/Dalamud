using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IObjectTable : IReadOnlyCollection<GameObject>
{
    /// <summary>
    /// Gets the address of the object table.
    /// </summary>
    IntPtr Address { get; }

    /// <summary>
    /// Gets the length of the object table.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Get an object at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>An <see cref="GameObject"/> at the specified spawn index.</returns>
    GameObject? this[int index] { get; }

    /// <summary>
    /// Search for a game object by their Object ID.
    /// </summary>
    /// <param name="objectId">Object ID to find.</param>
    /// <returns>A game object or null.</returns>
    GameObject? SearchById(uint objectId);

    /// <summary>
    /// Gets the address of the game object at the specified index of the object table.
    /// </summary>
    /// <param name="index">The index of the object.</param>
    /// <returns>The memory address of the object.</returns>
    IntPtr GetObjectAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV game object.
    /// </summary>
    /// <param name="address">The address of the object in memory.</param>
    /// <returns><see cref="GameObject"/> object or inheritor containing the requested data.</returns>
    GameObject? CreateObjectReference(IntPtr address);
    
    
}
