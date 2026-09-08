using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FanqieNovelDownloader.Models;

/// <summary>
/// 书籍信息模型
/// </summary>
public class BookInfo
{
    /// <summary>
    /// 书籍ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

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
    /// 封面图片URL
    /// </summary>
    [JsonPropertyName("thumb")]
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
    /// 简介/描述
    /// </summary>
    [JsonPropertyName("docs")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 字数
    /// </summary>
    [JsonPropertyName("word_number")]
    public long WordCount { get; set; }

    /// <summary>
    /// 在读人数
    /// </summary>
    [JsonPropertyName("read_count")]
    public long ReadCount { get; set; }

    /// <summary>
    /// 更新章节数量
    /// </summary>
    [JsonPropertyName("serial")]
    public int ChapterCount { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 格式化后的字数显示
    /// </summary>
    [JsonIgnore]
    public string WordCountDisplay => WordCount switch
    {
        >= 10000 => $"{WordCount / 10000.0:F1}万字",
        _ => $"{WordCount}字"
    };

    /// <summary>
    /// 格式化后的在读人数显示
    /// </summary>
    [JsonIgnore]
    public string ReadCountDisplay => ReadCount switch
    {
        >= 10000 => $"{ReadCount / 10000.0:F1}万人在读",
        _ => $"{ReadCount}人在读"
    };

    /// <summary>
    /// 格式化后的章节数显示
    /// </summary>
    [JsonIgnore]
    public string ChapterCountDisplay => ChapterCount switch
    {
        >= 1000 => $"{ChapterCount / 1000.0:F1}k章",
        _ => $"{ChapterCount}章"
    };
}
