using System;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.ClientState;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IClientState
{
    /// <summary>
    /// Event that gets fired when the current Territory changes.
    /// </summary>
    event EventHandler<ushort> TerritoryChanged;

    /// <summary>
    /// Event that fires when a character is logging in.
    /// </summary>
    event EventHandler Login;

    /// <summary>
    /// Event that fires when a character is logging out.
    /// </summary>
    event EventHandler Logout;

    /// <summary>
    /// Event that fires when a character is entering PvP.
    /// </summary>
    event System.Action EnterPvP;

    /// <summary>
    /// Event that fires when a character is leaving PvP.
    /// </summary>
    event System.Action LeavePvP;

    /// <summary>
    /// Event that gets fired when a duty is ready.
    /// </summary>
    event EventHandler<ContentFinderCondition> CfPop;

    /// <summary>
    /// Gets the language of the client.
    /// </summary>
    ClientLanguage ClientLanguage { get; }

    /// <summary>
    /// Gets the current Territory the player resides in.
    /// </summary>
    ushort TerritoryType { get; }

    /// <summary>
    /// Gets the local player character, if one is present.
    /// </summary>
    PlayerCharacter? LocalPlayer { get; }

    /// <summary>
    /// Gets the content ID of the local character.
    /// </summary>
    ulong LocalContentId { get; }

    /// <summary>
    /// Gets a value indicating whether a character is logged in.
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// Gets a value indicating whether or not the user is playing PvP.
    /// </summary>
    bool IsPvP { get; }

    /// <summary>
    /// Enable this module.
    /// </summary>
    void Enable();
}
