using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 书籍详情页
/// </summary>
public sealed partial class BookDetailPage : Page
{
    private readonly FanqieApiService _apiService;
    private string _bookId = string.Empty;
    private BookDetailData? _bookDetail;
    private List<ChapterInfo> _chapters = new();
    private bool _isAscendingOrder = true; // 正序/倒序状态

    public BookDetailPage()
    {
        InitializeComponent();
        _apiService = new FanqieApiService();
    }

    /// <summary>
    /// 导航到此页面时触发
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string bookId)
        {
            _bookId = bookId;
            await LoadBookDetailAsync();
        }
    }

    /// <summary>
    /// 加载书籍详情
    /// </summary>
    private async Task LoadBookDetailAsync()
    {
        try
        {
            // 显示加载状态
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ContentScrollViewer.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            // 并行获取书籍详情和章节列表
            var detailTask = _apiService.GetBookDetailAsync(_bookId);
            var chaptersTask = _apiService.GetChapterListAsync(_bookId);

            await Task.WhenAll(detailTask, chaptersTask);

            _bookDetail = detailTask.Result;
            _chapters = chaptersTask.Result;

            if (_bookDetail == null)
            {
                ShowError();
                return;
            }

            // 更新UI
            UpdateUI();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (_bookDetail == null) return;

        // 基本信息
        TitleText.Text = _bookDetail.Title;
        AuthorText.Text = _bookDetail.Author;

        // 封面图片
        if (!string.IsNullOrWhiteSpace(_bookDetail.CoverUrl))
        {
            CoverImage.Source = new BitmapImage(new Uri(_bookDetail.CoverUrl));
        }
        WordCountText.Text = _bookDetail.WordCount switch
        {
            >= 10000 => $"{_bookDetail.WordCount / 10000.0:F1}万字",
            _ => $"{_bookDetail.WordCount}字"
        };
        ChapterCountText.Text = $"{_bookDetail.ChapterCount}章";
        ReadCountText.Text = _bookDetail.ReadCount switch
        {
            >= 10000 => $"{_bookDetail.ReadCount / 10000.0:F1}万人在读",
            _ => $"{_bookDetail.ReadCount}人在读"
        };

        // 标签
        if (_bookDetail.Tags != null && _bookDetail.Tags.Count > 0)
        {
            TagsList.ItemsSource = _bookDetail.Tags;
        }

        // 简介
        DescriptionText.Text = _bookDetail.Description;

        // 章节列表
        UpdateChapterListDisplay();
        ChapterTotalText.Text = $"共 {_chapters.Count} 章";

        // 显示内容
        LoadingRing.Visibility = Visibility.Collapsed;
        ContentScrollViewer.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 更新章节列表显示
    /// </summary>
    private void UpdateChapterListDisplay()
    {
        var displayList = _isAscendingOrder
            ? _chapters.ToList()
            : _chapters.AsEnumerable().Reverse().ToList();
        ChapterListView.ItemsSource = displayList;
    }

    /// <summary>
    /// 切换排序按钮点击
    /// </summary>
    private void SortButton_Click(object sender, RoutedEventArgs e)
    {
        _isAscendingOrder = !_isAscendingOrder;
        UpdateChapterListDisplay();

        // 更新按钮图标
        if (SortButton != null)
        {
            var icon = SortButton.Content as FontIcon;
            if (icon != null)
            {
                icon.Glyph = _isAscendingOrder ? "&#xE8CB;" : "&#xE8CC;";
            }
        }
    }

    /// <summary>
    /// 显示错误状态
    /// </summary>
    private void ShowError(string? message = null)
    {
        LoadingRing.Visibility = Visibility.Collapsed;
        ContentScrollViewer.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 重试按钮点击
    /// </summary>
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadBookDetailAsync();
    }

    /// <summary>
    /// 加入书架按钮点击
    /// </summary>
    private async void AddToBookshelfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_bookDetail == null) return;

        var bookshelf = App.BookshelfService;

        // 检查是否已在书架
        if (await bookshelf.ContainsBookAsync(_bookId))
        {
            var existsDialog = new ContentDialog
            {
                Title = "提示",
                Content = $"《{_bookDetail.Title}》已在书架中",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await existsDialog.ShowAsync();
            return;
        }

        var item = new BookshelfItem
        {
            BookId = _bookId,
            Title = _bookDetail.Title,
            Author = _bookDetail.Author,
            CoverUrl = _bookDetail.CoverUrl,
            Description = _bookDetail.Description,
            TotalChapters = _chapters.Count,
            AddedTime = DateTime.Now
        };

        await bookshelf.AddBookAsync(item);

        var dialog = new ContentDialog
        {
            Title = "加入书架",
            Content = $"已将《{_bookDetail.Title}》加入书架",
            CloseButtonText = "确定",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// 开始阅读按钮点击
    /// </summary>
    private async void StartReadingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chapters.Count == 0 || _bookDetail == null) return;

        // 从书架获取上次阅读位置
        var bookshelf = App.BookshelfService;
        var book = bookshelf.Books.FirstOrDefault(b => b.BookId == _bookId);
        var startChapterId = book?.LastReadChapterId ?? _chapters[0].Id;

        Frame.Navigate(typeof(ReaderPage), new ReaderParams
        {
            BookId = _bookId,
            BookTitle = _bookDetail.Title,
            StartChapterId = startChapterId
        });
    }

    /// <summary>
    /// 下载全本按钮点击
    /// </summary>
    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_bookDetail == null) return;

        // 选择导出格式
        var formatDialog = new ContentDialog
        {
            Title = "选择导出格式",
            Content = "请选择下载格式：",
            PrimaryButtonText = "TXT",
            SecondaryButtonText = "EPUB",
            CloseButtonText = "取消",
            XamlRoot = Content.XamlRoot
        };

        var formatResult = await formatDialog.ShowAsync();
        if (formatResult == ContentDialogResult.None)
            return;

        var format = formatResult == ContentDialogResult.Primary
            ? ExportFormat.Txt
            : ExportFormat.Epub;

        // 确认下载
        var confirmDialog = new ContentDialog
        {
            Title = "下载全本",
            Content = $"开始下载《{_bookDetail.Title}》？\n格式: {format}\n共 {_chapters.Count} 章",
            PrimaryButtonText = "开始下载",
            CloseButtonText = "取消",
            XamlRoot = Content.XamlRoot
        };

        var confirmResult = await confirmDialog.ShowAsync();
        if (confirmResult == ContentDialogResult.Primary)
        {
            // 创建下载任务并开始下载
            var downloadService = App.DownloadService;
            var task = downloadService.CreateTask(
                _bookId,
                _bookDetail.Title,
                _bookDetail.Author,
                _bookDetail.CoverUrl,
                format);

            // 开始下载
            await downloadService.StartTaskAsync(task);

            // 提示用户
            var successDialog = new ContentDialog
            {
                Title = "下载已开始",
                Content = $"《{_bookDetail.Title}》已添加到下载队列。\n可在下载页面查看进度。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await successDialog.ShowAsync();
        }
    }

    /// <summary>
    /// 章节列表点击
    /// </summary>
    private void ChapterListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ChapterInfo chapter && _bookDetail != null)
        {
            Frame.Navigate(typeof(ReaderPage), new ReaderParams
            {
                BookId = _bookId,
                BookTitle = _bookDetail.Title,
                StartChapterId = chapter.Id
            });
        }
    }
}
