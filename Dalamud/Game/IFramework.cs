using System;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// This interface represents the Framework of the native game client and grants access to various subsystems.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IFramework
{
    /// <summary>
    /// Event that gets fired every time the game framework updates.
    /// </summary>
    event Framework.OnUpdateDelegate Update;

    /// <summary>
    /// Gets a raw pointer to the instance of Client::Framework.
    /// </summary>
    FrameworkAddressResolver Address { get; }

    /// <summary>
    /// Gets the last time that the Framework Update event was triggered.
    /// </summary>
    DateTime LastUpdate { get; }

    /// <summary>
    /// Gets the last time in UTC that the Framework Update event was triggered.
    /// </summary>
    DateTime LastUpdateUTC { get; }

    /// <summary>
    /// Gets the delta between the last Framework Update and the currently executing one.
    /// </summary>
    TimeSpan UpdateDelta { get; }

    /// <summary>
    /// Gets a value indicating whether currently executing code is running in the game's framework update thread.
    /// </summary>
    bool IsInFrameworkUpdateThread { get; }

    /// <summary>
    /// Enable this module.
    /// </summary>
    void Enable();

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    Task<T> RunOnFrameworkThread<T>(Func<T> func);

    /// <summary>
    /// Run given function right away if this function has been called from game's Framework.Update thread, or otherwise run on next Framework.Update call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <returns>Task representing the pending or already completed function.</returns>
    Task RunOnFrameworkThread(Action action);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    Task<T> RunOnTick<T>(Func<T> func, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run given function in upcoming Framework.Tick call.
    /// </summary>
    /// <param name="action">Function to call.</param>
    /// <param name="delay">Wait for given timespan before calling this function.</param>
    /// <param name="delayTicks">Count given number of Framework.Tick calls before calling this function. This takes precedence over delay parameter.</param>
    /// <param name="cancellationToken">Cancellation token which will prevent the execution of this function if wait conditions are not met.</param>
    /// <returns>Task representing the pending function.</returns>
    Task RunOnTick(Action action, TimeSpan delay = default, int delayTicks = default, CancellationToken cancellationToken = default);
}
