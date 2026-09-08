using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 搜索页面 - 用于搜索番茄小说
/// </summary>
public sealed partial class SearchPage : Page
{
    private readonly FanqieApiService _apiService;
    private CancellationTokenSource? _searchCts;

    public SearchPage()
    {
        InitializeComponent();
        _apiService = new FanqieApiService();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // TODO: 实现搜索建议功能（可选）
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText;
        if (string.IsNullOrWhiteSpace(query))
            return;

        await PerformSearchAsync(query);
    }

    private void SearchResultsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BookInfo book)
        {
            // 导航到书籍详情页
            Frame.Navigate(typeof(BookDetailPage), book.Id);
        }
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    private async Task PerformSearchAsync(string query)
    {
        // 取消之前的搜索请求
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            // 显示加载状态
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            SearchResultsGrid.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Collapsed;

            // 解析输入（可能是关键词、链接或ID）
            var searchQuery = query.Trim();

            // 调用API搜索
            var results = await _apiService.SearchAsync(searchQuery);

            // 更新UI
            if (results.Count > 0)
            {
                SearchResultsGrid.ItemsSource = results;
                SearchResultsGrid.Visibility = Visibility.Visible;
                EmptyStateText.Visibility = Visibility.Collapsed;
                ResultCountText.Text = $"找到 {results.Count} 本小说";
            }
            else
            {
                SearchResultsGrid.Visibility = Visibility.Collapsed;
                EmptyStateText.Visibility = Visibility.Visible;
                EmptyStateText.Text = $"未找到与 \"{query}\" 相关的小说";
                ResultCountText.Text = "搜索结果";
            }
        }
        catch (OperationCanceledException)
        {
            // 搜索被取消，忽略
        }
        catch (Exception ex)
        {
            // 显示错误信息
            SearchResultsGrid.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Visible;
            ErrorText.Text = $"搜索失败: {ex.Message}";
            ResultCountText.Text = "搜索结果";
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

}
