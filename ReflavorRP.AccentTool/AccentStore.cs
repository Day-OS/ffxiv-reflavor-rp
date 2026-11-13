using System.Text.Json;

namespace ReflavorRP.AccentTool;

// The tool's own accents live in %APPDATA%\ReflavorAccents\accents.json.
internal static class AccentStore
{
    private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string FilePath = Path.Combine(AppData, "ReflavorAccents", "accents.json");
    private static readonly string PluginConfigPath = Path.Combine(AppData, "XIVLauncher", "pluginConfigs", "ReflavorRP.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<Accent> Load()
    {
        if (!File.Exists(FilePath))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<Accent>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch (JsonException)
        {
            // Keep the unreadable file instead of overwriting it on the next save.
            File.Copy(FilePath, FilePath + ".bak", overwrite: true);
            return [];
        }
    }

    // Written to a temporary file first, so a crash mid-write never leaves a half file.
    public static void Save(IEnumerable<Accent> accents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(accents, Options));
        File.Move(temp, FilePath, overwrite: true);
    }

    // Custom accents made in the plugin. Dalamud tags objects with "$type", so fields are read by hand.
    public static List<Accent> ReadPluginAccents()
    {
        if (!File.Exists(PluginConfigPath))
            return [];
        using var stream = new FileStream(PluginConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("CustomAccents", out var accents) || accents.ValueKind != JsonValueKind.Array)
            return [];

        return accents.EnumerateArray()
            .Select(accent => new Accent
            {
                Name = ReadString(accent, "Name"),
                Rules = accent.TryGetProperty("Rules", out var rules) && rules.ValueKind == JsonValueKind.Array
                    ? rules.EnumerateArray().Select(rule => new AccentRule
                    {
                        Pattern = ReadString(rule, "Pattern"),
                        Replacement = ReadString(rule, "Replacement"),
                        Chance = rule.TryGetProperty("Chance", out var chance) && chance.TryGetInt32(out var value) ? value : 100,
                    }).ToList()
                    : [],
            })
            .Where(accent => accent.Name != "")
            .ToList();
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
}
