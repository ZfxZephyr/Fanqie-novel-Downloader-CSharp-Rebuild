using System.Text.Json;
using Windows.UI;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 阅读主题模式
/// </summary>
public enum ReaderThemeMode
{
    System,
    Light,
    Dark,
    Sepia,
    Green,
    Custom,
}

/// <summary>
/// 阅读主题服务：管理主阅读页与隐蔽模式小窗的主题设置，持久化到本地
/// </summary>
public class ReaderThemeService
{
    private readonly string _dataFilePath;

    /// <summary>当前主题模式（默认跟随系统）</summary>
    public ReaderThemeMode Mode { get; set; } = ReaderThemeMode.System;

    /// <summary>自定义背景色（#RRGGBB）</summary>
    public string CustomBackgroundHex { get; set; } = "#F5F0E8";

    /// <summary>自定义文字色（#RRGGBB）</summary>
    public string CustomForegroundHex { get; set; } = "#50432C";

    public ReaderThemeService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "FanqieNovelDownloader");
        _dataFilePath = Path.Combine(appDir, "reader-theme.json");
    }

    /// <summary>
    /// 获取当前主题颜色。跟随系统时返回 null（调用方使用系统主题资源）。
    /// </summary>
    public (Color Background, Color Foreground)? GetColors()
    {
        return Mode switch
        {
            ReaderThemeMode.Light => (FromRgb(255, 255, 255), FromRgb(26, 26, 26)),
            ReaderThemeMode.Dark => (FromRgb(30, 30, 30), FromRgb(220, 220, 220)),
            ReaderThemeMode.Sepia => (FromRgb(245, 240, 232), FromRgb(80, 67, 44)),
            ReaderThemeMode.Green => (FromRgb(199, 237, 204), FromRgb(43, 58, 43)),
            ReaderThemeMode.Custom => (ParseHex(CustomBackgroundHex), ParseHex(CustomForegroundHex)),
            _ => null,
        };
    }

    private static Color FromRgb(byte r, byte g, byte b) => Color.FromArgb(255, r, g, b);

    /// <summary>
    /// 解析 #RRGGBB / #AARRGGBB 格式颜色，失败时返回黑色
    /// </summary>
    public static Color ParseHex(string hex)
    {
        try
        {
            var value = hex.TrimStart('#');
            return value.Length switch
            {
                6 => Color.FromArgb(255,
                    Convert.ToByte(value.Substring(0, 2), 16),
                    Convert.ToByte(value.Substring(2, 2), 16),
                    Convert.ToByte(value.Substring(4, 2), 16)),
                8 => Color.FromArgb(
                    Convert.ToByte(value.Substring(0, 2), 16),
                    Convert.ToByte(value.Substring(2, 2), 16),
                    Convert.ToByte(value.Substring(4, 2), 16),
                    Convert.ToByte(value.Substring(6, 2), 16)),
                _ => Color.FromArgb(255, 0, 0, 0),
            };
        }
        catch
        {
            return Color.FromArgb(255, 0, 0, 0);
        }
    }

    /// <summary>
    /// 颜色转 #RRGGBB 字符串
    /// </summary>
    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// 从本地文件加载主题设置
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
                return;

            var json = await File.ReadAllTextAsync(_dataFilePath);
            var data = JsonSerializer.Deserialize<ReaderThemeData>(json);
            if (data != null)
            {
                Mode = data.Mode;
                CustomBackgroundHex = data.CustomBackgroundHex ?? CustomBackgroundHex;
                CustomForegroundHex = data.CustomForegroundHex ?? CustomForegroundHex;
            }
        }
        catch
        {
            // 配置损坏时使用默认值
        }
    }

    /// <summary>
    /// 保存主题设置到本地文件
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var appDir = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(appDir) && !Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }

            var data = new ReaderThemeData
            {
                Mode = Mode,
                CustomBackgroundHex = CustomBackgroundHex,
                CustomForegroundHex = CustomForegroundHex,
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
        }
        catch
        {
            // 保存失败不影响使用
        }
    }

    private class ReaderThemeData
    {
        public ReaderThemeMode Mode { get; set; } = ReaderThemeMode.System;
        public string? CustomBackgroundHex { get; set; }
        public string? CustomForegroundHex { get; set; }
    }
}
