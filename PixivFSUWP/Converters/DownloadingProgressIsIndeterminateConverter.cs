using System;

using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Converters
{
    public class DownloadingProgressIsIndeterminateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is int i && i <= 0;
        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
