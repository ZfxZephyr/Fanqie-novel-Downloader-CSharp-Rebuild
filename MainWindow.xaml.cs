using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using FanqieNovelDownloader.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FanqieNovelDownloader;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // 未打包模式（WindowsPackageType=None）下 ms-appx URI 失效，
        // 必须使用输出目录的绝对路径设置图标
        var iconDir = Path.Combine(AppContext.BaseDirectory, "Assets");

        // 窗口图标（标题栏 + 任务栏），使用 ICO
        var icoPath = Path.Combine(iconDir, "AppIcon.ico");
        if (File.Exists(icoPath))
        {
            AppWindow.SetIcon(icoPath);
        }

        // 标题栏控件左侧的 Logo，使用 PNG
        var pngPath = Path.Combine(iconDir, "AppIcon.png");
        if (File.Exists(pngPath))
        {
            AppTitleBar.IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(new Uri(pngPath))
            };
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "search":
                    NavFrame.Navigate(typeof(SearchPage));
                    break;
                case "bookshelf":
                    NavFrame.Navigate(typeof(BookshelfPage));
                    break;
                case "downloads":
                    NavFrame.Navigate(typeof(DownloadsPage));
                    break;
                case "history":
                    NavFrame.Navigate(typeof(HistoryPage));
                    break;
                case "about":
                    NavFrame.Navigate(typeof(AboutPage));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
            }
        }
    }
}
