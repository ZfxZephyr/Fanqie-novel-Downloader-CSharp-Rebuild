using System.Runtime.InteropServices;
using FanqieNovelDownloader.Models;
using FanqieNovelDownloader.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FanqieNovelDownloader;

/// <summary>
/// 隐蔽模式小窗：伪装成记事本的迷你阅读器。
/// 隐藏主窗口后显示，支持按行滚动与自动翻章，关闭时回传阅读进度。
/// </summary>
public sealed partial class StealthWindow : Window
{
    private readonly FanqieApiService _apiService;
    private readonly string _bookId;
    private readonly List<ChapterInfo> _chapters;

    private string[] _lines = Array.Empty<string>();
    private int _chapterIndex;
    private int _lineOffset;
    private readonly int _displayLineCount; // 显示行数 N
    private readonly int _scrollStep; // 滚动步长 n
    private bool _isLoading;
    private bool _closed;
    private Brush _textBrush = null!; // 内容文字画刷（随主题变化，构造时初始化）

    /// <summary>退出隐蔽模式时所在的章节索引</summary>
    public int ResultChapterIndex => _chapterIndex;

    /// <summary>退出隐蔽模式时的行偏移（可见首行索引）</summary>
    public int ResultLineOffset => _lineOffset;

    public StealthWindow(
        FanqieApiService apiService,
        string bookId,
        List<ChapterInfo> chapters,
        int chapterIndex,
        string chapterContent,
        int displayLineCount,
        int scrollStep)
    {
        InitializeComponent();

        _apiService = apiService;
        _bookId = bookId;
        _chapters = chapters;
        _chapterIndex = chapterIndex;
        _displayLineCount = Math.Max(1, displayLineCount);
        _scrollStep = Math.Max(1, scrollStep);

        // 窗口伪装：标题 + 记事本图标
        Title = "记事本";
        AppWindow.Title = "记事本";
        var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NotepadIcon.ico");
        if (File.Exists(icoPath))
        {
            AppWindow.SetIcon(icoPath);
        }

        InitWindowSizeAndPosition();

        Closed += (_, _) => _closed = true;

        ApplyThemeFromService();

        _lines = SplitLines(chapterContent);
        _lineOffset = 0;
        RefreshDisplay();
    }

    /// <summary>
    /// 应用阅读主题：自定义/预设主题用纯色背景+文字色，跟随系统保留亚克力半透明
    /// </summary>
    private void ApplyThemeFromService()
    {
        var colors = App.ReaderThemeService.GetColors();
        if (colors is null)
        {
            // 跟随系统：保留亚克力背景，使用系统主题资源
            _textBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            return;
        }

        SystemBackdrop = null;
        var bgBrush = new SolidColorBrush(colors.Value.Background);
        var fgBrush = new SolidColorBrush(colors.Value.Foreground);
        RootGrid.Background = bgBrush;
        BottomBar.Background = bgBrush;
        ProgressText.Foreground = fgBrush;
        LoadingText.Foreground = fgBrush;
        _textBrush = fgBrush;
    }

    /// <summary>
    /// 设置初始尺寸（按 DPI 缩放）并定位到屏幕右下角
    /// </summary>
    private void InitWindowSizeAndPosition()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        double scale = GetDpiForWindow(hwnd) / 96.0;
        if (scale <= 0)
        {
            scale = 1.0;
        }

        int width = (int)Math.Round(480 * scale);
        int height = (int)Math.Round(200 * scale);
        AppWindow.Resize(new SizeInt32(width, height));

