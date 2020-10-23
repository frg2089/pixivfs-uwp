using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Converters
{
    public class DownloadingProgressVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is int i && i == 100 ? Visibility.Collapsed : (object)Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
