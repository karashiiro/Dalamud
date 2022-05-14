using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game
{
    /// <summary>
    /// This class represents the Framework of the native game client and grants access to various subsystems.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class Framework : IDisposable, IFramework
    {
        private static Stopwatch statsStopwatch = new();

        private readonly List<RunOnNextTickTaskBase> runOnNextTickTaskList = new();
        private readonly Stopwatch updateStopwatch = new();

        private bool tier2Initialized = false;
        private bool tier3Initialized = false;
        private bool tierInitError = false;

        private Hook<OnUpdateDetour> updateHook;
        private Hook<OnDestroyDetour> destroyHook;
        private Hook<OnRealDestroyDelegate> realDestroyHook;

        private Thread? frameworkUpdateThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="Framework"/> class.
        /// </summary>
        internal Framework()
        {
            this.Address = new FrameworkAddressResolver();
            this.Address.Setup();

            Log.Verbose($"Framework address 0x{this.Address.BaseAddress.ToInt64():X}");
            if (this.Address.BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Framework is not initalized yet.");
            }

            // Hook virtual functions
            this.HookVTable();
        }

        /// <summary>
        /// A delegate type used with the <see cref="Update"/> event.
        /// </summary>
        /// <param name="framework">The Framework instance.</param>
        public delegate void OnUpdateDelegate(Framework framework);

        /// <summary>
        /// A delegate type used during the native Framework::destroy.
        /// </summary>
        /// <param name="framework">The native Framework address.</param>
        /// <returns>A value indicating if the call was successful.</returns>
        public delegate bool OnRealDestroyDelegate(IntPtr framework);

        /// <summary>
        /// A delegate type used during the native Framework::free.
        /// </summary>
        /// <returns>The native Framework address.</returns>
        public delegate IntPtr OnDestroyDelegate();

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool OnUpdateDetour(IntPtr framework);

        private delegate IntPtr OnDestroyDetour(); // OnDestroyDelegate

        /// <inheritdoc cref="IFramework.Update"/>
        public event OnUpdateDelegate Update;

        /// <summary>
        /// Gets or sets a value indicating whether the collection of stats is enabled.
        /// </summary>
        public static bool StatsEnabled { get; set; }

        /// <summary>
        /// Gets the stats history mapping.
        /// </summary>
        public static Dictionary<string, List<double>> StatsHistory { get; } = new();

        /// <inheritdoc cref="IFramework.Address"/>
        public FrameworkAddressResolver Address { get; }

        /// <inheritdoc cref="IFramework.LastUpdate"/>
        public DateTime LastUpdate { get; private set; } = DateTime.MinValue;

        /// <inheritdoc cref="IFramework.LastUpdateUTC"/>
        public DateTime LastUpdateUTC { get; private set; } = DateTime.MinValue;

        /// <inheritdoc cref="IFramework.UpdateDelta"/>
        public TimeSpan UpdateDelta { get; private set; } = TimeSpan.Zero;

        /// <inheritdoc cref="IFramework.IsInFrameworkUpdateThread"/>
        public bool IsInFrameworkUpdateThread => Thread.CurrentThread == this.frameworkUpdateThread;

        /// <summary>
        /// Gets or sets a value indicating whether to dispatch update events.
        /// </summary>
        internal bool DispatchUpdateEvents { get; set; } = true;

        /// <inheritdoc cref="IFramework.Enable"/>
        public void Enable()
        {
            Service<LibcFunction>.Set();
            Service<GameGui>.Get().Enable();
            Service<GameNetwork>.Get().Enable();

            this.updateHook.Enable();
            this.destroyHook.Enable();
            this.realDestroyHook.Enable();
        }

        /// <inheritdoc cref="IFramework.RunOnFrameworkThread{T}(Func{T})"/>
        public Task<T> RunOnFrameworkThread<T>(Func<T> func) => this.IsInFrameworkUpdateThread ? Task.FromResult(func()) : this.RunOnTick(func);

        /// <inheritdoc cref="IFramework.RunOnFrameworkThread(Action)"/>
        public Task RunOnFrameworkThread(Action action)
        {
            if (this.IsInFrameworkUpdateThread)
            {
                try
                {
                    action();
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
            else
            {
                return this.RunOnTick(action);
            }
        }

        /// <inheritdoc cref="IFramework.RunOnTick{T}(Func{T}, TimeSpan, int, CancellationToken)"/>
        public Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<T>();
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskFunc<T>()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Func = func,
            });
            return tcs.Task;
        }

        /// <inheritdoc cref="IFramework.RunOnTick(Action, TimeSpan, int, CancellationToken)"/>
        public Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();
            this.runOnNextTickTaskList.Add(new RunOnNextTickTaskAction()
            {
                RemainingTicks = delayTicks,
                RunAfterTickCount = Environment.TickCount64 + (long)Math.Ceiling(delay.TotalMilliseconds),
                CancellationToken = cancellationToken,
                TaskCompletionSource = tcs,
                Action = action,
            });
            return tcs.Task;
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            Service<GameGui>.GetNullable()?.ExplicitDispose();
            Service<GameNetwork>.GetNullable()?.ExplicitDispose();

            this.updateHook?.Disable();
            this.destroyHook?.Disable();
            this.realDestroyHook?.Disable();
            Thread.Sleep(500);

            this.updateHook?.Dispose();
            this.destroyHook?.Dispose();
            this.realDestroyHook?.Dispose();

            this.updateStopwatch.Reset();
            statsStopwatch.Reset();
        }

        private void HookVTable()
        {
            var vtable = Marshal.ReadIntPtr(this.Address.BaseAddress);
            // Virtual function layout:
            // .rdata:00000001411F1FE0 dq offset Xiv__Framework___dtor
            // .rdata:00000001411F1FE8 dq offset Xiv__Framework__init
            // .rdata:00000001411F1FF0 dq offset Xiv__Framework__destroy
            // .rdata:00000001411F1FF8 dq offset Xiv__Framework__free
            // .rdata:00000001411F2000 dq offset Xiv__Framework__update

            var pUpdate = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);
            this.updateHook = new Hook<OnUpdateDetour>(pUpdate, this.HandleFrameworkUpdate);

            var pDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);
            this.destroyHook = new Hook<OnDestroyDetour>(pDestroy, this.HandleFrameworkDestroy);

            var pRealDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 2);
            this.realDestroyHook = new Hook<OnRealDestroyDelegate>(pRealDestroy, this.HandleRealDestroy);
        }

        private bool HandleFrameworkUpdate(IntPtr framework)
        {
            // If any of the tier loads failed, just go to the original code.
            if (this.tierInitError)
                goto original;

            this.frameworkUpdateThread ??= Thread.CurrentThread;

            var dalamud = Service<Dalamud>.Get();

            // If this is the first time we are running this loop, we need to init Dalamud subsystems synchronously
            if (!this.tier2Initialized)
            {
                this.tier2Initialized = dalamud.LoadTier2();
                if (!this.tier2Initialized)
                    this.tierInitError = true;

                goto original;
            }

            // Plugins expect the interface to be available and ready, so we need to wait with plugins until we have init'd ImGui
            if (!this.tier3Initialized && Service<InterfaceManager>.GetNullable()?.IsReady == true)
            {
                this.tier3Initialized = dalamud.LoadTier3();
                if (!this.tier3Initialized)
                    this.tierInitError = true;

                goto original;
            }

            try
            {
                Service<ChatGui>.Get().UpdateQueue();
                Service<ToastGui>.Get().UpdateQueue();
                Service<GameNetwork>.Get().UpdateQueue();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while handling Framework::Update hook.");
            }

            if (this.DispatchUpdateEvents)
            {
                this.updateStopwatch.Stop();
                this.UpdateDelta = TimeSpan.FromMilliseconds(this.updateStopwatch.ElapsedMilliseconds);
                this.updateStopwatch.Restart();

                this.LastUpdate = DateTime.Now;
                this.LastUpdateUTC = DateTime.UtcNow;

                try
                {
                    this.runOnNextTickTaskList.RemoveAll(x => x.Run());

                    if (StatsEnabled && this.Update != null)
                    {
                        // Stat Tracking for Framework Updates
                        var invokeList = this.Update.GetInvocationList();
                        var notUpdated = StatsHistory.Keys.ToList();

                        // Individually invoke OnUpdate handlers and time them.
                        foreach (var d in invokeList)
                        {
                            statsStopwatch.Restart();
                            d.Method.Invoke(d.Target, new object[] { this });
                            statsStopwatch.Stop();

                            var key = $"{d.Target}::{d.Method.Name}";
                            if (notUpdated.Contains(key))
                                notUpdated.Remove(key);

                            if (!StatsHistory.ContainsKey(key))
                                StatsHistory.Add(key, new List<double>());

                            StatsHistory[key].Add(statsStopwatch.Elapsed.TotalMilliseconds);

                            if (StatsHistory[key].Count > 1000)
                            {
                                StatsHistory[key].RemoveRange(0, StatsHistory[key].Count - 1000);
                            }
                        }

                        // Cleanup handlers that are no longer being called
                        foreach (var key in notUpdated)
                        {
                            if (StatsHistory[key].Count > 0)
                            {
                                StatsHistory[key].RemoveAt(0);
                            }
                            else
                            {
                                StatsHistory.Remove(key);
                            }
                        }
                    }
                    else
                    {
                        this.Update?.Invoke(this);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception while dispatching Framework::Update event.");
                }
            }

            original:
            return this.updateHook.Original(framework);
        }

        private bool HandleRealDestroy(IntPtr framework)
        {
            if (this.DispatchUpdateEvents)
            {
                Log.Information("Framework::Destroy!");

                var dalamud = Service<Dalamud>.Get();
                dalamud.DisposePlugins();

                Log.Information("Framework::Destroy OK!");
            }

            this.DispatchUpdateEvents = false;

            return this.realDestroyHook.Original(framework);
        }

        private IntPtr HandleFrameworkDestroy()
        {
            Log.Information("Framework::Free!");

            // Store the pointer to the original trampoline location
            var originalPtr = Marshal.GetFunctionPointerForDelegate(this.destroyHook.Original);

            var dalamud = Service<Dalamud>.Get();
            dalamud.Unload();
            dalamud.WaitForUnloadFinish();

            Log.Information("Framework::Free OK!");

            // Return the original trampoline location to cleanly exit
            return originalPtr;
        }

        private abstract class RunOnNextTickTaskBase
        {
            internal int RemainingTicks { get; set; }

            internal long RunAfterTickCount { get; init; }

            internal CancellationToken CancellationToken { get; init; }

            internal bool Run()
            {
                if (this.CancellationToken.IsCancellationRequested)
                {
                    this.CancelImpl();
                    return true;
                }

                if (this.RemainingTicks > 0)
                    this.RemainingTicks -= 1;
                if (this.RemainingTicks > 0)
                    return false;

                if (this.RunAfterTickCount > Environment.TickCount64)
                    return false;

                this.RunImpl();

                return true;
            }

            protected abstract void RunImpl();

            protected abstract void CancelImpl();
        }

        private class RunOnNextTickTaskFunc<T> : RunOnNextTickTaskBase
        {
            internal TaskCompletionSource<T> TaskCompletionSource { get; init; }

            internal Func<T> Func { get; init; }

            protected override void RunImpl()
            {
                try
                {
                    this.TaskCompletionSource.SetResult(this.Func());
                }
                catch (Exception ex)
                {
                    this.TaskCompletionSource.SetException(ex);
                }
            }

            protected override void CancelImpl()
            {
                this.TaskCompletionSource.SetCanceled();
            }
        }

        private class RunOnNextTickTaskAction : RunOnNextTickTaskBase
        {
            internal TaskCompletionSource TaskCompletionSource { get; init; }

            internal Action Action { get; init; }

            protected override void RunImpl()
            {
                try
                {
                    this.Action();
                    this.TaskCompletionSource.SetResult();
                }
                catch (Exception ex)
                {
                    this.TaskCompletionSource.SetException(ex);
                }
            }

            protected override void CancelImpl()
            {
                this.TaskCompletionSource.SetCanceled();
            }
        }
    }
}
