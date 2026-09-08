# 番茄小说阅读器

一款基于 WinUI 3 的 Windows 桌面应用，支持搜索、阅读和下载番茄小说。

## 功能特性

- **小说搜索** - 通过番茄小说 API 搜索小说资源
- **书架管理** - 收藏和管理喜爱的小说
- **小说下载** - 支持多任务并发下载，最多同时下载 3 个任务
- **在线阅读** - 内置阅读器，支持章节浏览
- **下载管理** - 查看下载进度、暂停/恢复/取消下载任务

## 技术栈

- **框架**: .NET 10 + Windows App SDK 2.4.0 (WinUI 3)
- **目标平台**: Windows 10 (版本 1809 / 17763) 及以上
- **支持架构**: x86 / x64 / ARM64

## 项目结构

```
FanqieNovelDownloader/
├── Assets/             # 应用图标和图片资源
├── Converters/         # XAML 值转换器
├── Models/             # 数据模型
│   ├── ApiResponse.cs
│   ├── BookInfo.cs
│   ├── BookshelfItem.cs
│   ├── DownloadTask.cs
│   └── HistoryItem.cs
├── Pages/              # 页面
│   ├── SearchPage      # 搜索页
│   ├── BookDetailPage  # 书籍详情页
│   ├── BookshelfPage   # 书架页
│   ├── DownloadsPage   # 下载管理页
│   ├── ReaderPage      # 阅读器页
│   ├── HistoryPage     # 历史记录页
│   ├── SettingsPage    # 设置页
│   └── AboutPage       # 关于页
├── Services/           # 业务服务
│   ├── FanqieApiService   # 番茄小说 API 服务
│   ├── DownloadService    # 下载管理服务
│   ├── BookshelfService   # 书架服务
│   ├── HistoryService     # 历史记录服务
│   └── Logger             # 日志服务
├── App.xaml(.cs)       # 应用入口
└── MainWindow.xaml(.cs)# 主窗口
```

## 环境要求

- Windows 10 版本 1809 (Build 17763) 或更高
- [.NET 10 SDK](https://dotnet.microsoft.com/)

## 构建与运行

```bash
# 克隆仓库
git clone https://github.com/happy-zephyr/Fanqie-novel-Downloader-C--Rebuild.git

# 进入项目目录
cd Fanqie-novel-Downloader-C--Rebuild/FanqieNovelDownloader

# 还原依赖
dotnet restore

# 运行应用
dotnet run
```

## 发布

```bash
# 发布为独立应用 (x64)
dotnet publish -c Release -r win-x64 --self-contained

# 发布为独立应用 (ARM64)
dotnet publish -c Release -r win-arm64 --self-contained
```

## 许可证

本项目采用 [The Unlicense](LICENSE) 许可证 - 详见 [LICENSE](LICENSE) 文件。
