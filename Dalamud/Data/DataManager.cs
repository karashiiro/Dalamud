using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using ImGuiScene;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Data
{
    /// <summary>
    /// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class DataManager : IDisposable, IDataManager
    {
        private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";

        private Thread luminaResourceThread;
        private CancellationTokenSource luminaCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataManager"/> class.
        /// </summary>
        internal DataManager()
        {
            this.Language = Service<DalamudStartInfo>.Get().Language;

            // Set up default values so plugins do not null-reference when data is being loaded.
            this.ClientOpCodes = this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());
        }

        /// <inheritdoc cref="IDataManager.Language"/>
        public ClientLanguage Language { get; private set; }

        /// <inheritdoc cref="IDataManager.ServerOpCodes"/>
        public ReadOnlyDictionary<string, ushort> ServerOpCodes { get; private set; }

        /// <inheritdoc cref="IDataManager.ClientOpCodes"/>
        public ReadOnlyDictionary<string, ushort> ClientOpCodes { get; private set; }

        /// <inheritdoc cref="IDataManager.GameData"/>
        public GameData GameData { get; private set; }

        /// <inheritdoc cref="IDataManager.Excel"/>
        public ExcelModule Excel => this.GameData?.Excel;

        /// <inheritdoc cref="IDataManager.IsDataReady"/>
        public bool IsDataReady { get; private set; }

        #region Lumina Wrappers

        /// <inheritdoc cref="IDataManager.GetExcelSheet{T}()"/>
        public ExcelSheet<T>? GetExcelSheet<T>() where T : ExcelRow
        {
            return this.Excel.GetSheet<T>();
        }

        /// <inheritdoc cref="IDataManager.GetExcelSheet{T}(ClientLanguage)"/>
        public ExcelSheet<T>? GetExcelSheet<T>(ClientLanguage language) where T : ExcelRow
        {
            return this.Excel.GetSheet<T>(language.ToLumina());
        }

        /// <inheritdoc cref="IDataManager.GetFile"/>
        public FileResource? GetFile(string path)
        {
            return this.GetFile<FileResource>(path);
        }

        /// <inheritdoc cref="IDataManager.GetFile{T}(string)"/>
        public T? GetFile<T>(string path) where T : FileResource
        {
            var filePath = GameData.ParseFilePath(path);
            if (filePath == null)
                return default;
            return this.GameData.Repositories.TryGetValue(filePath.Repository, out var repository) ? repository.GetFile<T>(filePath.Category, filePath) : default;
        }

        /// <inheritdoc cref="IDataManager.FileExists"/>
        public bool FileExists(string path)
        {
            return this.GameData.FileExists(path);
        }

        /// <inheritdoc cref="IDataManager.GetIcon(uint)"/>
        public TexFile? GetIcon(uint iconId)
        {
            return this.GetIcon(this.Language, iconId);
        }

        /// <inheritdoc cref="IDataManager.GetIcon(bool, uint)"/>
        public TexFile? GetIcon(bool isHq, uint iconId)
        {
            var type = isHq ? "hq/" : string.Empty;
            return this.GetIcon(type, iconId);
        }

        /// <inheritdoc cref="IDataManager.GetIcon(ClientLanguage, uint)"/>
        public TexFile? GetIcon(ClientLanguage iconLanguage, uint iconId)
        {
            var type = iconLanguage switch
            {
                ClientLanguage.Japanese => "ja/",
                ClientLanguage.English => "en/",
                ClientLanguage.German => "de/",
                ClientLanguage.French => "fr/",
                _ => throw new ArgumentOutOfRangeException(nameof(iconLanguage), $"Unknown Language: {iconLanguage}"),
            };

            return this.GetIcon(type, iconId);
        }

        /// <inheritdoc cref="IDataManager.GetIcon(string, uint)"/>
        public TexFile? GetIcon(string type, uint iconId)
        {
            type ??= string.Empty;
            if (type.Length > 0 && !type.EndsWith("/"))
                type += "/";

            var filePath = string.Format(IconFileFormat, iconId / 1000, type, iconId);
            var file = this.GetFile<TexFile>(filePath);

            if (type == string.Empty || file != default)
                return file;

            // Couldn't get specific type, try for generic version.
            filePath = string.Format(IconFileFormat, iconId / 1000, string.Empty, iconId);
            file = this.GetFile<TexFile>(filePath);
            return file;
        }

        /// <inheritdoc cref="IDataManager.GetHqIcon(uint)"/>
        public TexFile? GetHqIcon(uint iconId)
            => this.GetIcon(true, iconId);

        /// <inheritdoc cref="IDataManager.GetImGuiTexture(TexFile?)"/>
        public TextureWrap? GetImGuiTexture(TexFile? tex)
        {
            return tex == null ? null : Service<InterfaceManager>.Get().LoadImageRaw(tex.GetRgbaImageData(), tex.Header.Width, tex.Header.Height, 4);
        }

        /// <inheritdoc cref="IDataManager.GetImGuiTexture(string)"/>
        public TextureWrap? GetImGuiTexture(string path)
            => this.GetImGuiTexture(this.GetFile<TexFile>(path));

        /// <inheritdoc cref="IDataManager.GetImGuiTextureIcon(uint)"/>
        public TextureWrap? GetImGuiTextureIcon(uint iconId)
            => this.GetImGuiTexture(this.GetIcon(iconId));

        /// <inheritdoc cref="IDataManager.GetImGuiTextureIcon(bool, uint)"/>
        public TextureWrap? GetImGuiTextureIcon(bool isHq, uint iconId)
            => this.GetImGuiTexture(this.GetIcon(isHq, iconId));

        /// <inheritdoc cref="IDataManager.GetImGuiTextureIcon(ClientLanguage, uint)"/>
        public TextureWrap? GetImGuiTextureIcon(ClientLanguage iconLanguage, uint iconId)
            => this.GetImGuiTexture(this.GetIcon(iconLanguage, iconId));

        /// <inheritdoc cref="IDataManager.GetImGuiTextureIcon(string, uint)"/>
        public TextureWrap? GetImGuiTextureIcon(string type, uint iconId)
            => this.GetImGuiTexture(this.GetIcon(type, iconId));

        /// <inheritdoc cref="IDataManager.GetImGuiTextureHqIcon(uint)"/>
        public TextureWrap? GetImGuiTextureHqIcon(uint iconId)
            => this.GetImGuiTexture(this.GetHqIcon(iconId));

        #endregion

        /// <summary>
        /// Dispose this DataManager.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.luminaCancellationTokenSource.Cancel();
        }

        /// <inheritdoc cref="IDataManager.Initialize"/>
        void IDataManager.Initialize(string baseDir)
        {
            try
            {
                Log.Verbose("Starting data load...");

                var zoneOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                    File.ReadAllText(Path.Combine(baseDir, "UIRes", "serveropcode.json")));
                this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(zoneOpCodeDict);

                Log.Verbose("Loaded {0} ServerOpCodes.", zoneOpCodeDict.Count);

                var clientOpCodeDict = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                    File.ReadAllText(Path.Combine(baseDir, "UIRes", "clientopcode.json")));
                this.ClientOpCodes = new ReadOnlyDictionary<string, ushort>(clientOpCodeDict);

                Log.Verbose("Loaded {0} ClientOpCodes.", clientOpCodeDict.Count);

                var luminaOptions = new LuminaOptions
                {
                    CacheFileResources = true,
#if DEBUG
                    PanicOnSheetChecksumMismatch = true,
#else
                    PanicOnSheetChecksumMismatch = false,
#endif
                    DefaultExcelLanguage = this.Language.ToLumina(),
                };

                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    this.GameData = new GameData(Path.Combine(Path.GetDirectoryName(processModule.FileName), "sqpack"), luminaOptions);
                }

                Log.Information("Lumina is ready: {0}", this.GameData.DataPath);

                this.IsDataReady = true;

                this.luminaCancellationTokenSource = new();

                var luminaCancellationToken = this.luminaCancellationTokenSource.Token;
                this.luminaResourceThread = new(() =>
                {
                    while (!luminaCancellationToken.IsCancellationRequested)
                    {
                        if (this.GameData.FileHandleManager.HasPendingFileLoads)
                        {
                            this.GameData.ProcessFileHandleQueue();
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                });
                this.luminaResourceThread.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not download data.");
            }
        }
    }
}
