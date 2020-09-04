using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using PixivCS;

using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Data.Collections
{
    public abstract class IllustsCollectionBase : CollectionBase<ViewModels.WaterfallItemViewModel>
    {
        public double VerticalOffset;

        protected virtual async Task<LoadMoreItemsResult> LoadMoreItems(LoadMoreItemsResult toret, PixivCS.Objects.UserPreviewIllust[] Illusts)
        {
            foreach (var recillust in Illusts)
            {
                await Task.Run(pause.WaitOne);
                if (_emergencyStop)
                {
                    nexturl = "";
                    Clear();
                    return new LoadMoreItemsResult { Count = 0 };
                }
                WaterfallItem recommendi = WaterfallItem.FromObject(recillust);
                var recommendmodel = ViewModels.WaterfallItemViewModel.FromItem(recommendi);
                System.Diagnostics.Debug.WriteLine($"LoadIllusts:{toret.Count + 1}/{Illusts.Length}\t{recommendmodel.ItemId}");
                _ = recommendmodel.LoadImageAsync();
                Add(recommendmodel);
                toret.Count++;
            }
            return toret;
        }
    }
}
