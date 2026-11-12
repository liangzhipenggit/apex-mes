using System.Text.Json;

namespace Mes.WinUI;

/// <summary>界面字体等偏好，持久化到本地。</summary>
public sealed class UiSettings
{
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public float FontSize { get; set; } = 10.5F;
    public bool FontBold { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static UiSettings _current = new();

    public static UiSettings Current => _current;

    public static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IndustrialMES");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-settings.json");
        }
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                _current = new UiSettings();
                return;
            }

            var json = File.ReadAllText(FilePath);
            _current = JsonSerializer.Deserialize<UiSettings>(json, JsonOptions) ?? new UiSettings();
            Normalize(_current);
        }
        catch
        {
            _current = new UiSettings();
        }
    }

    public static void Save(UiSettings settings)
    {
        Normalize(settings);
        _current = settings;
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void Normalize(UiSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.FontFamily))
            s.FontFamily = "Microsoft YaHei UI";
        s.FontSize = Math.Clamp(s.FontSize, 9F, 18F);
    }
}
