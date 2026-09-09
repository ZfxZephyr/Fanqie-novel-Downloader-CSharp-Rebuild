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
    private List<ChapterInfo> _chapters = new();
    private int _currentChapterIndex;
    private string _currentChapterContent = string.Empty;
    private double _currentFontSize = 18;
    private bool _isAscendingOrder = true; // 正序/倒序状态
    private bool _isStealthMode = false; // 隐蔽模式
    private int _stealthLineCount = 3; // 隐蔽模式显示行数
    private int _stealthScrollStep = 1; // 隐蔽模式滚动步长
    private StealthWindow? _stealthWindow; // 隐蔽模式小窗
    private int? _pendingScrollLine; // 退出隐蔽模式后需滚动到的行号
    private bool _suppressThemeEvents; // 初始化 ColorPicker 时抑制事件
    private CancellationTokenSource? _themeSaveCts; // 主题保存防抖

    public ReaderPage()
    {
        InitializeComponent();
        _apiService = new FanqieApiService();
        InitializeThemeUI();
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
            foreach (var line in lines)
            {
                var paragraph = new Paragraph();
                // 首行缩进（两个中文字符宽度）
                paragraph.TextIndent = _currentFontSize * 2;
                paragraph.Inlines.Add(new Run { Text = line.Trim() });
                ContentRichTextBlock.Blocks.Add(paragraph);
            }

            ContentScrollViewer.ScrollToVerticalOffset(0);
            ApplyPendingScrollLine();

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

            // 更新书架的最后阅读记录
            await UpdateBookshelfReadingProgress(chapter);
        }
        catch (Exception ex)
        {
            ChapterLoadingOverlay.Visibility = Visibility.Collapsed;
            PrevChapterButton.IsEnabled = _currentChapterIndex > 0;
            NextChapterButton.IsEnabled = _currentChapterIndex < _chapters.Count - 1;
            _pendingScrollLine = null;

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
    /// 主题预设按钮点击
    /// </summary>
    private void ThemePresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        if (!Enum.TryParse<ReaderThemeMode>(tag, out var mode))
            return;

        App.ReaderThemeService.Mode = mode;
        _ = App.ReaderThemeService.SaveAsync();

        ApplyCurrentTheme();
        UpdateThemeSelectionVisuals();
        UpdateCustomSwatch();
    }

    /// <summary>
    /// 自定义背景色改变
    /// </summary>
    private void BgColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_suppressThemeEvents) return;

        App.ReaderThemeService.CustomBackgroundHex = ReaderThemeService.ToHex(args.NewColor);
        UpdateCustomSwatch();
        if (App.ReaderThemeService.Mode == ReaderThemeMode.Custom)
        {
            ApplyCurrentTheme();
        }
        QueueThemeSave();
    }

    /// <summary>
    /// 自定义文字色改变
    /// </summary>
    private void FgColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_suppressThemeEvents) return;

        App.ReaderThemeService.CustomForegroundHex = ReaderThemeService.ToHex(args.NewColor);
        UpdateCustomSwatch();
        if (App.ReaderThemeService.Mode == ReaderThemeMode.Custom)
        {
            ApplyCurrentTheme();
        }
        QueueThemeSave();
    }

    /// <summary>
    /// 初始化主题设置 UI（选中状态、色板、自定义颜色）
    /// </summary>
    private void InitializeThemeUI()
    {
        _suppressThemeEvents = true;
        BgColorPicker.Color = ReaderThemeService.ParseHex(App.ReaderThemeService.CustomBackgroundHex);
        FgColorPicker.Color = ReaderThemeService.ParseHex(App.ReaderThemeService.CustomForegroundHex);
        _suppressThemeEvents = false;

        UpdateThemeSelectionVisuals();
        UpdateCustomSwatch();
        ApplyCurrentTheme();
    }

    /// <summary>
    /// 应用当前主题到阅读内容区
    /// </summary>
    private void ApplyCurrentTheme()
    {
        var colors = App.ReaderThemeService.GetColors();
        if (colors is null)
        {
            // 跟随系统：还原主题资源
            ContentGrid.Background = null;
            ContentRichTextBlock.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        }
        else
        {
            ContentGrid.Background = new SolidColorBrush(colors.Value.Background);
            ContentRichTextBlock.Foreground = new SolidColorBrush(colors.Value.Foreground);
        }
    }

    /// <summary>
    /// 更新主题预设按钮的选中高亮与自定义面板可见性
    /// </summary>
    private void UpdateThemeSelectionVisuals()
    {
        var buttons = new[]
        {
            ThemeSystemButton, ThemeLightButton, ThemeDarkButton,
            ThemeSepiaButton, ThemeGreenButton, ThemeCustomButton,
        };
        var current = App.ReaderThemeService.Mode.ToString();

        foreach (var button in buttons)
        {
            var selected = button.Tag as string == current;
            button.BorderBrush = selected
                ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
            button.BorderThickness = new Thickness(selected ? 2 : 1);
        }

        CustomThemePanel.Visibility = App.ReaderThemeService.Mode == ReaderThemeMode.Custom
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// 更新自定义预设按钮的色板预览
    /// </summary>
    private void UpdateCustomSwatch()
    {
        var bg = ReaderThemeService.ParseHex(App.ReaderThemeService.CustomBackgroundHex);
        var fg = ReaderThemeService.ParseHex(App.ReaderThemeService.CustomForegroundHex);
        ThemeCustomSwatchBorder.Background = new SolidColorBrush(bg);
        ThemeCustomSwatchText.Foreground = new SolidColorBrush(fg);
    }

    /// <summary>
    /// 防抖保存主题设置（拖动色板时避免频繁写盘）
    /// </summary>
    private async void QueueThemeSave()
    {
        _themeSaveCts?.Cancel();
        _themeSaveCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(500, _themeSaveCts.Token);
            await App.ReaderThemeService.SaveAsync();
        }
        catch (TaskCanceledException)
        {
            // 被新的更改取代，忽略
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
    /// 隐蔽模式开关切换：隐藏主窗口并打开伪装小窗
    /// </summary>
    private void StealthModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (StealthModeToggle == null) return;

        // 程序化设置开关时（退出流程）不重复触发
        if (_isStealthMode == StealthModeToggle.IsOn) return;

        _isStealthMode = StealthModeToggle.IsOn;
        StealthSettingsPanel.Visibility = _isStealthMode ? Visibility.Visible : Visibility.Collapsed;

        if (_isStealthMode)
        {
            EnterStealthMode();
        }
        else
        {
            CloseStealthWindow();
        }
    }

    /// <summary>
    /// 进入隐蔽模式：创建小窗并隐藏主窗口
    /// </summary>
    private void EnterStealthMode()
    {
        if (_chapters.Count == 0 || string.IsNullOrEmpty(_currentChapterContent))
        {
            _isStealthMode = false;
            StealthModeToggle.IsOn = false;
            return;
        }

        try
        {
            // 关闭可能打开的面板
            ChapterListPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;

            _stealthWindow = new StealthWindow(
                _apiService,
                _bookId,
                _chapters,
                _currentChapterIndex,
                _currentChapterContent,
                _stealthLineCount,
                _stealthScrollStep);
            _stealthWindow.Closed += StealthWindow_Closed;

            // 隐藏主窗口并激活小窗
            App.MainWindow?.AppWindow.Hide();
            _stealthWindow.Activate();
        }
        catch (Exception ex)
        {
            _isStealthMode = false;
            if (StealthModeToggle != null)
            {
                StealthModeToggle.IsOn = false;
            }
            App.MainWindow?.AppWindow.Show();
            _ = ShowErrorAsync("进入隐蔽模式失败", ex.Message);
        }
    }

    /// <summary>
    /// 小窗关闭：恢复主窗口并同步阅读进度
    /// </summary>
    private void StealthWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_stealthWindow == null) return;

        var chapterIndex = _stealthWindow.ResultChapterIndex;
        var lineOffset = _stealthWindow.ResultLineOffset;
        _stealthWindow = null;

        // 恢复状态与主窗口
        _isStealthMode = false;
        if (StealthModeToggle != null)
        {
            StealthModeToggle.IsOn = false;
        }

        App.MainWindow?.AppWindow.Show();
        App.MainWindow?.Activate();

        // 同步进度：跳转到退出时的章节并滚动到行位置
        _pendingScrollLine = lineOffset;
        if (chapterIndex != _currentChapterIndex)
        {
            _ = LoadChapterContentAsync(chapterIndex, isChapterSwitch: true);
        }
        else
        {
            ApplyPendingScrollLine();
        }
    }

    /// <summary>
    /// 关闭隐蔽模式小窗（兜底路径，正常退出由小窗 Closed 事件处理）
    /// </summary>
    private void CloseStealthWindow()
    {
        if (_stealthWindow != null)
        {
            _stealthWindow.Closed -= StealthWindow_Closed;
            _stealthWindow.Close();
            _stealthWindow = null;

            App.MainWindow?.AppWindow.Show();
            App.MainWindow?.Activate();
        }
    }

    /// <summary>
    /// 滚动主阅读器到退出隐蔽模式时的行位置（按行高近似定位）
    /// </summary>
    private void ApplyPendingScrollLine()
    {
        if (_pendingScrollLine is int line && line > 0 && ContentScrollViewer != null)
        {
            var offset = Math.Max(0, line * ContentRichTextBlock.LineHeight - 48);
            ContentScrollViewer.ScrollToVerticalOffset(offset);
        }
        _pendingScrollLine = null;
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// 隐蔽模式显示行数改变
    /// </summary>
    private void StealthLineSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (StealthLineSlider != null)
        {
            _stealthLineCount = (int)e.NewValue;
            if (StealthLineText != null)
                StealthLineText.Text = _stealthLineCount.ToString();
        }
    }

    /// <summary>
    /// 隐蔽模式滚动步长改变
    /// </summary>
    private void StealthStepSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (StealthStepSlider != null)
        {
            _stealthScrollStep = (int)e.NewValue;
            if (StealthStepText != null)
                StealthStepText.Text = _stealthScrollStep.ToString();
        }
    }
}
