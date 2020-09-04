﻿using System;
using System.Diagnostics;

using PixivCS;

using PixivFSUWP.Data;
using PixivFSUWP.Data.Collections;
using PixivFSUWP.Interfaces;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WaterfallPage : Page, IGoBackFlag
    {
        ListContent listContent;

        ViewModels.WaterfallItemViewModel tapped = null;

        private double? verticalOffset;
        private bool _backflag = false;

        public WaterfallPage()
        {
            this.InitializeComponent();
        }

        private async void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (tapped is null) return;
            System.Diagnostics.Debug.WriteLine(tapped.ItemId);
            await IllustDetail.FromObject(await new PixivAppAPI(OverAll.GlobalBaseAPI).GetIllustDetailAsync(tapped.ItemId.ToString())).DownloadFirstImage();
        }

        private async void QuickStar_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            var i = tapped;
            var title = i.Title;
            System.Diagnostics.Debug.WriteLine(i.ItemId);
            try
            {
                //用Title作标识，表明任务是否在执行
                i.Title = null;
                if (i.IsBookmarked)
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .PostIllustBookmarkDeleteAsync(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = false;
                        i.Stars--;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("DeletedBookmarkPlain"), title));
                    }
                    else
                    {
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("BookmarkDeleteFailedPlain"), title));
                    }
                }
                else
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .PostIllustBookmarkAddAsync(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = true;
                        i.Stars++;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("WorkBookmarkedPlain"), title));
                    }
                    else
                    {
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("WorkBookmarkFailedPlain"), title));
                    }
                }
            }
            finally
            {
                //确保出错时数据不被破坏
                i.Title = title;
            }
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            var WaterfallContent = sender as Controls.WaterfallContentPanel;
            if (ActualWidth < 700) WaterfallContent.Colums = 3;
            else if (ActualWidth < 900) WaterfallContent.Colums = 4;
            else if (ActualWidth < 1100) WaterfallContent.Colums = 5;
            else WaterfallContent.Colums = 6;
            if (verticalOffset != null)
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        WaterfallListView.ScrollToOffset(verticalOffset);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
            }
        }

        private void WaterfallListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            System.Diagnostics.Debug.WriteLine(tapped.ItemId);
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e)
            => Frame.Navigate(typeof(IllustDetailPage), (e.ClickedItem as ViewModels.WaterfallItemViewModel).ItemId, App.DrillInTransitionInfo);


        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            System.Diagnostics.Debug.WriteLine(tapped.ItemId);
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            (WaterfallListView.ItemsSource as IllustsCollectionBase)?.PauseLoading();
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(WaterfallPage), listContent);

                if (WaterfallListView.ItemsSource is SearchResultIllustsCollection searchResult)
                    IllustsCollectionManager.SearchResults.Push(searchResult);

                OverAll.TheMainPage?.UpdateNavButtonState();
            }
            if (WaterfallListView.ItemsSource is IllustsCollectionBase collection)
            {
                System.Diagnostics.Debug.WriteLine(WaterfallListView.ItemsSource + "PauseLoading");
                collection.VerticalOffset = WaterfallListView.VerticalOffset;
                collection.PauseLoading();
            }
            WaterfallListView.ItemsSource = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ListContent content)
                listContent = content;

            switch (listContent)
            {
                case ListContent.Recommend:
                    if (IllustsCollectionManager.RecommendList is null) IllustsCollectionManager.RefreshRecommendList();
                    WaterfallListView.ItemsSource = IllustsCollectionManager.RecommendList;
                    break;
                case ListContent.Bookmark:
                    if (IllustsCollectionManager.BookmarkList is null) IllustsCollectionManager.RefreshBookmarkList();
                    WaterfallListView.ItemsSource = IllustsCollectionManager.BookmarkList;
                    break;
                case ListContent.Following:
                    if (IllustsCollectionManager.FollowingList is null) IllustsCollectionManager.RefreshFollowingList();
                    WaterfallListView.ItemsSource = IllustsCollectionManager.FollowingList;
                    break;
                case ListContent.Ranking:
                    if (IllustsCollectionManager.RankingList is null) IllustsCollectionManager.RefreshRankingList();
                    WaterfallListView.ItemsSource = IllustsCollectionManager.RankingList;
                    break;
                case ListContent.SearchResult:
                    ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(OverAll.GetResourceString("SearchPagePlain"));
                    WaterfallListView.ItemsSource = IllustsCollectionManager.SearchResults.Pop();
                    break;
            }
            if (WaterfallListView.ItemsSource is IllustsCollectionBase collection)
            {
                System.Diagnostics.Debug.WriteLine(WaterfallListView.ItemsSource + "\tResumeLoading");
                collection.ResumeLoading();
                verticalOffset = collection.VerticalOffset;
            }
        }
        public void SetBackFlag(bool value) => _backflag = value;

        public enum ListContent
        {
            Recommend,
            Bookmark,
            Following,
            Ranking,
            SearchResult,
        }
    }
}