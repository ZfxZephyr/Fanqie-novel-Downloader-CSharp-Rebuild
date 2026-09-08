using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 历史记录页面 - 查看下载历史和已导出文件
/// </summary>
public sealed partial class HistoryPage : Page
{
    private readonly HistoryService _historyService;

    public HistoryPage()
    {
        InitializeComponent();
        _historyService = App.HistoryService;
        HistoryListView.ItemsSource = _historyService.Records;
        _historyService.RecordsChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateUI);
        UpdateUI();
    }

    /// <summary>
    /// 更新UI状态
    /// </summary>
    private void UpdateUI()
    {
        var hasRecords = _historyService.Records.Count > 0;
        EmptyStatePanel.Visibility = hasRecords ? Visibility.Collapsed : Visibility.Visible;
        HistoryListView.Visibility = hasRecords ? Visibility.Visible : Visibility.Collapsed;
        RecordCountText.Text = $"共 {_historyService.Records.Count} 条记录";
    }

    /// <summary>
    /// 记录点击 - 打开文件
    /// </summary>
    private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryItem record && record.FileExists)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(record.FilePath));
        }
    }

    /// <summary>
    /// 打开文件位置
    /// </summary>
    private async void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string recordId)
        {
            var record = _historyService.Records.FirstOrDefault(r => r.RecordId == recordId);
            if (record != null && record.FileExists)
            {
                var folder = Path.GetDirectoryName(record.FilePath);
                if (folder != null)
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(folder));
                }
            }
        }
    }

    /// <summary>
    /// 删除记录按钮点击
    /// </summary>
    private async void RemoveRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string recordId)
        {
            var record = _historyService.Records.FirstOrDefault(r => r.RecordId == recordId);
            if (record == null) return;

            var dialog = new ContentDialog
            {
                Title = "删除记录",
                Content = $"确定要删除《{record.Title}》的下载记录？",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _historyService.RemoveRecordAsync(recordId);
            }
        }
    }

    /// <summary>
    /// 删除选中文档按钮点击
    /// </summary>
    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = HistoryListView.SelectedItems
            .OfType<HistoryItem>()
            .Select(r => r.RecordId)
            .ToList();

        if (selectedIds.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "删除选中文档",
            Content = $"确定要删除选中的 {selectedIds.Count} 条记录及其对应文件？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var removed = await _historyService.RemoveRecordsWithFilesAsync(selectedIds);
        }
    }

    /// <summary>
    /// 清空记录按钮点击
    /// </summary>
    private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "清空记录",
            Content = "确定要清空所有下载记录？此操作不会删除已下载的文件。",
            PrimaryButtonText = "清空",
            CloseButtonText = "取消",
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _historyService.ClearAllAsync();
        }
    }
}
