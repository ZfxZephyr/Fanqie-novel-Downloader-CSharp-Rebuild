using System.Text.Json.Serialization;

namespace FanqieNovelDownloader.Models;

/// <summary>
/// 历史记录项目
/// </summary>
public class HistoryItem
{
    /// <summary>
    /// 记录ID
    /// </summary>
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 书籍ID
    /// </summary>
    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    /// <summary>
    /// 书名
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 导出格式
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "TXT";

    /// <summary>
    /// 文件路径
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// 下载时间
    /// </summary>
    [JsonPropertyName("downloadTime")]
    public DateTime DownloadTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 文件大小显示文本
    /// </summary>
    [JsonIgnore]
    public string FileSizeDisplay => FileSize switch
    {
        >= 1073741824 => $"{FileSize / 1073741824.0:F2} GB",
        >= 1048576 => $"{FileSize / 1048576.0:F2} MB",
        >= 1024 => $"{FileSize / 1024.0:F2} KB",
        _ => $"{FileSize} B"
    };

    /// <summary>
    /// 下载时间显示文本
    /// </summary>
    [JsonIgnore]
    public string DownloadTimeDisplay => DownloadTime.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 文件是否存在
    /// </summary>
    [JsonIgnore]
    public bool FileExists => File.Exists(FilePath);
}
