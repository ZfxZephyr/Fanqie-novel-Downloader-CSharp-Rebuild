using System.Text.Json;
using FanqieNovelDownloader.Models;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 历史记录服务
/// </summary>
public class HistoryService : IDisposable
{
    private readonly string _dataFilePath;
    private List<HistoryItem> _records = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public HistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "FanqieNovelDownloader");
        Directory.CreateDirectory(appDir);
        _dataFilePath = Path.Combine(appDir, "history.json");
    }

    /// <summary>
    /// 历史记录变更事件
    /// </summary>
    public event EventHandler? RecordsChanged;

    /// <summary>
    /// 获取历史记录列表
    /// </summary>
    public IReadOnlyList<HistoryItem> Records => _records.AsReadOnly();

    /// <summary>
    /// 加载历史记录
    /// </summary>
    public async Task LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _records = new List<HistoryItem>();
                return;
            }

            var json = await File.ReadAllTextAsync(_dataFilePath);
            _records = JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? new List<HistoryItem>();
        }
        catch
        {
            _records = new List<HistoryItem>();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 保存历史记录
    /// </summary>
    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_dataFilePath, json);
        RecordsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 添加历史记录
    /// </summary>
    public async Task AddRecordAsync(HistoryItem record)
    {
        await _lock.WaitAsync();
        try
        {
            _records.Insert(0, record);

            // 最多保留1000条记录
            if (_records.Count > 1000)
            {
                _records = _records.Take(1000).ToList();
            }

            await SaveAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 删除历史记录
    /// </summary>
    public async Task<bool> RemoveRecordAsync(string recordId)
    {
        await _lock.WaitAsync();
        try
        {
            var record = _records.FirstOrDefault(r => r.RecordId == recordId);
            if (record == null)
                return false;

            _records.Remove(record);
            await SaveAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 删除选中的历史记录并删除对应文件
    /// </summary>
    public async Task<int> RemoveRecordsWithFilesAsync(IEnumerable<string> recordIds)
    {
        await _lock.WaitAsync();
        try
        {
            var removed = 0;
            foreach (var recordId in recordIds)
            {
                var record = _records.FirstOrDefault(r => r.RecordId == recordId);
                if (record == null)
                    continue;

                // 删除文件
                try
                {
                    if (File.Exists(record.FilePath))
                    {
                        File.Delete(record.FilePath);
                    }
                }
                catch
                {
                    // 文件删除失败，继续处理
                }

                _records.Remove(record);
                removed++;
            }

            if (removed > 0)
                await SaveAsync();

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 清空所有历史记录
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _records.Clear();
            await SaveAsync();
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
