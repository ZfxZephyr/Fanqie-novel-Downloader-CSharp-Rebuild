using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 书架页面 - 管理本地和官方书架
/// </summary>
public sealed partial class BookshelfPage : Page
{
    private readonly BookshelfService _bookshelfService;

    public BookshelfPage()
    {
        InitializeComponent();
        _bookshelfService = App.BookshelfService;
        BookListView.ItemsSource = _bookshelfService.Books;
        _bookshelfService.BooksChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateUI);
        UpdateUI();
    }

    /// <summary>
    /// 更新UI状态
    /// </summary>
    private void UpdateUI()
    {
        var hasBooks = _bookshelfService.Books.Count > 0;
        EmptyStatePanel.Visibility = hasBooks ? Visibility.Collapsed : Visibility.Visible;
        BookListView.Visibility = hasBooks ? Visibility.Visible : Visibility.Collapsed;
        BookCountText.Text = $"共 {_bookshelfService.Books.Count} 本";
    }

    /// <summary>
    /// 书籍点击 - 导航到详情页
    /// </summary>
    private void BookListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BookshelfItem book)
        {
            Frame.Navigate(typeof(BookDetailPage), book.BookId);
        }
    }

    /// <summary>
    /// 继续阅读按钮点击
    /// </summary>
    private void ContinueReadingButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string bookId)
        {
            // TODO: 导航到阅读页面
            // Frame.Navigate(typeof(ReaderPage), bookId);
        }
    }

    /// <summary>
    /// 删除按钮点击
    /// </summary>
    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string bookId)
        {
            var book = _bookshelfService.Books.FirstOrDefault(b => b.BookId == bookId);
            if (book == null) return;

            var dialog = new ContentDialog
            {
                Title = "从书架移除",
                Content = $"确定要将《{book.Title}》从书架移除？",
                PrimaryButtonText = "移除",
                CloseButtonText = "取消",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _bookshelfService.RemoveBookAsync(bookId);
            }
        }
    }
}
