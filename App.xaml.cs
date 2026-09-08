using FanqieNovelDownloader.Services;
using Microsoft.UI.Xaml;

namespace FanqieNovelDownloader;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// 下载服务单例
    /// </summary>
    public static DownloadService DownloadService { get; } = new();

    /// <summary>
    /// 书架服务单例
    /// </summary>
    public static BookshelfService BookshelfService { get; } = new();

    /// <summary>
    /// 历史记录服务单例
    /// </summary>
    public static HistoryService HistoryService { get; } = new();

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 加载本地数据
        await BookshelfService.LoadAsync();
        await HistoryService.LoadAsync();

        _window = new MainWindow();
        _window.Activate();
    }
}
