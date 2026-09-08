using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 下载任务页面 - 管理下载和离线缓存任务
/// </summary>
public sealed partial class DownloadsPage : Page
{
    private readonly DownloadService _downloadService;

    public DownloadsPage()
    {
        InitializeComponent();
        _downloadService = App.DownloadService;
        TaskListView.ItemsSource = _downloadService.Tasks;
        UpdateUI();
    }

    /// <summary>
    /// 更新UI状态
    /// </summary>
    private void UpdateUI()
    {
        var hasTasks = _downloadService.Tasks.Count > 0;
        EmptyStatePanel.Visibility = hasTasks ? Visibility.Collapsed : Visibility.Visible;
        TaskListView.Visibility = hasTasks ? Visibility.Visible : Visibility.Collapsed;
        TaskCountText.Text = $"共 {_downloadService.Tasks.Count} 个任务";
    }

    /// <summary>
    /// 全部开始按钮点击
    /// </summary>
    private async void StartAllButton_Click(object sender, RoutedEventArgs e)
    {
        await _downloadService.StartAllWaitingTasksAsync();
        UpdateUI();
    }

    /// <summary>
    /// 全部暂停按钮点击
    /// </summary>
    private void PauseAllButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadService.PauseAllTasks();
    }

    /// <summary>
    /// 清空已完成按钮点击
    /// </summary>
    private void ClearCompletedButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadService.ClearCompletedTasks();
        UpdateUI();
    }

    /// <summary>
    /// 重试失败按钮点击
    /// </summary>
    private async void RetryFailedButton_Click(object sender, RoutedEventArgs e)
    {
        var failedTasks = _downloadService.Tasks
            .Where(t => t.Status == DownloadStatus.Failed)
            .ToList();

        foreach (var task in failedTasks)
        {
            await _downloadService.RetryTaskAsync(task);
        }
    }

    /// <summary>
    /// 暂停/继续按钮点击
    /// </summary>
    private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string taskId)
        {
            var task = _downloadService.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null)
            {
                _downloadService.PauseTask(task);
            }
        }
    }

    /// <summary>
    /// 继续按钮点击
    /// </summary>
    private async void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string taskId)
        {
            var task = _downloadService.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null)
            {
                await _downloadService.ResumeTaskAsync(task);
            }
        }
    }

    /// <summary>
    /// 重试按钮点击
    /// </summary>
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string taskId)
        {
            var task = _downloadService.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null)
            {
                await _downloadService.RetryTaskAsync(task);
            }
        }
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string taskId)
        {
            var task = _downloadService.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null)
            {
                _downloadService.CancelTask(task);
            }
        }
    }

    /// <summary>
    /// 删除按钮点击
    /// </summary>
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string taskId)
        {
            var task = _downloadService.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null)
            {
                _downloadService.RemoveTask(task);
                UpdateUI();
            }
        }
    }
}
