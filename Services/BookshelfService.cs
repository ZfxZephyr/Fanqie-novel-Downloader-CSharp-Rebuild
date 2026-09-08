using System.Text.Json;
using FanqieNovelDownloader.Models;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 书架服务 - 管理本地书架数据
/// </summary>
public class BookshelfService : IDisposable
{
    private readonly string _dataFilePath;
    private List<BookshelfItem> _books = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BookshelfService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "FanqieNovelDownloader");
        Directory.CreateDirectory(appDir);
        _dataFilePath = Path.Combine(appDir, "bookshelf.json");
    }

    /// <summary>
    /// 书架数据变更事件
    /// </summary>
    public event EventHandler? BooksChanged;

    /// <summary>
    /// 获取书架列表
    /// </summary>
    public IReadOnlyList<BookshelfItem> Books => _books.AsReadOnly();

    /// <summary>
    /// 加载书架数据
    /// </summary>
    public async Task LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _books = new List<BookshelfItem>();
                return;
            }

            var json = await File.ReadAllTextAsync(_dataFilePath);
            _books = JsonSerializer.Deserialize<List<BookshelfItem>>(json) ?? new List<BookshelfItem>();
        }
        catch
        {
            _books = new List<BookshelfItem>();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 保存书架数据
    /// </summary>
    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_books, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_dataFilePath, json);
        BooksChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 添加书籍到书架
    /// </summary>
    public async Task<bool> AddBookAsync(BookshelfItem book)
    {
        await _lock.WaitAsync();
        try
        {
            if (_books.Any(b => b.BookId == book.BookId))
                return false;

            _books.Add(book);
            await SaveAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 从书架移除书籍
    /// </summary>
    public async Task<bool> RemoveBookAsync(string bookId)
    {
        await _lock.WaitAsync();
        try
        {
            var book = _books.FirstOrDefault(b => b.BookId == bookId);
            if (book == null)
                return false;

            _books.Remove(book);
            await SaveAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 检查书籍是否在书架中
    /// </summary>
    public async Task<bool> ContainsBookAsync(string bookId)
    {
        await _lock.WaitAsync();
        try
        {
            return _books.Any(b => b.BookId == bookId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 更新书籍信息
    /// </summary>
    public async Task<bool> UpdateBookAsync(BookshelfItem book)
    {
        await _lock.WaitAsync();
        try
        {
            var index = _books.FindIndex(b => b.BookId == book.BookId);
            if (index < 0)
                return false;

            _books[index] = book;
            await SaveAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock?.Dispose();
        GC.SuppressFinalize(this);
    }
}
