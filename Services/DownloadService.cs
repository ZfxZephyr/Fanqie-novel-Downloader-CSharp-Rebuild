using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using FanqieNovelDownloader.Models;

namespace FanqieNovelDownloader.Services;

/// <summary>
/// 下载服务 - 管理下载任务的生命周期
/// </summary>
public class DownloadService : IDisposable
{
    private readonly FanqieApiService _apiService;
    private readonly ObservableCollection<DownloadTask> _tasks;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations;
    private readonly SemaphoreSlim _semaphore;
    private string _downloadDirectory;

    public DownloadService()
    {
        _apiService = new FanqieApiService();
        _tasks = new ObservableCollection<DownloadTask>();
        _cancellations = new ConcurrentDictionary<string, CancellationTokenSource>();
        _semaphore = new SemaphoreSlim(3); // 最多同时下载3个任务
        _downloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "FanqieNovels");
    }

    /// <summary>
    /// 下载任务列表
    /// </summary>
    public ObservableCollection<DownloadTask> Tasks => _tasks;

    /// <summary>
    /// 下载目录
    /// </summary>
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set
        {
            _downloadDirectory = value;
            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }
        }
    }

    /// <summary>
    /// 创建下载任务
    /// </summary>
    /// <param name="bookId">书籍ID</param>
    /// <param name="title">书名</param>
    /// <param name="author">作者</param>
    /// <param name="coverUrl">封面URL</param>
    /// <param name="format">导出格式</param>
    /// <returns>下载任务</returns>
    public DownloadTask CreateTask(string bookId, string title, string author, string coverUrl, ExportFormat format = ExportFormat.Txt)
    {
        var task = new DownloadTask
        {
            BookId = bookId,
            Title = title,
            Author = author,
            CoverUrl = coverUrl,
            Format = format,
            Status = DownloadStatus.Waiting
        };

        _tasks.Add(task);
        return task;
    }

    /// <summary>
    /// 开始下载任务
    /// </summary>
    /// <param name="task">下载任务</param>
    public async Task StartTaskAsync(DownloadTask task)
    {
        if (task.Status == DownloadStatus.Completed || task.Status == DownloadStatus.Downloading)
            return;

        var cts = new CancellationTokenSource();
        _cancellations[task.TaskId] = cts;

        task.Status = DownloadStatus.Downloading;
        task.StartedAt = DateTime.Now;
        task.ErrorMessage = string.Empty;

        try
        {
            await _semaphore.WaitAsync(cts.Token);
            await ExecuteDownloadAsync(task, cts.Token);
        }
        catch (OperationCanceledException)
        {
            if (task.Status == DownloadStatus.Downloading)
                task.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = ex.Message;
        }
        finally
        {
            _cancellations.TryRemove(task.TaskId, out _);
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 执行下载
    /// </summary>
    private async Task ExecuteDownloadAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 获取章节列表
            var chapters = await _apiService.GetChapterListAsync(task.BookId);
            if (chapters.Count == 0)
            {
                throw new Exception("未找到章节");
            }

            task.TotalChapters = chapters.Count;
            task.CompletedChapters = 0;
            task.FailedChapters = 0;

            // 2. 逐章下载内容
            var contents = new List<(string title, string content)>();

            foreach (var chapter in chapters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var content = await _apiService.GetChapterContentAsync(chapter.Id);
                    contents.Add((chapter.Title, content));
                    task.CompletedChapters++;
                }
                catch
                {
                    task.FailedChapters++;
                    // 继续下载其他章节
                }

                // 更新进度
                task.Progress = (int)((double)task.CompletedChapters / task.TotalChapters * 100);

                // 请求间隔，避免过于频繁
                await Task.Delay(400, cancellationToken);
            }

            // 3. 保存文件
            var fileName = SanitizeFileName($"{task.Title} - {task.Author}");
            var extension = task.Format == ExportFormat.Txt ? ".txt" : ".epub";
            var filePath = Path.Combine(_downloadDirectory, fileName + extension);

            // 确保文件名唯一
            var counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(_downloadDirectory, $"{fileName} ({counter}){extension}");
                counter++;
            }

            if (task.Format == ExportFormat.Txt)
            {
                await SaveAsTxtAsync(filePath, task.Title, task.Author, contents);
            }
            else
            {
                await SaveAsEpubAsync(filePath, task.Title, task.Author, contents);
            }

            task.OutputPath = filePath;
            task.Status = DownloadStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.Progress = 100;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存为TXT文件
    /// </summary>
    private async Task SaveAsTxtAsync(string filePath, string title, string author, List<(string title, string content)> contents)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"书名: {title}");
        sb.AppendLine($"作者: {author}");
        sb.AppendLine($"下载时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        foreach (var (chapterTitle, content) in contents)
        {
            sb.AppendLine(chapterTitle);
            sb.AppendLine();
            sb.AppendLine(content);
            sb.AppendLine();
            sb.AppendLine(new string('-', 50));
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 保存为EPUB文件（简化版本）
    /// </summary>
    private async Task SaveAsEpubAsync(string filePath, string title, string author, List<(string title, string content)> contents)
    {
        // TODO: 实现完整的EPUB格式
        // 目前先保存为TXT
        await SaveAsTxtAsync(filePath, title, author, contents);
    }

    /// <summary>
    /// 暂停下载任务
    /// </summary>
    public void PauseTask(DownloadTask task)
    {
        if (task.Status != DownloadStatus.Downloading)
            return;

        if (_cancellations.TryGetValue(task.TaskId, out var cts))
        {
            cts.Cancel();
        }

        task.Status = DownloadStatus.Paused;
    }

    /// <summary>
    /// 继续下载任务
    /// </summary>
    public async Task ResumeTaskAsync(DownloadTask task)
    {
        if (task.Status != DownloadStatus.Paused && task.Status != DownloadStatus.Failed)
            return;

        await StartTaskAsync(task);
    }

    /// <summary>
    /// 取消下载任务
    /// </summary>
    public void CancelTask(DownloadTask task)
    {
        if (task.Status == DownloadStatus.Completed)
            return;

        if (_cancellations.TryGetValue(task.TaskId, out var cts))
        {
            cts.Cancel();
        }

        task.Status = DownloadStatus.Cancelled;
    }

    /// <summary>
    /// 重试下载任务
    /// </summary>
    public async Task RetryTaskAsync(DownloadTask task)
    {
        if (task.Status != DownloadStatus.Failed)
            return;

        task.CompletedChapters = 0;
        task.FailedChapters = 0;
        task.Progress = 0;

        await StartTaskAsync(task);
    }

    /// <summary>
    /// 删除下载任务
    /// </summary>
    public void RemoveTask(DownloadTask task)
    {
        CancelTask(task);
        _tasks.Remove(task);
    }

    /// <summary>
    /// 清空已完成的任务
    /// </summary>
    public void ClearCompletedTasks()
    {
        var completedTasks = _tasks.Where(t => t.Status == DownloadStatus.Completed).ToList();
        foreach (var task in completedTasks)
        {
            _tasks.Remove(task);
        }
    }

    /// <summary>
    /// 开始所有等待中的任务
    /// </summary>
    public async Task StartAllWaitingTasksAsync()
    {
        var waitingTasks = _tasks.Where(t => t.Status == DownloadStatus.Waiting || t.Status == DownloadStatus.Paused).ToList();
        var startTasks = waitingTasks.Select(t => StartTaskAsync(t));
        await Task.WhenAll(startTasks);
    }

    /// <summary>
    /// 暂停所有下载中的任务
    /// </summary>
    public void PauseAllTasks()
    {
        var downloadingTasks = _tasks.Where(t => t.Status == DownloadStatus.Downloading).ToList();
        foreach (var task in downloadingTasks)
        {
            PauseTask(task);
        }
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    public void Dispose()
    {
        foreach (var cts in _cancellations.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _cancellations.Clear();
        _apiService?.Dispose();
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
