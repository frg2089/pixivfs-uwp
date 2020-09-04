using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Data.Collections
{
    public class RankingIllustsCollection : IllustsCollectionBase
    {
        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                if (!HasMoreItems) return new LoadMoreItemsResult() { Count = 0 };
                LoadMoreItemsResult toret = new LoadMoreItemsResult() { Count = 0 };
                PixivCS.Objects.UserIllusts rankingres = null;
                try
                {
                    if (nexturl == "begin")
                        rankingres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRankingAsync();
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        rankingres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRankingAsync(Mode: getparam("mode"), Filter: getparam("filter"), Offset: getparam("offset"));
                    }
                }
                catch
                {
                    return toret;
                }
                nexturl = rankingres.NextUrl?.ToString() ?? "";
                return await LoadMoreItems(toret, rankingres.Illusts);
            }
            finally
            {
                _busy = false;
                if (_emergencyStop)
                {
                    nexturl = "";
                    Clear();
                    GC.Collect();
                }
            }
        }
    }
}
