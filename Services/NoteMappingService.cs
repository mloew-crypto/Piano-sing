using System.Text.Json;

namespace PianoApp.Services;

public class NoteMappingService
{
    private const string MappingFileName = "NoteMapping.json";

    public Dictionary<string, string> NoteToPath { get; } = new();
    public string AudioBasePath { get; private set; } = "";
    /// <summary>When true, audio paths are loaded from the app package (e.g. Android assets).</summary>
    public bool UsePackageAssets { get; private set; }

    public void Load()
    {
        string path = Path.Combine(FileSystem.AppDataDirectory, MappingFileName);
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, MappingFileName);

#if ANDROID
        // On Android, also try from app package (bundled with app)
        if (!File.Exists(path))
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(MappingFileName).GetAwaiter().GetResult();
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    ParseMapping(reader.ReadToEnd(), fromPackage: true);
                    return;
                }
            }
            catch { /* not in package */ }
        }
#endif

        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        ParseMapping(json, fromPackage: false);
    }

    private void ParseMapping(string json, bool fromPackage)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict == null) return;

        UsePackageAssets = fromPackage;
        AudioBasePath = fromPackage ? "" : AppContext.BaseDirectory;
        foreach (var kv in dict)
        {
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            string key = kv.Key.Trim();
            if (string.IsNullOrEmpty(key) || key.StartsWith("//")) continue;
            if (key.Equals("_basePath", StringComparison.OrdinalIgnoreCase))
            {
                if (!fromPackage)
                {
                    string basePath = (kv.Value ?? "").Trim();
                    if (!string.IsNullOrEmpty(basePath))
                        AudioBasePath = Path.IsPathRooted(basePath)
                            ? basePath
                            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, basePath));
                }
                continue;
            }
            string normalized = NormalizeNoteKey(key);
            NoteToPath[normalized] = kv.Value;
            if (normalized.Contains('#'))
                NoteToPath[normalized.Replace("#", "s")] = kv.Value;
            string? sharpKey = FlatToSharp(normalized);
            if (sharpKey != null)
                NoteToPath[sharpKey] = kv.Value;
        }
    }

    private static string NormalizeNoteKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return key;
        key = key.Trim()
            .Replace('\uFF03', '#')
            .Replace("\u266F", "#");
        if (key.Length > 0 && char.IsLetter(key[0]))
            key = char.ToUpperInvariant(key[0]) + key[1..];
        return key;
    }

    private static string? FlatToSharp(string key)
    {
        if (key.Length < 2 || key[1] != 'b') return null;
        string? sharp = key[0] switch
        {
            'D' => "C#",
            'E' => "D#",
            'G' => "F#",
            'A' => "G#",
            'B' => "A#",
            _ => null
        };
        return sharp == null ? null : sharp + key[2..];
    }

    /// <summary>Returns filesystem path, or "package:relativePath" when UsePackageAssets is true.</summary>
    public string? GetAudioPath(string noteName)
    {
        string key = NormalizeNoteKey(noteName);
        if (!NoteToPath.TryGetValue(key, out var path) || string.IsNullOrWhiteSpace(path))
            NoteToPath.TryGetValue(key.Replace("#", "s"), out path);
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (UsePackageAssets)
            return "package:" + path.Replace('\\', '/');
        string baseDir = string.IsNullOrEmpty(AudioBasePath) ? AppContext.BaseDirectory : AudioBasePath;
        string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(baseDir, path);
        return File.Exists(fullPath) ? fullPath : null;
    }
}
