using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using CommandLine;
using CommandLine.Text;
using Dalamud.Game;
using Newtonsoft.Json;
using Reloaded.Memory.Buffers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using static Dalamud.Injector.NativeFunctions;

namespace Dalamud.Injector
{
    /// <summary>
    /// Entrypoint to the program.
    /// </summary>
    public sealed partial class EntryPoint
    {
        /// <summary>
        /// A delegate used during initialization of the CLR from Dalamud.Injector.Boot.
        /// </summary>
        /// <param name="argc">Count of arguments.</param>
        /// <param name="argvPtr">char** string arguments.</param>
        public delegate void MainDelegate(int argc, IntPtr argvPtr);

        /// <summary>
        /// Start the Dalamud injector.
        /// </summary>
        /// <param name="argc">Count of arguments.</param>
        /// <param name="argvPtr">byte** string arguments.</param>
        public static void Main(int argc, IntPtr argvPtr)
        {
            List<string> args = new(argc);
            unsafe
            {
                var argv = (IntPtr*)argvPtr;
                for (var i = 1; i < argc; i++)
                    args.Add(Marshal.PtrToStringUni(argv[i]));
            }

            if (args.Count >= 1 && args[0].ToLowerInvariant() is "--help" or "-h")
            {
                var command = args.Count >= 2 ? args[1] : null;
                Environment.Exit(ProcessHelpCommand(args, command));
            }

            if (args.Count >= 2 && args[1].ToLowerInvariant() is "--help" or "-h")
            {
                var command = args[0];
                Environment.Exit(ProcessHelpCommand(args, command));
            }

            var parser = new Parser(opts =>
            {
                opts.CaseInsensitiveEnumValues = true;
                opts.EnableDashDash = true;
            });
            var parserResult = parser.ParseArguments<InjectOptions, LaunchOptions, LaunchTestOptions>(args);
            var exitCode = parserResult
                .MapResult(
                    (InjectOptions opts) =>
                    {
                        Init(parserResult, opts);
                        DalamudStartInfo startInfo = null;
                        if (!string.IsNullOrEmpty(opts.StartInfo))
                        {
                            startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(
                                Encoding.UTF8.GetString(Convert.FromBase64String(opts.StartInfo)));
                        }

                        startInfo = ExtractAndInitializeStartInfoFromArguments(startInfo, opts);
                        return ProcessInjectCommand(opts, startInfo);
                    },
                    (LaunchOptions opts) =>
                    {
                        Init(parserResult, opts);
                        DalamudStartInfo startInfo = null;
                        if (!string.IsNullOrEmpty(opts.StartInfo))
                        {
                            startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(
                                Encoding.UTF8.GetString(Convert.FromBase64String(opts.StartInfo)));
                        }

                        startInfo = ExtractAndInitializeStartInfoFromArguments(startInfo, opts);
                        return ProcessLaunchCommand(opts, startInfo);
                    },
                    (LaunchTestOptions opts) =>
                    {
                        Init(parserResult, opts);
                        return ProcessLaunchTestCommand(opts);
                    },
                    _ => ProcessHelpCommand(args));

            Environment.Exit(exitCode);
        }

