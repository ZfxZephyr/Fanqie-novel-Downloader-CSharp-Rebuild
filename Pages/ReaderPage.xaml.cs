using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace FanqieNovelDownloader.Pages;

/// <summary>
/// 阅读器页面参数
/// </summary>
public class ReaderParams
{
    public string BookId { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string? StartChapterId { get; set; }
}

/// <summary>
/// 阅读器页面
/// </summary>
public sealed partial class ReaderPage : Page
{
    private readonly FanqieApiService _apiService;
    private string _bookId = string.Empty;
    private string _bookTitle = string.Empty;
    private List<ChapterInfo> _chapters = new();
    private int _currentChapterIndex;
    private string _currentChapterContent = string.Empty;
    private double _currentFontSize = 18;
    private bool _isAscendingOrder = true; // 正序/倒序状态
    private bool _isStealthMode = false; // 隐蔽模式
    private int _stealthLineCount = 3; // 隐蔽模式显示行数

    public ReaderPage()
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

        if (e.Parameter is ReaderParams p)
        {
            _bookId = p.BookId;
            _bookTitle = p.BookTitle;

            await LoadChaptersAsync(p.StartChapterId);
        }
    }

    /// <summary>
    /// 加载章节目录
    /// </summary>
    private async Task LoadChaptersAsync(string? startChapterId = null)
    {
        try
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ContentGrid.Visibility = Visibility.Collapsed;

            _chapters = await _apiService.GetChapterListAsync(_bookId);
            UpdateChapterListDisplay();

            if (_chapters.Count == 0)
            {
                LoadingRing.IsActive = false;
                return;
            }

            // 确定起始章节
            if (!string.IsNullOrEmpty(startChapterId))
            {
                _currentChapterIndex = _chapters.FindIndex(c => c.Id == startChapterId);
                if (_currentChapterIndex < 0) _currentChapterIndex = 0;
            }

            await LoadChapterContentAsync(_currentChapterIndex);
        }
        catch (Exception ex)
        {
            LoadingRing.IsActive = false;
            var dialog = new ContentDialog
            {
                Title = "加载失败",
                Content = ex.Message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// 加载章节内容
    /// </summary>
    /// <param name="chapterIndex">章节索引</param>
    /// <param name="isChapterSwitch">是否为章节切换（非首次加载）</param>
    private async Task LoadChapterContentAsync(int chapterIndex, bool isChapterSwitch = false)
    {
        if (chapterIndex < 0 || chapterIndex >= _chapters.Count)
            return;

        try
        {
            if (isChapterSwitch)
            {
                // 章节切换时显示加载遮罩并禁用按钮
                ChapterLoadingOverlay.Visibility = Visibility.Visible;
                PrevChapterButton.IsEnabled = false;
                NextChapterButton.IsEnabled = false;
            }
            else
            {
                LoadingRing.IsActive = true;
            }

            var chapter = _chapters[chapterIndex];
            _currentChapterContent = await _apiService.GetChapterContentAsync(chapter.Id);
            _currentChapterIndex = chapterIndex;

            // 更新UI
            ChapterTitleText.Text = chapter.Title;
            ChapterProgressText.Text = $"{chapterIndex + 1} / {_chapters.Count}";
            PrevChapterButton.IsEnabled = chapterIndex > 0;
            NextChapterButton.IsEnabled = chapterIndex < _chapters.Count - 1;

            // 显示内容
            ContentRichTextBlock.Blocks.Clear();
            var lines = _currentChapterContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var displayLines = _isStealthMode
                ? lines.Take(_stealthLineCount).ToArray()
                : lines;

            foreach (var line in displayLines)
            {
                var paragraph = new Paragraph();
                // 首行缩进（两个中文字符宽度）
                paragraph.TextIndent = _currentFontSize * 2;
                paragraph.Inlines.Add(new Run { Text = line.Trim() });
                ContentRichTextBlock.Blocks.Add(paragraph);
            }

            ContentScrollViewer.ScrollToVerticalOffset(0);

            if (isChapterSwitch)
            {
                ChapterLoadingOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
                ContentGrid.Visibility = Visibility.Visible;
            }

            // 更新隐蔽模式按钮状态
            if (_isStealthMode)
            {
                StealthSmallPrevButton.IsEnabled = chapterIndex > 0;
                StealthSmallNextButton.IsEnabled = chapterIndex < _chapters.Count - 1;
                StealthTitleText.Text = chapter.Title;
                StealthProgressText.Text = $"{chapterIndex + 1} / {_chapters.Count}";
            }

            // 更新书架的最后阅读记录
            await UpdateBookshelfReadingProgress(chapter);
        }
        catch (Exception ex)
        {
            ChapterLoadingOverlay.Visibility = Visibility.Collapsed;
            PrevChapterButton.IsEnabled = _currentChapterIndex > 0;
            NextChapterButton.IsEnabled = _currentChapterIndex < _chapters.Count - 1;

            if (!isChapterSwitch)
                LoadingRing.IsActive = false;

            var dialog = new ContentDialog
            {
                Title = "加载章节失败",
                Content = ex.Message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// 更新书架阅读进度
    /// </summary>
    private async Task UpdateBookshelfReadingProgress(ChapterInfo chapter)
    {
        var bookshelf = App.BookshelfService;
        var book = bookshelf.Books.FirstOrDefault(b => b.BookId == _bookId);
        if (book != null)
        {
            book.LastReadChapterId = chapter.Id;
            book.LastReadChapterTitle = chapter.Title;
            book.LastReadTime = DateTime.Now;
            await bookshelf.UpdateBookAsync(book);
        }
    }

    /// <summary>
    /// 返回按钮
    /// </summary>
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    /// <summary>
    /// 上一章
    /// </summary>
    private async void PrevChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChapterIndex > 0)
        {
            await LoadChapterContentAsync(_currentChapterIndex - 1, isChapterSwitch: true);
        }
    }

    /// <summary>
    /// 下一章
    /// </summary>
    private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChapterIndex < _chapters.Count - 1)
        {
            await LoadChapterContentAsync(_currentChapterIndex + 1, isChapterSwitch: true);
        }
    }

    /// <summary>
    /// 打开章节列表
    /// </summary>
    private void ChapterListButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        ChapterListPanel.Visibility = ChapterListPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// 关闭章节列表
    /// </summary>
    private void CloseChapterListButton_Click(object sender, RoutedEventArgs e)
    {
        ChapterListPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 章节列表点击
    /// </summary>
    private async void ChapterList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string title)
        {
            var index = _chapters.FindIndex(c => c.Title == title);
            if (index >= 0)
            {
                ChapterListPanel.Visibility = Visibility.Collapsed;
                await LoadChapterContentAsync(index);
            }
        }
    }

