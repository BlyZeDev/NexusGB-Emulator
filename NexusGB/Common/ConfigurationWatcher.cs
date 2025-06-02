namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using NexusGB.Statics;
using System.Text.Json;

public sealed class ConfigurationWatcher : IDisposable
{
    private static readonly EmulatorConfig _defaultConfig = new EmulatorConfig
    {
        Color1 = new NexusColor(0xFF, 0xFF, 0xFF),
        Color2 = new NexusColor(0x80, 0x80, 0x80),
        Color3 = new NexusColor(0x40, 0x40, 0x40),
        Color4 = new NexusColor(0x00, 0x00, 0x00),
        BackgroundColor = NexusColor.Black,
        Controls = new Dictionary<NexusKey, byte>
        {
            { NexusKey.Up, 0x14 },
            { NexusKey.Left, 0x12 },
            { NexusKey.Right, 0x11 },
            { NexusKey.Down, 0x18 },
            { NexusKey.A, 0x21 },
            { NexusKey.B, 0x22 },
            { NexusKey.Back, 0x24 },
            { NexusKey.Return, 0x28 }
        }
    };

    private readonly FileSystemWatcher _watcher;

    public string ConfigPath { get; }

    public EmulatorConfig Current { get; private set; }

    public event EventHandler<EmulatorConfig>? Changed;

    public ConfigurationWatcher()
    {
        var directory = AppContext.BaseDirectory;
        var file = "NexusGB.config";

        ConfigPath = Path.Combine(directory, file);

        Logger.LogInfo($"The configuration path is {ConfigPath}");

        if (!File.Exists(ConfigPath)) WriteConfigFile(ConfigPath, _defaultConfig);
        Current = ReadConfigFile(ConfigPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {ConfigPath}");

        _watcher = new FileSystemWatcher
        {
            Path = directory,
            Filter = file,
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnConfigChange;
        _watcher.Deleted += OnConfigDeleted;
    }

    public void Dispose() => _watcher.Dispose();

    private void OnConfigChange(object sender, FileSystemEventArgs e)
    {
        var old = Current;
        Current = ReadConfigFile(e.FullPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {e.FullPath}");
        Changed?.Invoke(this, old);
    }

    private void OnConfigDeleted(object sender, FileSystemEventArgs e) => WriteConfigFile(e.FullPath, _defaultConfig);

    private static EmulatorConfig ReadConfigFile(string filepath)
    {
        try
        {
            return Json.ReadFromFile<EmulatorConfig>(filepath);
        }
        catch (JsonException jsonEx)
        {
            Logger.LogWarning("Couldn't parse the configuration file, maybe it's invalid", jsonEx);
        }
        catch (Exception ex)
        {
            Logger.LogError("Something went wrong reading the configuration file", ex);
        }

        Logger.LogWarning("Resetting the configuration file to default");

        File.Delete(filepath);
        return _defaultConfig;
    }

    private static void WriteConfigFile(string filepath, EmulatorConfig config)
    {
        Json.WriteToFile(filepath, config);
        Logger.LogDebug("Updated config file");
    }
}