﻿using System;
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
    public class UserIllustsCollection : IllustsCollectionBase
    {
        readonly string userID;

        public UserIllustsCollection(string UserID)
        {
            userID = UserID;
        }

        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                if (!HasMoreItems) return new LoadMoreItemsResult() { Count = 0 };
                LoadMoreItemsResult toret = new LoadMoreItemsResult() { Count = 0 };
                PixivCS.Objects.UserIllusts illustsres = null;
                try
                {
                    if (nexturl == "begin")
                        illustsres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetUserIllustsAsync(userID);
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        illustsres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetUserIllustsAsync(getparam("user_id"), getparam("type"), getparam("filter"), getparam("offset"));
                    }
                }
                catch
                {
                    return toret;
                }
                nexturl = illustsres.NextUrl?.ToString() ?? "";
                return await LoadMoreItems(toret, illustsres.Illusts);
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
