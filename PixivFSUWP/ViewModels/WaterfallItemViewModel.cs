using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixivFSUWP.Data;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using System.ComponentModel;

namespace PixivFSUWP.ViewModels
{
    public class WaterfallItemViewModel : INotifyPropertyChanged
    {
        public int ItemId { get; private set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUri { get; set; }
        public int Stars { get; set; }
        public int Pages { get; set; }
        public bool IsBookmarked { get; set; }
        public BitmapImage ImageSource
        {
            get => imageSource;
            set
            {
                imageSource = value;
                NotifyChange(nameof(ImageSource));
            }
        }
        public int Width { get; set; }
        public int Height { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private WeakReference<BitmapImage> imageSourceWeakReference;
        private BitmapImage imageSource;
        private System.Threading.CancellationTokenSource cancelSource = new System.Threading.CancellationTokenSource();

        public async Task LoadImageAsync()
        {
            if (ImageSource is null
                && !(imageSourceWeakReference is null)
                && imageSourceWeakReference.TryGetTarget(out var img))
            {
                ImageSource = img;
            }
            else
                ImageSource = await Data.OverAll.LoadImageAsync(ImageUri, cancelSource.Token);
        }
        public void ReleaseImage()
        {
            cancelSource.Cancel();
            if (!(ImageSource is null))
            {
                imageSourceWeakReference = new WeakReference<BitmapImage>(ImageSource);
                ImageSource = null;
            }
        }

        public string StarsString
        {
            get
            {
                return "★" + Stars.ToString();
            }
        }

        public static WaterfallItemViewModel FromItem(WaterfallItem Item)
            => new WaterfallItemViewModel()
            {
                ItemId = Item.Id,
                Title = Item.Title,
                Author = Item.Author,
                ImageUri = Item.ImageUri,
                IsBookmarked = Item.IsBookmarked,
                Stars = Item.Stars,
                Pages = Item.Pages,
                Width = Item.Width,
                Height = Item.Height
            };

        public void NotifyChange(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}
