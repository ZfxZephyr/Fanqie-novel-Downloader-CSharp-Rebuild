using System.Text.Json.Serialization;

namespace FanqieNovelDownloader.Models;

/// <summary>
/// API搜索响应
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// API来源标识
    /// </summary>
    [JsonPropertyName("api_source")]
    public string? ApiSource { get; set; }

    /// <summary>
    /// 搜索结果数据
    /// </summary>
    [JsonPropertyName("data")]
    public List<BookInfo>? Data { get; set; }
}

/// <summary>
/// 书籍详情响应
/// </summary>
public class BookDetailResponse
{
    [JsonPropertyName("api_source")]
    public string? ApiSource { get; set; }

    [JsonPropertyName("data")]
    public BookDetailData? Data { get; set; }
}

/// <summary>
/// 书籍详情数据
/// </summary>
public class BookDetailData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("thumb")]
    public string CoverUrl { get; set; } = string.Empty;

    [JsonPropertyName("docs")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("word_number")]
    public long WordCount { get; set; }

    [JsonPropertyName("read_count")]
    public long ReadCount { get; set; }

    [JsonPropertyName("serial")]
    public int ChapterCount { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

/// <summary>
/// 章节列表响应
/// </summary>
public class ChapterListResponse
{
    [JsonPropertyName("api_source")]
    public string? ApiSource { get; set; }

    [JsonPropertyName("data")]
    public List<ChapterInfo>? Data { get; set; }
}

/// <summary>
/// 章节信息
/// </summary>
public class ChapterInfo
{
    [JsonPropertyName("item_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("volume_name")]
    public string VolumeName { get; set; } = string.Empty;
}

/// <summary>
/// 章节内容响应
/// </summary>
public class ChapterContentResponse
{
    [JsonPropertyName("api_source")]
    public string? ApiSource { get; set; }

    [JsonPropertyName("data")]
    public ChapterContentData? Data { get; set; }
}

/// <summary>
/// 章节内容数据
/// </summary>
public class ChapterContentData
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