        private static int ProcessHelpCommand(IReadOnlyList<string> args, string? particularCommand = default)
        {
            var exeName = Path.GetFileName(args[0]);

            var exeSpaces = string.Empty;
            for (var i = exeName.Length; i > 0; i--)
                exeSpaces += " ";

            if (particularCommand is null or "help")
                Console.WriteLine("{0} help [command]", exeName);

            if (particularCommand is null or "inject")
                Console.WriteLine("{0} inject [-h/--help] [-a/--all] [--warn] [--fix-acl] [--se-debug-privilege] [pid1] [pid2] [pid3] ...", exeName);

            if (particularCommand is null or "launch")
            {
                Console.WriteLine("{0} launch [-h/--help] [-f/--fake-arguments]", exeName);
                Console.WriteLine("{0}        [-g path/to/ffxiv_dx11.exe] [--game=path/to/ffxiv_dx11.exe]", exeSpaces);
                Console.WriteLine("{0}        [-m entrypoint|inject] [--mode=entrypoint|inject]", exeSpaces);
                Console.WriteLine("{0}        [--handle-owner=inherited-handle-value]", exeSpaces);
                Console.WriteLine("{0}        [--without-dalamud] [--no-fix-acl]", exeSpaces);
                Console.WriteLine("{0}        [--no-wait]", exeSpaces);
                Console.WriteLine("{0}        [-- game_arg1=value1 game_arg2=value2 ...]", exeSpaces);
            }

            Console.WriteLine("Specifying dalamud start info: [--dalamud-working-directory=path] [--dalamud-configuration-path=path]");
            Console.WriteLine("                               [--dalamud-plugin-directory=path] [--dalamud-dev-plugin-directory=path]");
            Console.WriteLine("                               [--dalamud-asset-directory=path] [--dalamud-delay-initialize=0(ms)]");
            Console.WriteLine("                               [--dalamud-client-language=0-3|j(apanese)|e(nglish)|d|g(erman)|f(rench)]");

            Console.WriteLine("Verbose logging:\t[-v]");
            Console.WriteLine("Show Console:\t[--console] [--crash-handler-console]");
            Console.WriteLine("Enable ETW:\t[--etw]");
            Console.WriteLine("Enable VEH:\t[--veh], [--veh-full]");
            Console.WriteLine("Show messagebox:\t[--msgbox1], [--msgbox2], [--msgbox3]");
            Console.WriteLine("No plugins:\t[--no-plugin] [--no-3rd-plugin]");

            return 0;
        }

        private static string GetLogPath(string fileName, string? logName)
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            fileName = !string.IsNullOrEmpty(logName) ? $"{fileName}-{logName}.log" : $"{fileName}.log";

#if DEBUG
            var logPath = Path.Combine(baseDirectory, fileName);
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", fileName);
#endif

            return logPath;
        }

        private static void Init<T>(ParserResult<T> parserResult, Options opts)
        {
            InitLogging(opts);
            InitUnhandledException(parserResult);

            var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            if (cwd?.FullName != Directory.GetCurrentDirectory())
            {
                Log.Debug("Changing cwd to {Cwd}", cwd);
                Directory.SetCurrentDirectory(cwd.FullName);
            }
        }

        private static void InitUnhandledException<T>(ParserResult<T> parserResult)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                var exObj = eventArgs.ExceptionObject;

