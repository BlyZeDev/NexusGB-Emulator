namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using NexusGB.Statics;
using System.Reflection;
using System.Text.Json;

public sealed class ConfigurationWatcher : IDisposable
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

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

    public EmulatorConfig Current { get; private set; }

    public event EventHandler<EmulatorConfig>? Changed;

    public ConfigurationWatcher()
    {
        var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory;
        var file = "NexusGB.config";

        var fullPath = Path.Combine(directory, file);

        if (!File.Exists(fullPath)) WriteJsonToFile(fullPath, _defaultConfig);
        Current = ReadJsonFromFile(fullPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {fullPath}");

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
        Current = ReadJsonFromFile(e.FullPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {e.FullPath}");
        Changed?.Invoke(this, old);
    }

    private void OnConfigDeleted(object sender, FileSystemEventArgs e) => WriteJsonToFile(e.FullPath, _defaultConfig);

    private static EmulatorConfig ReadJsonFromFile(string filepath)
    {
        try
        {
            using (var reader = new StreamReader(filepath))
            {
                return JsonSerializer.Deserialize<EmulatorConfig>(reader.ReadToEnd(), _options)
                    ?? throw new JsonException("The deserialization result was null", filepath, null, null);
            }
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

    private static void WriteJsonToFile(string filepath, EmulatorConfig jObject)
    {
        using (var writer = new StreamWriter(filepath))
        {
            writer.Write(JsonSerializer.Serialize(jObject, _options));
            writer.Flush();
        }
    }
}