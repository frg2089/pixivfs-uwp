using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PixivFSUWP.Controls
{
    public class WaterfallContentPanel : Panel
    {
        //此属性决定瀑布流列数
        public static readonly DependencyProperty ColumsProperty =
            DependencyProperty.Register("Colums", typeof(int),
                typeof(WaterfallContentPanel), new PropertyMetadata(2,
                    (DepObj, e) =>
                    {
                        (DepObj as WaterfallContentPanel).InvalidateMeasure();
                        (DepObj as WaterfallContentPanel).InvalidateArrange();
                    }));

        public int Colums
        {
            get => (int)GetValue(ColumsProperty);
            set => SetValue(ColumsProperty, value);
        }

        //此属性决定项目间隔
        public static readonly DependencyProperty ItemMarginProperty =
            DependencyProperty.Register("ItemMargin", typeof(double),
                typeof(WaterfallContentPanel), new PropertyMetadata((double)0,
                    (DepObj, e) =>
                    {
                        (DepObj as WaterfallContentPanel).InvalidateMeasure();
                        (DepObj as WaterfallContentPanel).InvalidateArrange();
                    }));

        public double ItemMargin
        {
            get => (double)GetValue(ItemMarginProperty);
            set => SetValue(ItemMarginProperty, value);
        }

        //此属性决定顶端间距
        public static readonly DependencyProperty TopOffsetProperty =
            DependencyProperty.Register("TopOffset", typeof(double),
                typeof(WaterfallContentPanel), new PropertyMetadata((double)0,
                    (DepObj, e) =>
                    {
                        (DepObj as WaterfallContentPanel).InvalidateMeasure();
                        (DepObj as WaterfallContentPanel).InvalidateArrange();
                    }));

        public double TopOffset
        {
            get => (double)GetValue(TopOffsetProperty);
            set => SetValue(TopOffsetProperty, value);
        }

        //测量panel需要的空间
        //宽度填满，高度进行计算
        protected override Size MeasureOverride(Size availableSize)
        {
            Size toret = new Size();
            List<double> heights = (new double[Colums]).ToList();
            toret.Width = availableSize.Width;
            double itemwidth = (availableSize.Width - ItemMargin * (Colums - 1)) / Colums;
            foreach (var i in Children)
            {
                i.Measure(new Size(itemwidth, double.PositiveInfinity));
                heights[heights.IndexOf(heights.Min())] += ItemMargin + i.DesiredSize.Height;
                (i as FrameworkElement).Tag = i.DesiredSize;
                //if (i.DesiredSize.Height > 0)
                //    if (((i as ListViewItem).ContentTemplateRoot as Grid)?.Children[0] is Image image)
                //    {
                //        image.Height = i.DesiredSize.Height;
                //        image.Width = i.DesiredSize.Width;
                //    }
            }
            toret.Height = heights.Max() + TopOffset;
            return toret;
        }

        private bool virtualizationBusy = false;
        internal void Virtualization()
        {
            Debug.WriteLine($"[{nameof(WaterfallContentPanel)}.{nameof(MeasureOverride)}]\t:Window Height = {Window.Current.Bounds.Height}\tActualHeight = {ActualHeight}");
            if (virtualizationBusy)
                return;

            try
            {
                virtualizationBusy = true;
                if (Tag is ScrollViewer scrollViewer)
                {
                    Debug.WriteLine($"[{nameof(WaterfallContentPanel)}.{nameof(MeasureOverride)}]\t:ScrollViewer.VerticalOffset = {scrollViewer.VerticalOffset}");

                    foreach (var i in Children)
                    {
                        var generalTransform = i.TransformToVisual(this);
                        var point = generalTransform.TransformPoint(new Point(0, 0));
                        if (point.Y + i.DesiredSize.Height < scrollViewer.VerticalOffset - 500)
                        {
                            if (((i as ListViewItem).ContentTemplateRoot as Grid)?.Children[0] is Image image)
                            {
                                image.Height = image.ActualHeight;
                                image.Width = i.DesiredSize.Width;
                            }
                            ((i as FrameworkElement).DataContext as ViewModels.WaterfallItemViewModel).ReleaseImage();
                            i.Opacity = 0;
                            //i.Visibility = Visibility.Collapsed;
                        }
                        else if (point.Y > scrollViewer.VerticalOffset + Window.Current.Bounds.Height + 500)
                        {
                            if (((i as ListViewItem).ContentTemplateRoot as Grid)?.Children[0] is Image image)
                            {
                                image.Height = image.ActualHeight;
                                image.Width = i.DesiredSize.Width;
                            }
                            ((i as FrameworkElement).DataContext as ViewModels.WaterfallItemViewModel).ReleaseImage();
                            i.Opacity = 0;
                            //i.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            var task = ((i as FrameworkElement).DataContext as ViewModels.WaterfallItemViewModel).LoadImageAsync();
                            i.Opacity = 1;
                            task.ContinueWith(t => i.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                if (((i as ListViewItem).ContentTemplateRoot as Grid)?.Children[0] is Image image)
                                {
                                    image.Height = double.NaN;
                                    image.Width = double.NaN;
                                }
                            }));
                            //if ((i as FrameworkElement).Tag is Size size)
                            //    size.Height;
                            //i.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            finally
            {
                virtualizationBusy = false;
            }
        }

        //排版，不改变大小
        protected override Size ArrangeOverride(Size finalSize)
        {
            List<double> Xs = new List<double>();
            List<double> Ys = new List<double>();
            for (int i = 0; i < Colums; i++)
            {
                Xs.Add(i * (DesiredSize.Width + ItemMargin) / Colums);
                Ys.Add(TopOffset);
            }
            foreach (var i in Children)
            {
                var minC = Ys.IndexOf(Ys.Min());
                i.Arrange(new Rect(Xs[minC], Ys[minC], i.DesiredSize.Width, i.DesiredSize.Height));
                Ys[minC] += i.DesiredSize.Height + ItemMargin;
            }
            return finalSize;
        }
    }
}