    /// <summary>
    /// 打开设置面板
    /// </summary>
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ChapterListPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// 关闭设置面板
    /// </summary>
    private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 字体大小改变
    /// </summary>
    private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ContentRichTextBlock != null)
        {
            _currentFontSize = e.NewValue;
            ContentRichTextBlock.FontSize = e.NewValue;
            if (FontSizeText != null)
                FontSizeText.Text = ((int)e.NewValue).ToString();
        }
    }

    /// <summary>
    /// 行间距改变
    /// </summary>
    private void LineHeightSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ContentRichTextBlock != null)
        {
            ContentRichTextBlock.LineHeight = e.NewValue;
            if (LineHeightText != null)
                LineHeightText.Text = ((int)e.NewValue).ToString();
        }
    }

    /// <summary>
    /// 字体选择改变
    /// </summary>
    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox == null || ContentRichTextBlock == null) return;

        var selectedFont = FontComboBox.SelectedIndex switch
        {
            1 => "Microsoft YaHei",
            2 => "SimSun",
            3 => "KaiTi",
            _ => "Segoe UI"
        };
        ContentRichTextBlock.FontFamily = new FontFamily(selectedFont);
    }

    /// <summary>
    /// 主题切换
    /// </summary>
    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string theme)
        {
            ApplyTheme(theme);
        }
    }

    /// <summary>
    /// 应用主题
    /// </summary>
    private void ApplyTheme(string theme)
    {
        var resources = Application.Current.Resources;

        switch (theme)
        {
            case "Light":
                ContentGrid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                ContentRichTextBlock.Foreground = (Brush)resources["TextFillColorPrimaryBrush"];
                break;
            case "Dark":
                ContentGrid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 30));
                ContentRichTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 220, 220));
                break;
            case "Sepia":
                ContentGrid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 240, 232));
                ContentRichTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 80, 60, 40));
                break;
        }
    }

    /// <summary>
    /// 更新章节列表显示
    /// </summary>
    private void UpdateChapterListDisplay()
    {
        var displayList = _isAscendingOrder
            ? _chapters.Select(c => c.Title).ToList()
            : _chapters.Select(c => c.Title).Reverse().ToList();
        ChapterList.ItemsSource = displayList;
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
    /// 隐蔽模式开关切换
    /// </summary>
    private void StealthModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (StealthModeToggle == null) return;

        _isStealthMode = StealthModeToggle.IsOn;
        StealthSettingsPanel.Visibility = _isStealthMode ? Visibility.Visible : Visibility.Collapsed;

        // 隐蔽模式小窗
        StealthOverlay.Visibility = _isStealthMode ? Visibility.Visible : Visibility.Collapsed;
        ContentGrid.Visibility = _isStealthMode ? Visibility.Collapsed : Visibility.Visible;

        // 隐藏章节列表和设置面板
        if (_isStealthMode)
        {
            ChapterListPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        // 更新按钮状态
        if (_isStealthMode)
        {
            StealthSmallPrevButton.IsEnabled = _currentChapterIndex > 0;
            StealthSmallNextButton.IsEnabled = _currentChapterIndex < _chapters.Count - 1;
            StealthTitleText.Text = _bookTitle;
            StealthProgressText.Text = $"{_currentChapterIndex + 1} / {_chapters.Count}";
        }

        // 重新显示当前章节内容
        if (!string.IsNullOrEmpty(_currentChapterContent))
        {
            RefreshContentDisplay();
        }
    }

    /// <summary>
    /// 关闭隐蔽模式
    /// </summary>
    private void StealthCloseButton_Click(object sender, RoutedEventArgs e)
    {
        StealthModeToggle.IsOn = false;
    }

    /// <summary>
    /// 隐蔽模式行数改变
    /// </summary>
    private void StealthLineSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (StealthLineSlider != null)
        {
            _stealthLineCount = (int)e.NewValue;
            if (StealthLineText != null)
                StealthLineText.Text = _stealthLineCount.ToString();

            // 重新显示内容
            if (_isStealthMode && !string.IsNullOrEmpty(_currentChapterContent))
            {
                RefreshContentDisplay();
            }
        }
    }

    /// <summary>
    /// 刷新内容显示
    /// </summary>
    private void RefreshContentDisplay()
    {
        var lines = _currentChapterContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var displayLines = _isStealthMode
            ? lines.Take(_stealthLineCount).ToArray()
            : lines;

        // 更新主阅读器
        ContentRichTextBlock.Blocks.Clear();
        foreach (var line in displayLines)
        {
            var paragraph = new Paragraph();
            paragraph.TextIndent = _currentFontSize * 2;
            paragraph.Inlines.Add(new Run { Text = line.Trim() });
            ContentRichTextBlock.Blocks.Add(paragraph);
        }

        // 更新隐蔽模式小窗
        if (_isStealthMode)
        {
            StealthContentBlock.Blocks.Clear();
            foreach (var line in displayLines)
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = line.Trim() });
                StealthContentBlock.Blocks.Add(paragraph);
            }
        }
    }
}
