namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
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
        Color4 = new NexusColor(0x00, 0x00, 0x00)
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
        Current = ReadJsonFromFile<EmulatorConfig>(fullPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {fullPath}");

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
        Current = ReadJsonFromFile<EmulatorConfig>(e.FullPath) ?? throw new FileNotFoundException($"The configuration file was not found.\nThe path should be {e.FullPath}");
        Changed?.Invoke(this, old);
    }

    private void OnConfigDeleted(object sender, FileSystemEventArgs e) => WriteJsonToFile(e.FullPath, _defaultConfig);

    private static T? ReadJsonFromFile<T>(string filepath)
    {
        using (var reader = new StreamReader(filepath))
        {
            return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), _options);
        }
    }

    private static void WriteJsonToFile<T>(string filepath, T jObject)
    {
        using (var writer = new StreamWriter(filepath))
        {
            writer.Write(JsonSerializer.Serialize(jObject, _options));
            writer.Flush();
        }
    }
}