using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FanqieNovelDownloader.Models;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 番茄小说API服务
/// 对接 http://101.35.133.34:5000/
/// </summary>
public class FanqieApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string BaseUrl = "http://101.35.133.34:5000";

    public FanqieApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 发送GET请求并记录详细日志
    /// </summary>
    private async Task<string> GetAsync(string url, string apiName)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.Info($"[{apiName}] ═══════════════════════════════════════");
        Logger.Info($"[{apiName}] HTTP方法: GET");
        Logger.Info($"[{apiName}] 请求URL: {url}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            Logger.Info($"[{apiName}] 响应状态码: {(int)response.StatusCode} {response.StatusCode}");
            Logger.Info($"[{apiName}] 耗时: {stopwatch.ElapsedMilliseconds}ms");

            var json = await response.Content.ReadAsStringAsync();
            Logger.Debug($"[{apiName}] 响应内容长度: {json.Length} 字符");

            if (json.Length > 500)
            {
                Logger.Debug($"[{apiName}] 响应内容(前500): {json.Substring(0, 500)}...");
            }
            else
            {
                Logger.Debug($"[{apiName}] 响应内容: {json}");
            }

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error($"[{apiName}] 请求失败: {response.StatusCode}");
                throw new Exception($"API请求失败: {response.StatusCode}");
            }

            return json;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.Error($"[{apiName}] 网络请求异常 (耗时{stopwatch.ElapsedMilliseconds}ms)", ex);
            throw new Exception($"网络请求失败: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            Logger.Error($"[{apiName}] 请求超时 (耗时{stopwatch.ElapsedMilliseconds}ms)", ex);
            throw new Exception("请求超时，请检查网络连接", ex);
        }
    }

    /// <summary>
    /// 搜索小说
    /// </summary>
    public async Task<List<BookInfo>> SearchAsync(string keyword)
    {
        Logger.Info($"[搜索] 关键词: {keyword}");
        try
        {
            var url = $"{BaseUrl}/api/search?key={Uri.EscapeDataString(keyword)}&tab_type=3";
            var json = await GetAsync(url, "搜索");

            var result = JsonSerializer.Deserialize<SearchRootResponse>(json, _jsonOptions);

            if (result?.Code != 200 || result.Data == null)
            {
                Logger.Info($"[搜索] API返回错误: code={result?.Code}, message={result?.Message}");
                return new List<BookInfo>();
            }

            var books = new List<BookInfo>();

            // 从 search_tabs 中找到 tab_type=3 (书籍) 的数据
            var bookTab = result.Data.SearchTabs?.FirstOrDefault(t => t.TabType == 3);
            if (bookTab?.Data == null)
            {
                Logger.Info("[搜索] 未找到书籍数据");
                return new List<BookInfo>();
            }

            foreach (var item in bookTab.Data)
            {
                var bookData = item.BookData?.FirstOrDefault();
                if (bookData == null) continue;

                books.Add(new BookInfo
                {
                    Id = bookData.BookId ?? string.Empty,
                    Title = bookData.BookName ?? string.Empty,
                    Author = bookData.Author ?? "未知",
                    CoverUrl = bookData.ThumbUrl ?? string.Empty,
                    Description = bookData.Abstract ?? string.Empty,
                    WordCount = long.TryParse(bookData.WordNumber, out var wn) ? wn : 0,
                    ReadCount = long.TryParse(bookData.ReadCount, out var rc) ? rc : 0,
                    ChapterCount = int.TryParse(bookData.SerialCount, out var sc) ? sc : 0,
                    Tags = bookData.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                });
            }

            Logger.Info($"[搜索] 成功，找到 {books.Count} 本书");
            foreach (var book in books)
            {
                Logger.Debug($"[搜索]   - {book.Title} (ID:{book.Id}, 作者:{book.Author})");
            }

            return books;
        }
        catch (JsonException ex)
        {
            Logger.Error("[搜索] JSON解析失败", ex);
            throw new Exception($"数据解析失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取书籍详情
    /// </summary>
    public async Task<BookDetailData?> GetBookDetailAsync(string bookId)
    {
        Logger.Info($"[详情] 书籍ID: {bookId}");
        try
        {
            var url = $"{BaseUrl}/api/detail?book_id={bookId}";
            var json = await GetAsync(url, "详情");

            var result = JsonSerializer.Deserialize<ApiResponse<BookDetailWrapper>>(json, _jsonOptions);

            if (result?.Code != 200 || result.Data?.Data == null)
            {
                Logger.Info($"[详情] API返回错误: code={result?.Code}, message={result?.Message}");
                return null;
            }

            var data = result.Data.Data;
            Logger.Info($"[详情] 成功: {data.BookName}");

            return new BookDetailData
            {
                Id = bookId,
                Title = data.BookName ?? string.Empty,
                Author = data.Author ?? "未知",
                CoverUrl = data.ThumbUrl ?? string.Empty,
                Description = data.Abstract ?? string.Empty,
                WordCount = long.TryParse(data.WordNumber, out var wn) ? wn : 0,
                ReadCount = long.TryParse(data.ReadCount, out var rc) ? rc : 0,
                ChapterCount = int.TryParse(data.SerialCount, out var sc) ? sc : 0,
                Tags = data.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }
        catch (Exception ex)
        {
            Logger.Error("[详情] 获取失败", ex);
            return null;
        }
    }

    /// <summary>
    /// 获取章节列表
    /// </summary>
    public async Task<List<ChapterInfo>> GetChapterListAsync(string bookId)
    {
        Logger.Info($"[目录] 书籍ID: {bookId}");
        try
        {
            var url = $"{BaseUrl}/api/directory?book_id={bookId}";
            var json = await GetAsync(url, "目录");

            var result = JsonSerializer.Deserialize<DirectoryResponse>(json, _jsonOptions);

            if (result?.Code != 200 || result.Data?.Lists == null)
            {
                Logger.Info($"[目录] API返回错误: code={result?.Code}, message={result?.Message}");
                return new List<ChapterInfo>();
            }

            var chapters = new List<ChapterInfo>();

            foreach (var item in result.Data.Lists)
            {
                chapters.Add(new ChapterInfo
                {
                    Id = item.ItemId ?? string.Empty,
                    Title = item.Title ?? $"第{chapters.Count + 1}章",
                    VolumeName = string.Empty
                });
            }

            Logger.Info($"[目录] 成功，共 {chapters.Count} 章");
            return chapters;
        }
        catch (Exception ex)
        {
            Logger.Error("[目录] 获取失败", ex);
            return new List<ChapterInfo>();
        }
    }

    /// <summary>
    /// 获取章节内容
    /// </summary>
    public async Task<string> GetChapterContentAsync(string chapterId)
    {
        Logger.Info($"[内容] 章节ID: {chapterId}");
        try
        {
            var url = $"{BaseUrl}/api/content?item_id={chapterId}&tab=小说";
            var json = await GetAsync(url, "内容");

            var result = JsonSerializer.Deserialize<ApiResponse<ChapterContentItem>>(json, _jsonOptions);

            if (result?.Code != 200 || result.Data == null)
            {
                Logger.Info($"[内容] API返回错误: code={result?.Code}, message={result?.Message}");
                return string.Empty;
            }

            var content = result.Data.Content ?? result.Data.NovelData?.ShowNaviCnBody ?? string.Empty;
            Logger.Info($"[内容] 成功，内容长度: {content.Length} 字符");
            return content;
        }
        catch (Exception ex)
        {
            Logger.Error("[内容] 获取失败", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// 解析番茄小说链接或ID
    /// </summary>
    public static string ParseBookId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.Trim();

        if (long.TryParse(input, out _))
            return input;

        try
        {
            var uri = new Uri(input);
            var segments = uri.Segments;
            if (segments.Length > 0)
            {
                var lastSegment = segments[^1].TrimEnd('/');
                if (long.TryParse(lastSegment, out _))
                    return lastSegment;
            }
        }
        catch { }

        return input;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// ═══════════════════════════════════════════════════
// 搜索API响应模型
// ═══════════════════════════════════════════════════

internal class SearchRootResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("data")]
    public SearchRootData? Data { get; set; }
    [JsonPropertyName("elapsed_ms")]
    public int ElapsedMs { get; set; }
}

internal class SearchRootData
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("search_tabs")]
    public List<SearchTab>? SearchTabs { get; set; }
}

internal class SearchTab
{
    [JsonPropertyName("tab_type")]
    public int TabType { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("data")]
    public List<SearchResultItem>? Data { get; set; }
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
    [JsonPropertyName("next_offset")]
    public int NextOffset { get; set; }
}

internal class SearchResultItem
{
    [JsonPropertyName("book_id")]
    public string? BookId { get; set; }
    [JsonPropertyName("book_data")]
    public List<BookDataItem>? BookData { get; set; }
}

internal class BookDataItem
{
    [JsonPropertyName("book_id")]
    public string? BookId { get; set; }
    [JsonPropertyName("book_name")]
    public string? BookName { get; set; }
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    [JsonPropertyName("thumb_url")]
    public string? ThumbUrl { get; set; }
    [JsonPropertyName("abstract")]
    public string? Abstract { get; set; }
    [JsonPropertyName("word_number")]
    public string? WordNumber { get; set; }
    [JsonPropertyName("read_count")]
    public string? ReadCount { get; set; }
    [JsonPropertyName("serial_count")]
    public string? SerialCount { get; set; }
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    [JsonPropertyName("score")]
    public string? Score { get; set; }
}

// ═══════════════════════════════════════════════════
// 通用API响应模型
// ═══════════════════════════════════════════════════

internal class ApiResponse<T>
{
    public int Code { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int ElapsedMs { get; set; }
}

// 书籍详情外层包装
internal class BookDetailWrapper
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("data")]
    public BookDetailItem? Data { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// 书籍详情
internal class BookDetailItem
{
    [JsonPropertyName("book_id")]
    public string? BookId { get; set; }
    [JsonPropertyName("book_name")]
    public string? BookName { get; set; }
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    [JsonPropertyName("thumb_url")]
    public string? ThumbUrl { get; set; }
    [JsonPropertyName("abstract")]
    public string? Abstract { get; set; }
    [JsonPropertyName("word_number")]
    public string? WordNumber { get; set; }
    [JsonPropertyName("read_count")]
    public string? ReadCount { get; set; }
    [JsonPropertyName("serial_count")]
    public string? SerialCount { get; set; }
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }
}

// 目录响应
internal class DirectoryResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("data")]
    public DirectoryData? Data { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("elapsed_ms")]
    public int ElapsedMs { get; set; }
}

// 目录数据
internal class DirectoryData
{
    [JsonPropertyName("lists")]
    public List<DirectoryItem>? Lists { get; set; }
}

internal class DirectoryItem
{
    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

// 章节内容
internal class ChapterContentItem
{
    public string? Content { get; set; }
    public ChapterNovelData? NovelData { get; set; }
}

internal class ChapterNovelData
{
    public string? ShowNaviCnBody { get; set; }
}
