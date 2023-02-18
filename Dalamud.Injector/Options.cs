using System.Collections.Generic;

using CommandLine;

namespace Dalamud.Injector;

public class Options
{
    [Option('v', Required = false, HelpText = "Verbose logging")]
    public bool Verbose { get; set; }

    [Option("dalamud-working-directory", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudWorkingDirectory { get; set; }

    [Option("dalamud-configuration-path", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudConfigurationPath { get; set; }

    [Option("dalamud-plugin-directory", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudPluginDirectory { get; set; }

    [Option("dalamud-dev-plugin-directory", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudDevPluginDirectory { get; set; }

    [Option("dalamud-asset-directory", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudAssetDirectory { get; set; }

    [Option("dalamud-delay-initialize", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudDelayInitialize { get; set; }

    [Option("dalamud-client-language", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudClientLanguage { get; set; }

    [Option("dalamud-tspack-b64", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? DalamudTsPackB64 { get; set; }

    [Option("logname", Required = false, HelpText = "Specifying Dalamud start info")]
    public string? LogName { get; set; }

    [Option("console", Required = false, HelpText = "Show Console")]
    public bool BootShowConsole { get; set; }

    [Option("etw", Required = false, HelpText = "Enable ETW")]
    public bool BootEnableEtw { get; set; }

    [Option("msgbox1", Required = false, HelpText = "Show messagebox")]
    public bool BootWaitMessageBox1 { get; set; }

    [Option("msgbox2", Required = false, HelpText = "Show messagebox")]
    public bool BootWaitMessageBox2 { get; set; }

    [Option("msgbox3", Required = false, HelpText = "Show messagebox")]
    public bool BootWaitMessageBox3 { get; set; }

    [Option("veh", Required = false, HelpText = "Enable VEH")]
    public bool BootVehEnabled { get; set; }

    [Option("veh-full", Required = false, HelpText = "Enable VEH")]
    public bool BootVehFull { get; set; }

    [Option("no-plugin", Required = false, HelpText = "No plugins")]
    public bool NoLoadPlugins { get; set; }

    [Option("no-3rd-plugin", Required = false, HelpText = "No plugins")]
    public bool NoLoadThirdPartyPlugins { get; set; }

    [Option("crash-handler-console", Required = false, HelpText = "Show Console")]
    public bool CrashHandlerShow { get; set; }
}

[Verb("inject", isDefault: true)]
public class InjectOptions : Options
{
    [Option('a', "all", Default = true, Required = false)]
    public bool All { get; set; }

    [Option("warn", Required = false)]
    public bool Warn { get; set; }

    [Option("fix-acl", Required = false)]
    public bool FixAcl { get; set; }

    [Option("acl-fix", Required = false, Hidden = true)]
    public bool AclFix { get; set; }

    [Option("se-debug-privilege", Required = false)]
    public bool SeDebugPrivilege { get; set; }

    [Value(0, MetaName = "processids", HelpText = "IDs of processes to inject into")]
    public IEnumerable<int> ProcessIds { get; set; }

    [Value(1, MetaName = "startinfo", HelpText = "Serialized Dalamud StartInfo")]
    public string? StartInfo { get; set; }
}

public enum LaunchMode
{
    Entrypoint,
    Inject,
}

[Verb("launch")]
public class LaunchOptions : Options
{
    [Option('f', "fake-arguments", Required = false)]
    public bool UseFakeArguments { get; set; }

    [Option('g', "game", Required = false)]
    public string? Game { get; set; }

    [Option('m', "mode", Required = false)]
    public LaunchMode Mode { get; set; }

    [Option("handle-owner", Required = false)]
    public string? HandleOwner { get; set; }

    [Option("without-dalamud", Required = false)]
    public bool WithoutDalamud { get; set; }

    [Option("no-fix-acl", Required = false)]
    public bool NoFixAcl { get; set; }

    [Option("no-wait", Required = false)]
    public bool NoWaitForGameWindow { get; set; }

    [Value(0, MetaName = "gameargs", HelpText = "Arguments to be passed to the game")]
    public IEnumerable<string> GameArgs { get; set; }

    [Value(1, MetaName = "startinfo", HelpText = "Serialized Dalamud StartInfo")]
    public string? StartInfo { get; set; }
}

[Verb("launch-test")]
public class LaunchTestOptions : Options
{
}
