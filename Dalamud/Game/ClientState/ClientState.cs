using System;
using System.Runtime.InteropServices;

using Dalamud.Data;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the state of the game client at the time of access.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class ClientState : IDisposable, IClientState
    {
        private readonly ClientStateAddressResolver address;
        private readonly Hook<SetupTerritoryTypeDelegate> setupTerritoryTypeHook;

        private bool lastConditionNone = true;
        private bool lastFramePvP = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientState"/> class.
        /// Set up client state access.
        /// </summary>
        internal ClientState()
        {
            this.address = new ClientStateAddressResolver();
            this.address.Setup();

            Log.Verbose("===== C L I E N T  S T A T E =====");

            this.ClientLanguage = Service<DalamudStartInfo>.Get().Language;

            Service<ObjectTable>.Set(this.address);

            Service<FateTable>.Set(this.address);

            Service<PartyList>.Set(this.address);

            Service<BuddyList>.Set(this.address);

            Service<JobGauges>.Set(this.address);

            Service<KeyState>.Set(this.address);

            Service<GamepadState>.Set(this.address);

            Service<Conditions.Condition>.Set(this.address);

            Service<TargetManager>.Set(this.address);

            Service<AetheryteList>.Set(this.address);

            Log.Verbose($"SetupTerritoryType address 0x{this.address.SetupTerritoryType.ToInt64():X}");

            this.setupTerritoryTypeHook = new Hook<SetupTerritoryTypeDelegate>(this.address.SetupTerritoryType, this.SetupTerritoryTypeDetour);

            var framework = Service<Framework>.Get();
            framework.Update += this.FrameworkOnOnUpdateEvent;

            var networkHandlers = Service<NetworkHandlers>.Get();
            networkHandlers.CfPop += this.NetworkHandlersOnCfPop;
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetupTerritoryTypeDelegate(IntPtr manager, ushort terriType);

        /// <inheritdoc cref="IClientState.TerritoryChanged"/>
        public event EventHandler<ushort> TerritoryChanged;

        /// <inheritdoc cref="IClientState.Login"/>
        public event EventHandler Login;

        /// <inheritdoc cref="IClientState.Logout"/>
        public event EventHandler Logout;

        /// <inheritdoc cref="IClientState.EnterPvP"/>
        public event System.Action EnterPvP;

        /// <inheritdoc cref="IClientState.LeavePvP"/>
        public event System.Action LeavePvP;

        /// <inheritdoc cref="IClientState.CfPop"/>
        public event EventHandler<ContentFinderCondition> CfPop;

        /// <inheritdoc cref="IClientState.ClientLanguage"/>
        public ClientLanguage ClientLanguage { get; }

        /// <inheritdoc cref="IClientState.TerritoryType"/>
        public ushort TerritoryType { get; private set; }

        /// <inheritdoc cref="IClientState.LocalPlayer"/>
        public PlayerCharacter? LocalPlayer => Service<ObjectTable>.Get()[0] as PlayerCharacter;

        /// <inheritdoc cref="IClientState.LocalContentId"/>
        public ulong LocalContentId => (ulong)Marshal.ReadInt64(this.address.LocalContentId);

        /// <inheritdoc cref="IClientState.IsLoggedIn"/>
        public bool IsLoggedIn { get; private set; }

        /// <inheritdoc cref="IClientState.IsPvP"/>
        public bool IsPvP { get; private set; }

        /// <inheritdoc cref="IClientState.Enable"/>
        public void Enable()
        {
            Service<Conditions.Condition>.Get().Enable();
            Service<GamepadState>.Get().Enable();
            this.setupTerritoryTypeHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.setupTerritoryTypeHook.Dispose();
            Service<Conditions.Condition>.Get().ExplicitDispose();
            Service<GamepadState>.Get().ExplicitDispose();
            Service<Framework>.Get().Update -= this.FrameworkOnOnUpdateEvent;
            Service<NetworkHandlers>.Get().CfPop -= this.NetworkHandlersOnCfPop;
        }

        private IntPtr SetupTerritoryTypeDetour(IntPtr manager, ushort terriType)
        {
            this.TerritoryType = terriType;
            this.TerritoryChanged?.Invoke(this, terriType);

            Log.Debug("TerritoryType changed: {0}", terriType);

            return this.setupTerritoryTypeHook.Original(manager, terriType);
        }

        private void NetworkHandlersOnCfPop(object sender, Lumina.Excel.GeneratedSheets.ContentFinderCondition e)
        {
            this.CfPop?.Invoke(this, e);
        }

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            var condition = Service<Conditions.Condition>.Get();
            var gameGui = Service<GameGui>.Get();
            var data = Service<DataManager>.Get();

            if (condition.Any() && this.lastConditionNone == true)
            {
                Log.Debug("Is login");
                this.lastConditionNone = false;
                this.IsLoggedIn = true;
                this.Login?.Invoke(this, null);
                gameGui.ResetUiHideState();
            }

            if (!condition.Any() && this.lastConditionNone == false)
            {
                Log.Debug("Is logout");
                this.lastConditionNone = true;
                this.IsLoggedIn = false;
                this.Logout?.Invoke(this, null);
                gameGui.ResetUiHideState();
            }

            if (this.TerritoryType != 0)
            {
                var terriRow = data.GetExcelSheet<TerritoryType>()!.GetRow(this.TerritoryType);
                this.IsPvP = terriRow?.Bg.RawString.StartsWith("ffxiv/pvp") ?? false;
            }

            if (this.IsPvP != this.lastFramePvP)
            {
                this.lastFramePvP = this.IsPvP;

                if (this.IsPvP)
                {
                    this.EnterPvP?.Invoke();
                }
                else
                {
                    this.LeavePvP?.Invoke();
                }
            }
        }
    }
}