                if (exObj is CommandLineException clex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command line error: {0}", clex.Message);
                    Console.WriteLine();
                    Console.WriteLine(HelpText.RenderUsageText(parserResult));
                }
                else if (Log.Logger == null)
                {
                    Console.WriteLine($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                }
                else if (exObj is Exception ex)
                {
                    Log.Error(ex, "A fatal error has occurred");
                }
                else
                {
                    Log.Error("A fatal error has occurred: {Exception}", eventArgs.ExceptionObject.ToString());
                }

                Environment.Exit(-1);
            };
        }

        private static void InitLogging(Options opts)
        {
            var verbose = opts.Verbose;
#if DEBUG
            verbose = true;
#endif

            var levelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = verbose ? LogEventLevel.Verbose : LogEventLevel.Information,
            };

            var logPath = GetLogPath("dalamud.injector", opts.LogName);

            CullLogFile(logPath, 1 * 1024 * 1024);

            Log.Logger = new LoggerConfiguration()
                         .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
                         .WriteTo.Async(a => a.File(logPath))
                         .MinimumLevel.ControlledBy(levelSwitch)
                         .CreateLogger();
        }

        private static void CullLogFile(string logPath, int cullingFileSize)
        {
            try
            {
                var bufferSize = 4096;

                var logFile = new FileInfo(logPath);

                if (!logFile.Exists)
                    logFile.Create();

                if (logFile.Length <= cullingFileSize)
                    return;

                var amountToCull = logFile.Length - cullingFileSize;

                if (amountToCull < bufferSize)
                    return;

                using var reader = new BinaryReader(logFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                using var writer = new BinaryWriter(logFile.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite));

                reader.BaseStream.Seek(amountToCull, SeekOrigin.Begin);

                var read = -1;
                var total = 0;
                var buffer = new byte[bufferSize];
                while (read != 0)
                {
                    read = reader.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, read);
                    total += read;
                }

                writer.BaseStream.SetLength(total);
            }
            catch (Exception)
            {
                /*
                var caption = "XIVLauncher Error";
                var message = $"Log cull threw an exception: {ex.Message}\n{ex.StackTrace ?? string.Empty}";
                _ = MessageBoxW(IntPtr.Zero, message, caption, MessageBoxType.IconError | MessageBoxType.Ok);
                */
            }
        }

        private static DalamudStartInfo ExtractAndInitializeStartInfoFromArguments(
            DalamudStartInfo? startInfo, Options opts)
        {
            int len;
            string key;

            startInfo ??= new DalamudStartInfo();

            var workingDirectory = opts.DalamudWorkingDirectory ?? startInfo.WorkingDirectory;
            var configurationPath = opts.DalamudConfigurationPath ?? startInfo.ConfigurationPath;
            var pluginDirectory = opts.DalamudPluginDirectory ?? startInfo.PluginDirectory;
            var defaultPluginDirectory = opts.DalamudDevPluginDirectory ?? startInfo.DefaultPluginDirectory;
            var assetDirectory = opts.DalamudAssetDirectory ?? startInfo.AssetDirectory;
            var delayInitializeMs = string.IsNullOrEmpty(opts.DalamudDelayInitialize)
                                        ? startInfo.DelayInitializeMs
                                        : int.Parse(opts.DalamudDelayInitialize);
            var logName = opts.LogName ?? startInfo.LogName;
            var languageStr = opts.DalamudClientLanguage?.ToLowerInvariant() ??
                              startInfo.Language.ToString().ToLowerInvariant();
            var troubleshootingData = opts.DalamudTsPackB64 ??
                                      "{\"empty\": true, \"description\": \"No troubleshooting data supplied.\"}";

            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            workingDirectory ??= Directory.GetCurrentDirectory();
            configurationPath ??= Path.Combine(xivlauncherDir, "dalamudConfig.json");
            pluginDirectory ??= Path.Combine(xivlauncherDir, "installedPlugins");
            defaultPluginDirectory ??= Path.Combine(xivlauncherDir, "devPlugins");
            assetDirectory ??= Path.Combine(xivlauncherDir, "dalamudAssets", "dev");

            ClientLanguage clientLanguage;
            if (languageStr[..(len = Math.Min(languageStr.Length, (key = "english").Length))] == key[..len])
                clientLanguage = ClientLanguage.English;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "japanese").Length))] == key[..len])
                clientLanguage = ClientLanguage.Japanese;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "日本語").Length))] == key[..len])
                clientLanguage = ClientLanguage.Japanese;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "german").Length))] == key[..len])
                clientLanguage = ClientLanguage.German;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "deutsch").Length))] == key[..len])
                clientLanguage = ClientLanguage.German;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "french").Length))] == key[..len])
                clientLanguage = ClientLanguage.French;
            else if (languageStr[..(len = Math.Min(languageStr.Length, (key = "français").Length))] == key[..len])
                clientLanguage = ClientLanguage.French;
            else if (int.TryParse(languageStr, out var languageInt) && Enum.IsDefined((ClientLanguage)languageInt))
                clientLanguage = (ClientLanguage)languageInt;
            else
                throw new CommandLineException($"\"{languageStr}\" is not a valid supported language.");

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.ConfigurationPath = configurationPath;
            startInfo.PluginDirectory = pluginDirectory;
            startInfo.DefaultPluginDirectory = defaultPluginDirectory;
            startInfo.AssetDirectory = assetDirectory;
            startInfo.Language = clientLanguage;
            startInfo.DelayInitializeMs = delayInitializeMs;
            startInfo.GameVersion = null;
            startInfo.TroubleshootingPackData = troubleshootingData;
            startInfo.LogName = logName;

            // Set boot defaults
            startInfo.BootShowConsole = opts.BootShowConsole;
            startInfo.BootEnableEtw = opts.BootEnableEtw;
            startInfo.BootLogPath = GetLogPath("dalamud.boot", startInfo.LogName);
            startInfo.BootEnabledGameFixes = new List<string>
            {
                "prevent_devicechange_crashes", "disable_game_openprocess_access_check", "redirect_openprocess",
                "backup_userdata_save", "clr_failfast_hijack"
            };
            startInfo.BootDotnetOpenProcessHookMode = 0;
            startInfo.BootWaitMessageBox |= opts.BootWaitMessageBox1 ? 1 : 0;
            startInfo.BootWaitMessageBox |= opts.BootWaitMessageBox2 ? 2 : 0;
            startInfo.BootWaitMessageBox |= opts.BootWaitMessageBox3 ? 4 : 0;
            // startInfo.BootVehEnabled = opts.BootVehEnabled;
            startInfo.BootVehEnabled = true;
            startInfo.BootVehFull = opts.BootVehFull;
            startInfo.NoLoadPlugins = opts.NoLoadPlugins;
            startInfo.NoLoadThirdPartyPlugins = opts.NoLoadThirdPartyPlugins;
            // startInfo.BootUnhookDlls = new List<string>() { "kernel32.dll", "ntdll.dll", "user32.dll" };
            startInfo.CrashHandlerShow = opts.CrashHandlerShow;

            return startInfo;
        }

        private static int ProcessInjectCommand(InjectOptions opts, DalamudStartInfo dalamudStartInfo)
        {
            var processes = opts.ProcessIds
                                .Select(pid =>
                                {
                                    try
                                    {
                                        return Process.GetProcessById(pid);
                                    }
                                    catch (ArgumentException)
                                    {
                                        Log.Error("Could not find process with PID: {Pid}", pid);
                                        return null;
                                    }
                                })
                                .Where(proc => proc is not null)
                                .ToList();
            var targetProcessSpecified = processes.Any();
            var warnManualInjection = opts.Warn;
            var tryFixAcl = opts.FixAcl || opts.AclFix;
            var tryClaimSeDebugPrivilege = opts.SeDebugPrivilege;

            if (opts.All)
            {
                targetProcessSpecified = true;
                processes.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
            }

            if (!targetProcessSpecified)
            {
                throw new CommandLineException(
                    "No target process has been specified. Use -a(--all) option to inject to all ffxiv_dx11.exe processes.");
            }

            if (!processes.Any())
            {
                Log.Error("No suitable target process has been found");
                return -1;
            }

            if (warnManualInjection)
            {
                var result = MessageBoxW(
                    nint.Zero,
                    $"Take care: you are manually injecting Dalamud into FFXIV({string.Join(", ", processes.Select(x => $"{x.Id}"))}).\n\nIf you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\nWe discourage you from doing this and you won't be warned again in-game.",
                    "Dalamud", MessageBoxType.IconWarning | MessageBoxType.OkCancel);

                // IDCANCEL
                if (result == 2)
                {
                    Log.Information("User cancelled injection");
                    return -2;
                }
            }

            if (tryClaimSeDebugPrivilege)
            {
                try
                {
                    GameStart.ClaimSeDebug();
                    Log.Information("SeDebugPrivilege claimed");
                }
                catch (Win32Exception e2)
                {
                    Log.Warning(e2, "Failed to claim SeDebugPrivilege");
                }
            }

            foreach (var process in processes)
                Inject(process, AdjustStartInfo(dalamudStartInfo, process.MainModule.FileName), tryFixAcl);

            return 0;
        }

        private static int ProcessLaunchCommand(LaunchOptions opts, DalamudStartInfo dalamudStartInfo)
        {
            var gamePath = opts.Game;
            var gameArguments = opts.GameArgs.ToList();
            var mode = opts.Mode;
            var useFakeArguments = opts.UseFakeArguments;
            var handleOwner = nint.Zero;
            var withoutDalamud = opts.WithoutDalamud;
            var noFixAcl = opts.NoFixAcl;
            var waitForGameWindow = !opts.NoWaitForGameWindow;
            var encryptArguments = false;

            if (!string.IsNullOrEmpty(opts.HandleOwner))
                handleOwner = nint.Parse(opts.HandleOwner);

            var checksumTable = "fX1pGtdS5CAP4_VL";
            var argDelimiterRegex = new Regex(" (?<!(?:^|[^ ])(?:  )*)/");
            var kvDelimiterRegex = new Regex(" (?<!(?:^|[^ ])(?:  )*)=");
            gameArguments = gameArguments.SelectMany(x =>
            {
                if (!x.StartsWith("//**sqex0003") || !x.EndsWith("**//"))
                    return new List<string>() { x };

                var checksum = checksumTable.IndexOf(x[^5]);
                if (checksum == -1)
                    return new List<string>() { x };

                var encData = Convert.FromBase64String(x.Substring(12, x.Length - 12 - 5).Replace('-', '+')
                                                        .Replace('_', '/').Replace('*', '='));
                var rawData = new byte[encData.Length];

                for (var i = (uint)checksum; i < 0x10000u; i += 0x10)
                {
                    var bf = new LegacyBlowfish(Encoding.UTF8.GetBytes($"{i << 16:x08}"));
                    Buffer.BlockCopy(encData, 0, rawData, 0, rawData.Length);
                    bf.Decrypt(ref rawData);
                    var rawString = Encoding.UTF8.GetString(rawData).Split('\0', 2).First();
                    encryptArguments = true;
                    var args = argDelimiterRegex.Split(rawString).Skip(1)
                                                .Select(y => string.Join('=', kvDelimiterRegex.Split(y, 2))
                                                                   .Replace("  ", " ")).ToList();
                    if (!args.Any())
                        continue;
                    if (!args.First().StartsWith("T="))
                        continue;
                    if (!uint.TryParse(args.First()[2..], out var tickCount))
                        continue;
                    if (tickCount >> 16 != i)
                        continue;
                    return args.Skip(1);
                }

                return new List<string>() { x };
            }).ToList();

            if (gamePath == null)
            {
                try
                {
                    var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");
                    var launcherConfigPath = Path.Combine(xivlauncherDir, "launcherConfigV3.json");
                    gamePath = Path.Combine(
                        JsonSerializer.CreateDefault()
                                      .Deserialize<Dictionary<string, string>>(
                                          new JsonTextReader(
                                              new StringReader(File.ReadAllText(launcherConfigPath))))["GamePath"],
                        "game",
                        "ffxiv_dx11.exe");
                    Log.Information("Using game installation path configuration from from XIVLauncher: {0}", gamePath);
                }
                catch (Exception)
                {
                    Log.Error(
                        "Failed to read launcherConfigV3.json to get the set-up game path, please specify one using -g");
                    return -1;
                }

                if (!File.Exists(gamePath))
                {
                    Log.Error("File not found: {0}", gamePath);
                    return -1;
                }
            }

            if (useFakeArguments)
            {
                var gameVersion =
                    File.ReadAllText(Path.Combine(Directory.GetParent(gamePath).FullName, "ffxivgame.ver"));
                var sqpackPath = Path.Combine(Directory.GetParent(gamePath).FullName, "sqpack");
                var maxEntitledExpansionId = 0;
                while (File.Exists(Path.Combine(sqpackPath, $"ex{maxEntitledExpansionId + 1}",
                                                $"ex{maxEntitledExpansionId + 1}.ver")))
                    maxEntitledExpansionId++;

                gameArguments.InsertRange(0, new[]
                {
                    "DEV.TestSID=0",
                    "DEV.UseSqPack=1",
                    "DEV.DataPathType=1",
                    "DEV.LobbyHost01=127.0.0.1",
                    "DEV.LobbyPort01=54994",
                    "DEV.LobbyHost02=127.0.0.2",
                    "DEV.LobbyPort02=54994",
                    "DEV.LobbyHost03=127.0.0.3",
                    "DEV.LobbyPort03=54994",
                    "DEV.LobbyHost04=127.0.0.4",
                    "DEV.LobbyPort04=54994",
                    "DEV.LobbyHost05=127.0.0.5",
                    "DEV.LobbyPort05=54994",
                    "DEV.LobbyHost06=127.0.0.6",
                    "DEV.LobbyPort06=54994",
                    "DEV.LobbyHost07=127.0.0.7",
                    "DEV.LobbyPort07=54994",
                    "DEV.LobbyHost08=127.0.0.8",
                    "DEV.LobbyPort08=54994",
                    "DEV.LobbyHost09=127.0.0.9",
                    "DEV.LobbyPort09=54994",
                    "SYS.Region=0",
                    $"language={(int)dalamudStartInfo.Language}",
                    $"ver={gameVersion}",
                    $"DEV.MaxEntitledExpansionID={maxEntitledExpansionId}",
                    "DEV.GMServerHost=127.0.0.100",
                    "DEV.GameQuitMessageBox=0",
                });
            }

            string gameArgumentString;
            if (encryptArguments)
            {
                var rawTickCount = (uint)Environment.TickCount;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    [DllImport("c")]
                    static extern ulong clock_gettime_nsec_np(int clock_id);

                    const int clockMonotonicRaw = 4;
                    var rawTickCountFixed = clock_gettime_nsec_np(clockMonotonicRaw) / 1000000;
                    Log.Information("ArgumentBuilder::DeriveKey() fixing up rawTickCount from {0} to {1} on macOS",
                                    rawTickCount, rawTickCountFixed);
                    rawTickCount = (uint)rawTickCountFixed;
                }

                var ticks = rawTickCount & 0xFFFF_FFFFu;
                var key = ticks & 0xFFFF_0000u;
                gameArguments.Insert(0, $"T={ticks}");

                string EscapeValue(string x) => x.Replace(" ", "  ");
                gameArgumentString = gameArguments.Select(x => x.Split('=', 2))
                                                  .Aggregate(new StringBuilder(),
                                                             (whole, part) =>
                                                                 whole.Append(
                                                                     $" /{EscapeValue(part[0])} ={EscapeValue(part.Length > 1 ? part[1] : string.Empty)}"))
                                                  .ToString();
                var bf = new LegacyBlowfish(Encoding.UTF8.GetBytes($"{key:x08}"));
                var ciphertext = bf.Encrypt(Encoding.UTF8.GetBytes(gameArgumentString));
                var base64Str = Convert.ToBase64String(ciphertext).Replace('+', '-').Replace('/', '_')
                                       .Replace('=', '*');
                var checksum = checksumTable[(int)(key >> 16) & 0xF];
                gameArgumentString = $"//**sqex0003{base64Str}{checksum}**//";
            }
            else
            {
                gameArgumentString = string.Join(" ", gameArguments.Select(x => EncodeParameterArgument(x)));
            }

            var process = GameStart.LaunchGame(
                Path.GetDirectoryName(gamePath),
                gamePath,
                gameArgumentString,
                noFixAcl,
                p =>
                {
                    if (!withoutDalamud && mode == LaunchMode.Entrypoint)
                    {
                        var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                        Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                        if (RewriteRemoteEntryPointW(p.Handle, gamePath, JsonConvert.SerializeObject(startInfo)) != 0)
                        {
                            Log.Error("[HOOKS] RewriteRemoteEntryPointW failed");
                            throw new Exception("RewriteRemoteEntryPointW failed");
                        }

                        Log.Verbose("RewriteRemoteEntryPointW called!");
                    }
                },
                waitForGameWindow);

            Log.Verbose("Game process started with PID {0}", process.Id);

            if (!withoutDalamud && mode == LaunchMode.Inject)
            {
                var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                Inject(process, startInfo, false);
            }

            var processHandleForOwner = nint.Zero;
            if (handleOwner != nint.Zero)
            {
                if (!DuplicateHandle(Process.GetCurrentProcess().Handle, process.Handle, handleOwner,
                                     out processHandleForOwner, 0, false, DuplicateOptions.SameAccess))
                    Log.Warning("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
            }

            Console.WriteLine($"{{\"pid\": {process.Id}, \"handle\": {processHandleForOwner}}}");

            return 0;
        }

        private static Process? GetInheritableCurrentProcessHandle()
        {
            if (!DuplicateHandle(Process.GetCurrentProcess().Handle, Process.GetCurrentProcess().Handle,
                                 Process.GetCurrentProcess().Handle, out var inheritableCurrentProcessHandle, 0, true,
                                 DuplicateOptions.SameAccess))
            {
                Log.Error("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
                return null;
            }

            return new ExistingProcess(inheritableCurrentProcessHandle);
        }

        private static int ProcessLaunchTestCommand(LaunchTestOptions opts)
        {
            Console.WriteLine("Testing launch command.");

            var args = new List<string>();
            args[0] = Process.GetCurrentProcess().MainModule.FileName;
            args[1] = "launch";

            var inheritableCurrentProcess =
                GetInheritableCurrentProcessHandle(); // so that it closes the handle when it's done
            args.Insert(2, $"--handle-owner={inheritableCurrentProcess.Handle}");

            for (var i = 0; i < args.Count; i++)
                Console.WriteLine("Argument {0}: {1}", i, args[i]);

            Process helperProcess = new();
            helperProcess.StartInfo.FileName = args[0];
            for (var i = 1; i < args.Count; i++)
                helperProcess.StartInfo.ArgumentList.Add(args[i]);
            helperProcess.StartInfo.RedirectStandardOutput = true;
            helperProcess.StartInfo.RedirectStandardError = true;
            helperProcess.StartInfo.UseShellExecute = false;
            helperProcess.ErrorDataReceived += (_, errLine) => Console.WriteLine($"stderr: \"{errLine.Data}\"");
            helperProcess.Start();
            helperProcess.BeginErrorReadLine();
            helperProcess.WaitForExit();
            if (helperProcess.ExitCode != 0)
                return -1;

            var result = JsonSerializer.CreateDefault()
                                       .Deserialize<Dictionary<string, int>>(
                                           new JsonTextReader(helperProcess.StandardOutput));
            var pid = result["pid"];
            var handle = (IntPtr)result["handle"];
            var resultProcess = new ExistingProcess(handle);
            Console.WriteLine("PID: {0}, Handle: {1}", pid, handle);
            Console.WriteLine("Press Enter to force quit");
            Console.ReadLine();
            resultProcess.Kill();
            return 0;
        }

        private static DalamudStartInfo AdjustStartInfo(DalamudStartInfo startInfo, string gamePath)
        {
            var ffxivDir = Path.GetDirectoryName(gamePath);
            var gameVerStr = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver"));
            var gameVer = GameVersion.Parse(gameVerStr);

            return new DalamudStartInfo(startInfo)
            {
                GameVersion = gameVer,
            };
        }

        private static void Inject(Process process, DalamudStartInfo startInfo, bool tryFixAcl = false)
        {
            if (tryFixAcl)
            {
                try
                {
                    GameStart.CopyAclFromSelfToTargetProcess(process.SafeHandle.DangerousGetHandle());
                }
                catch (Win32Exception e1)
                {
                    Log.Warning(e1, "Failed to copy ACL");
                }
            }

            var bootName = "Dalamud.Boot.dll";
            var bootPath = Path.GetFullPath(bootName);

            // ======================================================

            using var injector = new Injector(process, false);

            injector.LoadLibrary(bootPath, out var bootModule);

            // ======================================================

            var startInfoJson = JsonConvert.SerializeObject(startInfo);
            var startInfoBytes = Encoding.UTF8.GetBytes(startInfoJson);

            using var startInfoBuffer =
                new MemoryBufferHelper(process).CreatePrivateMemoryBuffer(startInfoBytes.Length + 0x8);
            var startInfoAddress = startInfoBuffer.Add(startInfoBytes);

            if (startInfoAddress == 0)
                throw new Exception("Unable to allocate start info JSON");

            injector.GetFunctionAddress(bootModule, "Initialize", out var initAddress);
            injector.CallRemoteFunction(initAddress, startInfoAddress, out var exitCode);

            // ======================================================

            if (exitCode > 0)
            {
                Log.Error($"Dalamud.Boot::Initialize returned {exitCode}");
                return;
            }

            Log.Information("Done");
        }

        [LibraryImport("Dalamud.Boot.dll")]
        private static partial int RewriteRemoteEntryPointW(
            IntPtr hProcess, [MarshalAs(UnmanagedType.LPWStr)] string gamePath,
            [MarshalAs(UnmanagedType.LPWStr)] string loadInfoJson);

        /// <summary>
        ///     This routine appends the given argument to a command line such that
        ///     CommandLineToArgvW will return the argument string unchanged. Arguments
        ///     in a command line should be separated by spaces; this function does
        ///     not add these spaces.
        ///
        ///     Taken from https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp
        ///     and https://blogs.msdn.microsoft.com/twistylittlepassagesallalike/2011/04/23/everyone-quotes-command-line-arguments-the-wrong-way/.
        /// </summary>
        /// <param name="argument">Supplies the argument to encode.</param>
        /// <param name="force">
        ///     Supplies an indication of whether we should quote the argument even if it
        ///     does not contain any characters that would ordinarily require quoting.
        /// </param>
        private static string EncodeParameterArgument(string argument, bool force = false)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            // Unless we're told otherwise, don't quote unless we actually
            // need to do so --- hopefully avoid problems if programs won't
            // parse quotes properly
            if (force == false
                && argument.Length > 0
                && argument.IndexOfAny(" \t\n\v\"".ToCharArray()) == -1)
            {
                return argument;
            }

            var quoted = new StringBuilder();
            quoted.Append('"');

            var numberBackslashes = 0;

            foreach (var chr in argument)
            {
                switch (chr)
                {
                    case '\\':
                        numberBackslashes++;
                        continue;
                    case '"':
                        // Escape all backslashes and the following
                        // double quotation mark.
                        quoted.Append('\\', (numberBackslashes * 2) + 1);
                        quoted.Append(chr);
                        break;
                    default:
                        // Backslashes aren't special here.
                        quoted.Append('\\', numberBackslashes);
                        quoted.Append(chr);
                        break;
                }

                numberBackslashes = 0;
            }

            // Escape all backslashes, but let the terminating
            // double quotation mark we add below be interpreted
            // as a metacharacter.
            quoted.Append('\\', numberBackslashes * 2);
            quoted.Append('"');

            return quoted.ToString();
        }

        private class CommandLineException : Exception
        {
            public CommandLineException(string cause)
                : base(cause) { }
        }
    }
}
