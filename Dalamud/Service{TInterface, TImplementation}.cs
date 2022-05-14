using System;
using System.Reflection;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud;

/// <summary>
/// Basic service locator.
/// </summary>
/// <remarks>
/// Only used internally within Dalamud, if plugins need access to things it should be _only_ via DI.
/// </remarks>
/// <typeparam name="TInterface">An additional interface of the class you want to store in the service locator.</typeparam>
/// <typeparam name="TImplementation">The class you want to store in the service locator.</typeparam>
internal class Service<TInterface, TImplementation> : Service<TImplementation>
    where TInterface : class
    where TImplementation : class, TInterface
{
    static Service()
    {
    }

    /// <summary>
    /// Sets the type in the service locator to the given object, using the provided additional interface type.
    /// </summary>
    /// <param name="obj">Object to set.</param>
    /// <returns>The set object.</returns>
    public static new TInterface Set(TImplementation obj)
    {
        SetInstanceObject(obj);

        return Instance!;
    }

    /// <summary>
    /// Sets the type in the service locator via the default parameterless constructor.
    /// </summary>
    /// <returns>The set object.</returns>
    public static new TInterface Set()
    {
        if (Instance != null)
            throw new Exception($"Service {typeof(TImplementation).FullName} was set twice");

        var obj = (TImplementation?)Activator.CreateInstance(typeof(TImplementation), true);

        SetInstanceObject(obj);

        return Instance!;
    }

    /// <summary>
    /// Sets a type in the service locator via a constructor with the given parameter types.
    /// </summary>
    /// <param name="args">Constructor arguments.</param>
    /// <returns>The set object.</returns>
    public static new TInterface Set(params object[] args)
    {
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args), $"Service locator was passed a null for type {typeof(TImplementation).FullName} parameterized constructor ");
        }

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
        var obj = (TImplementation?)Activator.CreateInstance(typeof(TImplementation), flags, null, args, null, null);

        SetInstanceObject(obj);

        return obj;
    }

    /// <summary>
    /// Attempt to pull the instance out of the service locator.
    /// </summary>
    /// <returns>The object if registered.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the object instance is not present in the service locator.</exception>
    public static new TInterface Get()
    {
        return Instance ?? throw new InvalidOperationException($"{typeof(TInterface).FullName} has not been registered in the service locator!");
    }

    /// <summary>
    /// Attempt to pull the instance out of the service locator.
    /// </summary>
    /// <returns>The object if registered, null otherwise.</returns>
    public static new TInterface? GetNullable()
    {
        return Instance;
    }

    private static void SetInstanceObject(TImplementation? instance)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance), $"Service locator received a null for type {typeof(TInterface).FullName}");

        var availableToPlugins = RegisterInIoCContainer(instance);

        if (availableToPlugins)
            Log.Information($"Registered {typeof(TInterface).FullName} into service locator and exposed to plugins");
        else
            Log.Information($"Registered {typeof(TInterface).FullName} into service locator privately");
    }

    private static bool RegisterInIoCContainer(TImplementation instance)
    {
        var attr = typeof(TInterface).GetCustomAttribute<PluginInterfaceAttribute>();
        if (attr == null)
        {
            return false;
        }

        var ioc = Service<ServiceContainer>.GetNullable();
        if (ioc == null)
        {
            return false;
        }

        ioc.RegisterSingleton<TInterface>(instance);

        return true;
    }
}
