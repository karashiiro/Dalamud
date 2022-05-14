using System;

namespace Dalamud.IoC
{
    /// <summary>
    /// This attribute indicates whether the decorated class should be exposed to plugins via IoC.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class PluginInterfaceAttribute : Attribute
    {
    }
}