        var workArea = DisplayArea.Primary.WorkArea;
        AppWindow.Move(new PointInt32(
            workArea.X + workArea.Width - width - 20,
            workArea.Y + workArea.Height - height - 20));
    }

    /// <summary>当前章节允许的最大行偏移（保证可见窗口不越过末尾）</summary>
    private int MaxOffset => Math.Max(0, _lines.Length - _displayLineCount);

    private static string[] SplitLines(string content)
    {
        return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// 刷新内容与进度显示
    /// </summary>
    private void RefreshDisplay()
    {
        LinesHost.Children.Clear();
        foreach (var line in _lines.Skip(_lineOffset).Take(_displayLineCount))
        {
            LinesHost.Children.Add(new TextBlock
            {
                Text = line.Trim(),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = _textBrush
            });
        }

        var chapter = _chapters[_chapterIndex];
        ProgressText.Text = $"{chapter.Title} · {_chapterIndex + 1}/{_chapters.Count}";
        UpdateButtons();
    }

    /// <summary>
    /// 更新按钮状态与文字
    /// </summary>
    private void UpdateButtons()
    {
        UpLinesButton.Content = $"↑{_scrollStep}行";
        DownLinesButton.Content = $"↓{_scrollStep}行";

        var atChapterStart = _chapterIndex <= 0 && _lineOffset <= 0;
        var atChapterEnd = _chapterIndex >= _chapters.Count - 1 && _lineOffset >= MaxOffset;

        UpLinesButton.IsEnabled = !_isLoading && !atChapterStart;
        DownLinesButton.IsEnabled = !_isLoading && !atChapterEnd;
        PrevChapterButton.IsEnabled = !_isLoading && _chapterIndex > 0;
        NextChapterButton.IsEnabled = !_isLoading && _chapterIndex < _chapters.Count - 1;
    }

    /// <summary>
    /// 上 n 行（章节开头时自动回到上一章结尾）
    /// </summary>
    private async void UpLinesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        if (_lineOffset > 0)
        {
            _lineOffset = Math.Max(0, _lineOffset - _scrollStep);
            RefreshDisplay();
        }
        else if (_chapterIndex > 0)
        {
            await LoadChapterAsync(_chapterIndex - 1, startFromEnd: true);
        }
    }

    /// <summary>
    /// 下 n 行（章节末尾时自动进入下一章开头）
    /// </summary>
    private async void DownLinesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        if (_lineOffset < MaxOffset)
        {
            _lineOffset = Math.Min(MaxOffset, _lineOffset + _scrollStep);
            RefreshDisplay();
        }
        else if (_chapterIndex < _chapters.Count - 1)
        {
            await LoadChapterAsync(_chapterIndex + 1, startFromEnd: false);
        }
    }

    /// <summary>
    /// 上一章（从该章第一行开始）
    /// </summary>
    private async void PrevChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _chapterIndex <= 0)
        {
            return;
        }

        await LoadChapterAsync(_chapterIndex - 1, startFromEnd: false);
    }

    /// <summary>
    /// 下一章（从该章第一行开始）
    /// </summary>
    private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _chapterIndex >= _chapters.Count - 1)
        {
            return;
        }

        await LoadChapterAsync(_chapterIndex + 1, startFromEnd: false);
    }

    /// <summary>
    /// 加载章节并同步书架阅读进度
    /// </summary>
    private async Task LoadChapterAsync(int chapterIndex, bool startFromEnd)
    {
        _isLoading = true;
        UpdateButtons();
        LoadingPanel.Visibility = Visibility.Visible;
        LinesHost.Visibility = Visibility.Collapsed;

        try
        {
            var chapter = _chapters[chapterIndex];
            var content = await _apiService.GetChapterContentAsync(chapter.Id);
            if (_closed)
            {
                return;
            }

            _chapterIndex = chapterIndex;
            _lines = SplitLines(content);
            _lineOffset = startFromEnd ? MaxOffset : 0;

            await UpdateBookshelfProgressAsync(chapter);
            if (_closed)
            {
                return;
            }

            LinesHost.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
            _isLoading = false;
            RefreshDisplay();
        }
        catch
        {
            if (_closed)
            {
                return;
            }

            // 静默提示，保持小窗伪装
            _isLoading = false;
            LinesHost.Children.Clear();
            LinesHost.Children.Add(new TextBlock
            {
                Text = "加载失败，请重试",
                FontSize = 12,
                Foreground = _textBrush
            });
            LinesHost.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
            UpdateButtons();
        }
    }

    /// <summary>
    /// 同步书架的最后阅读记录
    /// </summary>
    private async Task UpdateBookshelfProgressAsync(ChapterInfo chapter)
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

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
