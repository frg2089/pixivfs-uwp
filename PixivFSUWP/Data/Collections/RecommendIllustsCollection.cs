using PixivCS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using System.Web;
using Windows.Data.Json;
using System.Diagnostics;

namespace PixivFSUWP.Data.Collections
{
    public class RecommendIllustsCollection : IllustsCollectionBase
    {
        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                LoadMoreItemsResult toret = new LoadMoreItemsResult { Count = 0 };
                if (!HasMoreItems) return toret;
                PixivCS.Objects.IllustRecommended recommendres = null;
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadMoreItemsAsync]\t{nexturl}");
                    if (nexturl == "begin")
                        recommendres = await new PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRecommendedAsync();
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        recommendres = await new PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRecommendedAsync(
                                ContentType: getparam("content_type"),
                                IncludeRankingLabel: bool.Parse(getparam("include_ranking_label")),
                                Filter: getparam("filter"),
                                MinBookmarkIDForRecentIllust: getparam("min_bookmark_id_for_recent_illust"),
                                MaxBookmarkIDForRecommended: getparam("max_bookmark_id_for_recommend"),
                                Offset: getparam("offset"),
                                IncludeRankingIllusts: bool.Parse(getparam("include_ranking_illusts")),
                                IncludePrivacyPolicy: getparam("include_privacy_policy"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    return toret;
                }
                nexturl = recommendres.NextUrl?.ToString() ?? "";
                System.Diagnostics.Debug.WriteLine($"[RecommendIllustsCollection]\t{nexturl}");
                if (recommendres.Illusts is null)
                {
                    System.Diagnostics.Debug.WriteLine("出现了NULL");
                    // 这里调用更新身份信息的方法

                    System.Diagnostics.Debug.WriteLine("Done");
                }
                return await LoadMoreItems(toret, recommendres.Illusts);
            }
            finally
            {
                LoadMoreItemsAsync_Finally();
            }
        }
    }
}

