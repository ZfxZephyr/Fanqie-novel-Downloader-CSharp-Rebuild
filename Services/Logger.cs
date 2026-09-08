using System.Diagnostics;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 日志服务
/// </summary>
public static class Logger
{
    private static readonly string LogDirectory;
    private static readonly string LogFilePath;
    private static readonly object _lock = new();

    static Logger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        LogDirectory = Path.Combine(appData, "FanqieNovelDownloader", "logs");
        Directory.CreateDirectory(LogDirectory);
        LogFilePath = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var logMessage = ex != null ? $"{message}\n{ex}" : message;
        WriteLog("ERROR", logMessage);
    }

    public static void Debug(string message)
    {
        WriteLog("DEBUG", message);
    }

    private static void WriteLog(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
        }
        catch
        {
            // 日志写入失败不影响主程序
        }
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public static string GetLogFilePath() => LogFilePath;

    /// <summary>
    /// 获取日志目录
    /// </summary>
    public static string GetLogDirectory() => LogDirectory;
}
