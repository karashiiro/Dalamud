using System;
using System.Collections.Generic;
using System.IO;

using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace Dalamud.Plugin;

/// <summary>
/// This interface acts as an interface to the interface to various objects needed to interact with Dalamud and the game.
/// </summary>
public interface IDalamudPluginInterface
{
    /// <summary>
    /// Event that gets fired when loc is changed
    /// </summary>
    event DalamudPluginInterface.LanguageChangedDelegate LanguageChanged; // This is a circular reference, yes.

    /// <summary>
    /// Gets the reason this plugin was loaded.
    /// </summary>
    PluginLoadReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether this is a dev plugin.
    /// </summary>
    bool IsDev { get; }

    /// <summary>
    /// Gets the time that this plugin was loaded.
    /// </summary>
    DateTime LoadTime { get; }

    /// <summary>
    /// Gets the UTC time that this plugin was loaded.
    /// </summary>
    DateTime LoadTimeUTC { get; }

    /// <summary>
    /// Gets the timespan delta from when this plugin was loaded.
    /// </summary>
    TimeSpan LoadTimeDelta { get; }

    /// <summary>
    /// Gets the directory Dalamud assets are stored in.
    /// </summary>
    DirectoryInfo DalamudAssetDirectory { get; }

    /// <summary>
    /// Gets the location of your plugin assembly.
    /// </summary>
    FileInfo AssemblyLocation { get; }

    /// <summary>
    /// Gets the directory your plugin configurations are stored in.
    /// </summary>
    DirectoryInfo ConfigDirectory { get; }

    /// <summary>
    /// Gets the config file of your plugin.
    /// </summary>
    FileInfo ConfigFile { get; }

    /// <summary>
    /// Gets the <see cref="UiBuilder"/> instance which allows you to draw UI into the game via ImGui draw calls.
    /// </summary>
    UiBuilder UiBuilder { get; }

    /// <summary>
    /// Gets a value indicating whether Dalamud is running in Debug mode or the /xldev menu is open. This can occur on release builds.
    /// </summary>
    bool IsDebugging { get; }

    /// <summary>
    /// Gets the current UI language in two-letter iso format.
    /// </summary>
    string UiLanguage { get; }

    /// <summary>
    /// Gets serializer class with functions to remove special characters from strings.
    /// </summary>
    ISanitizer Sanitizer { get; }

    /// <summary>
    /// Gets the chat type used by default for plugin messages.
    /// </summary>
    XivChatType GeneralChatType { get; }

    /// <summary>
    /// Gets a list of installed plugin names.
    /// </summary>
    List<string> PluginNames { get; }

    /// <summary>
    /// Gets a list of installed plugin internal names.
    /// </summary>
    List<string> PluginInternalNames { get; }

    /// <summary>
    /// Gets an IPC provider.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC provider.</returns>
    /// <exception cref="IpcTypeMismatchError">This is thrown when the requested types do not match the previously registered types are different.</exception>
    ICallGateProvider<TRet> GetIpcProvider<TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, TRet> GetIpcProvider<T1, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, TRet> GetIpcProvider<T1, T2, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, TRet> GetIpcProvider<T1, T2, T3, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, TRet> GetIpcProvider<T1, T2, T3, T4, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, TRet> GetIpcProvider<T1, T2, T3, T4, T5, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name);

    /// <summary>
    /// Gets an IPC subscriber.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC subscriber.</returns>
    ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, TRet> GetIpcSubscriber<T1, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, TRet> GetIpcSubscriber<T1, T2, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, TRet> GetIpcSubscriber<T1, T2, T3, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, TRet> GetIpcSubscriber<T1, T2, T3, T4, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name);

    /// <summary>
    /// Save a plugin configuration(inheriting IPluginConfiguration).
    /// </summary>
    /// <param name="currentConfig">The current configuration.</param>
    void SavePluginConfig(IPluginConfiguration? currentConfig);

    /// <summary>
    /// Get a previously saved plugin configuration or null if none was saved before.
    /// </summary>
    /// <returns>A previously saved config or null if none was saved before.</returns>
    IPluginConfiguration? GetPluginConfig();

    /// <summary>
    /// Get the config directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName.</returns>
    string GetPluginConfigDirectory();

    /// <summary>
    /// Get the loc directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName/loc.</returns>
    string GetPluginLocDirectory();

    /// <summary>
    /// Register a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="commandAction">The action to be executed.</param>
    /// <returns>Returns an SeString payload for the link.</returns>
    DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction);

    /// <summary>
    /// Remove a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    void RemoveChatLinkHandler(uint commandId);

    /// <summary>
    /// Removes all chat link handlers registered by the plugin.
    /// </summary>
    void RemoveChatLinkHandler();

    /// <summary>
    /// Create a new object of the provided type using its default constructor, then inject objects and properties.
    /// </summary>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <returns>The created and initialized type.</returns>
    T? Create<T>(params object[] scopedObjects) where T : class;

    /// <summary>
    /// Inject services into properties on the provided object instance.
    /// </summary>
    /// <param name="instance">The instance to inject services into.</param>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <returns>Whether or not the injection succeeded.</returns>
    bool Inject(object instance, params object[] scopedObjects);
}
