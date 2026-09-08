using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FanqieNovelDownloader.Models;

/// <summary>
/// 下载任务状态
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Waiting,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
}

/// <summary>
/// 导出格式
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// TXT格式
    /// </summary>
    Txt,

    /// <summary>
    /// EPUB格式
    /// </summary>
    Epub
}

/// <summary>
/// 下载任务模型
/// </summary>
public class DownloadTask : INotifyPropertyChanged
{
    private DownloadStatus _status = DownloadStatus.Waiting;
    private int _progress;
    private int _totalChapters;
    private int _completedChapters;
    private int _failedChapters;
    private string _errorMessage = string.Empty;
    private string _outputPath = string.Empty;

    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 书籍ID
    /// </summary>
    public string BookId { get; set; } = string.Empty;

    /// <summary>
    /// 书名
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 封面URL
    /// </summary>
    public string CoverUrl { get; set; } = string.Empty;

    /// <summary>
    /// 封面图片BitmapImage（用于XAML绑定）
    /// </summary>
    public BitmapImage? CoverImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CoverUrl))
                return null;
            return new BitmapImage(new Uri(CoverUrl));
        }
    }

    /// <summary>
    /// 导出格式
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Txt;

    /// <summary>
    /// 任务状态
    /// </summary>
    public DownloadStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    /// <summary>
    /// 总章节数
    /// </summary>
    public int TotalChapters
    {
        get => _totalChapters;
        set => SetProperty(ref _totalChapters, value);
    }

    /// <summary>
    /// 已完成章节数
    /// </summary>
    public int CompletedChapters
    {
        get => _completedChapters;
        set => SetProperty(ref _completedChapters, value);
    }

    /// <summary>
    /// 失败章节数
    /// </summary>
    public int FailedChapters
    {
        get => _failedChapters;
        set => SetProperty(ref _failedChapters, value);
    }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 输出文件路径
    /// </summary>
    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 是否可以暂停
    /// </summary>
    public bool CanPause => Status == DownloadStatus.Downloading;

    /// <summary>
    /// 是否可以继续
    /// </summary>
    public bool CanResume => Status == DownloadStatus.Paused || Status == DownloadStatus.Waiting;

    /// <summary>
    /// 是否可以取消
    /// </summary>
    public bool CanCancel => Status == DownloadStatus.Downloading || Status == DownloadStatus.Paused || Status == DownloadStatus.Waiting;

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry => Status == DownloadStatus.Failed;

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        DownloadStatus.Waiting => "等待中",
        DownloadStatus.Downloading => $"下载中 {Progress}%",
        DownloadStatus.Paused => "已暂停",
        DownloadStatus.Completed => "已完成",
        DownloadStatus.Failed => $"失败: {ErrorMessage}",
        DownloadStatus.Cancelled => "已取消",
        _ => "未知"
    };

    /// <summary>
    /// 进度显示文本
    /// </summary>
    public string ProgressText => $"{CompletedChapters}/{TotalChapters} 章";

    /// <summary>
    /// 格式显示文本
    /// </summary>
    public string FormatText => Format switch
    {
        ExportFormat.Txt => "TXT",
        ExportFormat.Epub => "EPUB",
        _ => "未知"
    };

    // INotifyPropertyChanged 实现
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
