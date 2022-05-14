using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class acts as an interface to various objects needed to interact with Dalamud and the game.
    /// </summary>
    public sealed class DalamudPluginInterface : IDisposable, IDalamudPluginInterface
    {
        private readonly string pluginName;
        private readonly PluginConfigurations configs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudPluginInterface"/> class.
        /// Set up the interface and populate all fields needed.
        /// </summary>
        /// <param name="pluginName">The internal name of the plugin.</param>
        /// <param name="assemblyLocation">Location of the assembly.</param>
        /// <param name="reason">The reason the plugin was loaded.</param>
        /// <param name="isDev">A value indicating whether this is a dev plugin.</param>
        internal DalamudPluginInterface(string pluginName, FileInfo assemblyLocation, PluginLoadReason reason, bool isDev)
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var dataManager = Service<DataManager>.Get();
            var localization = Service<Localization>.Get();

            this.UiBuilder = new UiBuilder(pluginName);

            this.pluginName = pluginName;
            this.AssemblyLocation = assemblyLocation;
            this.configs = Service<PluginManager>.Get().PluginConfigs;
            this.Reason = reason;
            this.IsDev = isDev;

            this.LoadTime = DateTime.Now;
            this.LoadTimeUTC = DateTime.UtcNow;

            this.GeneralChatType = configuration.GeneralChatType;
            this.Sanitizer = new Sanitizer(dataManager.Language);
            if (configuration.LanguageOverride != null)
            {
                this.UiLanguage = configuration.LanguageOverride;
            }
            else
            {
                var currentUiLang = CultureInfo.CurrentUICulture;
                if (Localization.ApplicableLangCodes.Any(langCode => currentUiLang.TwoLetterISOLanguageName == langCode))
                    this.UiLanguage = currentUiLang.TwoLetterISOLanguageName;
                else
                    this.UiLanguage = "en";
            }

            localization.LocalizationChanged += this.OnLocalizationChanged;
            configuration.DalamudConfigurationSaved += this.OnDalamudConfigurationSaved;
        }

        /// <summary>
        /// Delegate for localization change with two-letter iso lang code.
        /// </summary>
        /// <param name="langCode">The new language code.</param>
        public delegate void LanguageChangedDelegate(string langCode);

        /// <inheritdoc cref="IDalamudPluginInterface.LanguageChanged"/>
        public event LanguageChangedDelegate LanguageChanged;

        /// <inheritdoc cref="IDalamudPluginInterface.Reason"/>
        public PluginLoadReason Reason { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.IsDev"/>
        public bool IsDev { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.LoadTime"/>
        public DateTime LoadTime { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.LoadTimeUTC"/>
        public DateTime LoadTimeUTC { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.LoadTimeDelta"/>
        public TimeSpan LoadTimeDelta => DateTime.Now - this.LoadTime;

        /// <inheritdoc cref="IDalamudPluginInterface.DalamudAssetDirectory"/>
        public DirectoryInfo DalamudAssetDirectory => Service<Dalamud>.Get().AssetDirectory;

        /// <inheritdoc cref="IDalamudPluginInterface.AssemblyLocation"/>
        public FileInfo AssemblyLocation { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.ConfigDirectory"/>
        public DirectoryInfo ConfigDirectory => new(this.GetPluginConfigDirectory());

        /// <inheritdoc cref="IDalamudPluginInterface.ConfigFile"/>
        public FileInfo ConfigFile => this.configs.GetConfigFile(this.pluginName);

        /// <inheritdoc cref="IDalamudPluginInterface.UiBuilder"/>
        public UiBuilder UiBuilder { get; private set; }

        /// <inheritdoc cref="IDalamudPluginInterface.IsDebugging"/>
        public bool IsDebugging => Debugger.IsAttached;

        /// <inheritdoc cref="IDalamudPluginInterface.UiLanguage"/>
        public string UiLanguage { get; private set; }

        /// <inheritdoc cref="IDalamudPluginInterface.Sanitizer"/>
        public ISanitizer Sanitizer { get; }

        /// <inheritdoc cref="IDalamudPluginInterface.GeneralChatType"/>
        public XivChatType GeneralChatType { get; private set; }

        /// <inheritdoc cref="IDalamudPluginInterface.PluginNames"/>
        public List<string> PluginNames => Service<PluginManager>.Get().InstalledPlugins.Select(p => p.Manifest.Name).ToList();

        /// <inheritdoc cref="IDalamudPluginInterface.PluginInternalNames"/>
        public List<string> PluginInternalNames => Service<PluginManager>.Get().InstalledPlugins.Select(p => p.Manifest.InternalName).ToList();

        #region IPC

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<TRet> GetIpcProvider<TRet>(string name)
            => new CallGatePubSub<TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, TRet> GetIpcProvider<T1, TRet>(string name)
            => new CallGatePubSub<T1, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, TRet> GetIpcProvider<T1, T2, TRet>(string name)
            => new CallGatePubSub<T1, T2, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, TRet> GetIpcProvider<T1, T2, T3, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, T4, TRet> GetIpcProvider<T1, T2, T3, T4, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, T4, T5, TRet> GetIpcProvider<T1, T2, T3, T4, T5, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcProvider{TRet}"/>
        public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name)
            => new CallGatePubSub<TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, TRet> GetIpcSubscriber<T1, TRet>(string name)
            => new CallGatePubSub<T1, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, TRet> GetIpcSubscriber<T1, T2, TRet>(string name)
            => new CallGatePubSub<T1, T2, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, TRet> GetIpcSubscriber<T1, T2, T3, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, T4, TRet> GetIpcSubscriber<T1, T2, T3, T4, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

        /// <inheritdoc cref="IDalamudPluginInterface.GetIpcSubscriber{TRet}"/>
        public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
            => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

        #endregion

        #region Configuration

        /// <inheritdoc cref="IDalamudPluginInterface.SavePluginConfig"/>
        public void SavePluginConfig(IPluginConfiguration? currentConfig)
        {
            if (currentConfig == null)
                return;

            this.configs.Save(currentConfig, this.pluginName);
        }

        /// <inheritdoc cref="IDalamudPluginInterface.GetPluginConfig"/>
        public IPluginConfiguration? GetPluginConfig()
        {
            // This is done to support json deserialization of plugin configurations
            // even after running an in-game update of plugins, where the assembly version
            // changes.
            // Eventually it might make sense to have a separate method on this class
            // T GetPluginConfig<T>() where T : IPluginConfiguration
            // that can invoke LoadForType() directly instead of via reflection
            // This is here for now to support the current plugin API
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsAssignableTo(typeof(IPluginConfiguration)))
                {
                    var mi = this.configs.GetType().GetMethod("LoadForType");
                    var fn = mi.MakeGenericMethod(type);
                    return (IPluginConfiguration)fn.Invoke(this.configs, new object[] { this.pluginName });
                }
            }

            // this shouldn't be a thing, I think, but just in case
            return this.configs.Load(this.pluginName);
        }

        /// <inheritdoc cref="IDalamudPluginInterface.GetPluginConfigDirectory"/>
        public string GetPluginConfigDirectory() => this.configs.GetDirectory(this.pluginName);

        /// <inheritdoc cref="IDalamudPluginInterface.GetPluginLocDirectory"/>
        public string GetPluginLocDirectory() => this.configs.GetDirectory(Path.Combine(this.pluginName, "loc"));

        #endregion

        #region Chat Links

        /// <inheritdoc cref="IDalamudPluginInterface.AddChatLinkHandler"/>
        public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
        {
            return Service<ChatGui>.Get().AddChatLinkHandler(this.pluginName, commandId, commandAction);
        }

        /// <inheritdoc cref="IDalamudPluginInterface.RemoveChatLinkHandler(uint)"/>
        public void RemoveChatLinkHandler(uint commandId)
        {
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName, commandId);
        }

        /// <inheritdoc cref="IDalamudPluginInterface.RemoveChatLinkHandler()"/>
        public void RemoveChatLinkHandler()
        {
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName);
        }
        #endregion

        #region Dependency Injection

        /// <inheritdoc cref="IDalamudPluginInterface.Create{T}"/>
        public T? Create<T>(params object[] scopedObjects) where T : class
        {
            var svcContainer = Service<IoC.Internal.ServiceContainer>.Get();

            var realScopedObjects = new object[scopedObjects.Length + 1];
            realScopedObjects[0] = this;
            Array.Copy(scopedObjects, 0, realScopedObjects, 1, scopedObjects.Length);

            return svcContainer.Create(typeof(T), realScopedObjects) as T;
        }

        /// <inheritdoc cref="IDalamudPluginInterface.Inject"/>
        public bool Inject(object instance, params object[] scopedObjects)
        {
            var svcContainer = Service<IoC.Internal.ServiceContainer>.Get();

            var realScopedObjects = new object[scopedObjects.Length + 1];
            realScopedObjects[0] = this;
            Array.Copy(scopedObjects, 0, realScopedObjects, 1, scopedObjects.Length);

            return svcContainer.InjectProperties(instance, realScopedObjects);
        }

        #endregion

        /// <summary>
        /// Unregister your plugin and dispose all references.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.UiBuilder.ExplicitDispose();
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName);
            Service<Localization>.Get().LocalizationChanged -= this.OnLocalizationChanged;
            Service<DalamudConfiguration>.Get().DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        }

        /// <summary>
        /// Obsolete implicit dispose implementation. Should not be used.
        /// </summary>
        [Obsolete("Do not dispose \"DalamudPluginInterface\".", true)]
        public void Dispose()
        {
            // ignored
        }

        private void OnLocalizationChanged(string langCode)
        {
            this.UiLanguage = langCode;
            this.LanguageChanged?.Invoke(langCode);
        }

        private void OnDalamudConfigurationSaved(DalamudConfiguration dalamudConfiguration)
        {
            this.GeneralChatType = dalamudConfiguration.GeneralChatType;
        }
    }
}
