using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMA.Utilities.DataverseExporter;

/// <summary>
/// Persisted configuration for the exporter. Saved to dvexporter-config.json
/// alongside the executable so settings survive between runs.
/// </summary>
public class AppConfig
{
    public string DataverseUrl { get; set; } = string.Empty;
    public string ExportDir { get; set; } = string.Empty;
    public string PatchDir { get; set; } = string.Empty;

    // ── Persistence ───────────────────────────────────────────────────────────

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BlackInk", "DataverseExporter", "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
            }
        }
        catch { /* first run or corrupt file — start fresh */ }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { /* non-fatal — just means config won't persist */ }
    }
}