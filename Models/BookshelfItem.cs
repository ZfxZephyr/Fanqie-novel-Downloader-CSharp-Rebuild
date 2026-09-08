using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FanqieNovelDownloader.Models;

/// <summary>
/// 书架项目
/// </summary>
public class BookshelfItem
{
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
    /// 封面URL
    /// </summary>
    [JsonPropertyName("coverUrl")]
    public string CoverUrl { get; set; } = string.Empty;

    /// <summary>
    /// 封面图片BitmapImage（用于XAML绑定）
    /// </summary>
    [JsonIgnore]
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
    /// 简介
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 最后阅读章节ID
    /// </summary>
    [JsonPropertyName("lastReadChapterId")]
    public string? LastReadChapterId { get; set; }

    /// <summary>
    /// 最后阅读章节标题
    /// </summary>
    [JsonPropertyName("lastReadChapterTitle")]
    public string? LastReadChapterTitle { get; set; }

    /// <summary>
    /// 最后阅读时间
    /// </summary>
    [JsonPropertyName("lastReadTime")]
    public DateTime? LastReadTime { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    [JsonPropertyName("addedTime")]
    public DateTime AddedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 总章节数
    /// </summary>
    [JsonPropertyName("totalChapters")]
    public int TotalChapters { get; set; }

    /// <summary>
    /// 最后阅读章节显示文本
    /// </summary>
    [JsonIgnore]
    public string LastReadDisplay => LastReadChapterTitle ?? "未开始阅读";

    /// <summary>
    /// 添加时间显示文本
    /// </summary>
    [JsonIgnore]
    public string AddedTimeDisplay => AddedTime.ToString("yyyy-MM-dd HH:mm");
}
