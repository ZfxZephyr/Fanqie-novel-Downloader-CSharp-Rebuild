using Microsoft.UI.Xaml.Data;

namespace FanqieNovelDownloader.Converters;

/// <summary>
/// 章节数量转换器
/// </summary>
public class ChapterCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return count switch
            {
                >= 1000 => $"{count / 1000.0:F1}k章",
                _ => $"{count}章"
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
