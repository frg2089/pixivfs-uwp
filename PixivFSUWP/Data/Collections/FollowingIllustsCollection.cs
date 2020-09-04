﻿using System;
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

namespace PixivFSUWP.Data.Collections
{
    public class FollowingIllustsCollection : IllustsCollectionBase
    {
        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                if (!HasMoreItems) return new LoadMoreItemsResult() { Count = 0 };
                LoadMoreItemsResult toret = new LoadMoreItemsResult() { Count = 0 };
                PixivCS.Objects.UserIllusts followingres = null;
                try
                {
                    if (nexturl == "begin")
                        followingres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustFollowAsync();
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        followingres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustFollowAsync(getparam("restrict"), getparam("offset"));
                    }
                }
                catch
                {
                    return toret;
                }
                nexturl = followingres.NextUrl?.ToString() ?? "";
                return await LoadMoreItems(toret, followingres.Illusts);
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

