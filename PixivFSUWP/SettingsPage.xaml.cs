﻿using PixivFSUWP.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.Credentials;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static PixivFSUWP.Data.OverAll;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page, IGoBackFlag
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            _ = loadContentsAsync();
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            TheMainPage?.SelectNavPlaceholder(GetResourceString("SettingsPagePlain"));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(SettingsPage), null);
                TheMainPage?.UpdateNavButtonState();
            }
        }

        async Task loadContentsAsync()
        {
            var imgTask = LoadImageAsync(currentUser.Avatar170);
            txtVersion.Text = GetResourceString("ReleasedVersion") + string.Format("{0} version-{1}.{2}.{3} {4}",
                Package.Current.DisplayName,
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Architecture);
            txtPkgName.Text = GetResourceString("ReleasedID") + string.Format("{0}", Package.Current.Id.Name);
            txtInsDate.Text = GetResourceString("ReleasedTime") + string.Format("{0}", Package.Current.InstalledDate.ToLocalTime().DateTime);
            txtID.Text = currentUser.ID.ToString();
            txtName.Text = currentUser.Username;
            txtAccount.Text = "@" + currentUser.UserAccount;
            txtEmail.Text = currentUser.Email;
            //硬编码主要开发者信息
            List<ViewModels.ContributorViewModel> mainDevs = new List<ViewModels.ContributorViewModel>();
            mainDevs.Add(new ViewModels.ContributorViewModel()
            {
                Account = "@tobiichiamane",
                DisplayName = "Communist Fish",
                AvatarUrl = "https://avatars2.githubusercontent.com/u/14824064?v=4&s=45",
                ProfileUrl = "https://github.com/tobiichiamane",
                Contributions = new List<Data.Contribution>()
                {
                    Data.Contribution.bug,
                    Data.Contribution.code,
                    Data.Contribution.doc,
                    Data.Contribution.idea,
                    Data.Contribution.translation
                }
            });
            lstMainDev.ItemsSource = mainDevs;
            //加载贡献者信息
            _ = loadContributors();
            //TODO: 考虑设置项不存在的情况
            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //tbSauceNAO.Text = localSettings.Values["SauceNAOAPI"] as string;//读取设置项
            //tbImgur.Text = localSettings.Values["ImgurAPI"] as string;
            // 获取储存的颜色主题信息
            switch (ApplicationData.Current.LocalSettings.Values["ColorTheme"])
            {
                case false:
                    cboxColorTheme.SelectedIndex = 0;
                    break;
                case true:
                    cboxColorTheme.SelectedIndex = 1;
                    break;
                default:
                    cboxColorTheme.SelectedIndex = 2;
                    break;
            }
            // 检查文件系统访问权限
            //_ = FileAccessPermissionsCheck();

            // 自动保存
            if (ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] is bool autosave)
                UseAutoSave_CB.IsChecked = autosave;
            else UseAutoSave_CB.IsChecked = false;

            // 图片名格式
            if (ApplicationData.Current.LocalSettings.Values["PictureSaveName"] is string psn &&
                !string.IsNullOrWhiteSpace(psn))
                PicName_ASB.Text = psn;
            else PicName_ASB.Text = "${picture_id}_${picture_page}";
            PicName_ASB.PlaceholderText = PicName_ASB.Text;

            // 图片保存位置
            if (ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] is string psd &&
                !string.IsNullOrWhiteSpace(psd))
                PicSaveDir_ASB.Text = psd;
            else
                PicSaveDir_ASB.Text = KnownFolders.SavedPictures.Path;
            PicSaveDir_ASB.PlaceholderText = PicSaveDir_ASB.Text;
            _ = calculateCacheSize();
            //等待头像加载完毕
            imgAvatar.ImageSource = await imgTask;
        }

        async Task loadContributors()
        {
            progressLoadingContributors.Visibility = Visibility.Visible;
            progressLoadingContributors.IsActive = true;
            var res = await Data.ContributorsHelper.GetContributorsAsync();
            if (res == null)
            {
                progressLoadingContributors.Visibility = Visibility.Collapsed;
                progressLoadingContributors.IsActive = false;
                txtContributors.Text = GetResourceString("ContributorsReadingErrorPlain");
                return;
            }
            var enumerable = from item in res select ViewModels.ContributorViewModel.FromItem(item);
            lstContributors.ItemsSource = enumerable.ToList();
            progressLoadingContributors.Visibility = Visibility.Collapsed;
            progressLoadingContributors.IsActive = false;
            lstContributors.Visibility = Visibility.Visible;
        }

        async Task calculateCacheSize()
        {
            //计算缓存大小
            var cacheSize = await Data.CacheManager.GetCacheSizeAsync();
            decimal sizeInMB = new decimal(cacheSize) / new decimal(1048576);
            txtCacheSize.Text = decimal.Round(sizeInMB, 2).ToString() + "MB";
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var vault = new PasswordVault();
            try
            {
                vault.Remove(GetCredentialFromLocker(passwordResource));
                vault.Remove(GetCredentialFromLocker(refreshTokenResource));
            }
            catch { }
            finally
            {
                TheMainPage.Frame.Navigate(typeof(LoginPage), null, App.FromRightTransitionInfo);
            }
        }

        private async void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new
                Uri(@"https://github.com/tobiichiamane/pixivfs-uwp"));
        }

        private void API_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["SauceNAOAPI"] = tbSauceNAO.Text;
            localSettings.Values["ImgurAPI"] = tbImgur.Text;
        }

        private async void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = GetResourceString("Recalculating");
            await Data.CacheManager.ClearCacheAsync();
            await calculateCacheSize();
        }

        private async void btnDelInvalid_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = GetResourceString("Recalculating");
            await Data.CacheManager.ClearTempFilesAsync();
            await calculateCacheSize();
        }

        private async void lstMainDev_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as ViewModels.ContributorViewModel;
            await Launcher.LaunchUriAsync(new Uri(item.ProfileUrl));
        }

        private async void btnQQGroup_Click(object sender, RoutedEventArgs e)
        {
            //腾讯的一键加群
            await Launcher.LaunchUriAsync(new
                Uri(@"https://shang.qq.com/wpa/qunwpa?idkey=d6ba54103ced0e2d7c5bbf6422e4f9f6fa4849c91d4521fe9a1beec72626bbb6"));
        }

        private void ComboBox_DropDownClosed(object sender, object e)
        {
            if (sender is ComboBox cb)
            {
                // 保存颜色主题信息
                switch (cb.SelectedIndex)
                {
                    case 2:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = null;
                        break;
                    case 0:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = false;
                        break;
                    case 1:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = true;
                        break;
                }
                _ = TheMainPage?.ShowTip(GetResourceString("RestartApplyColorTheme"));
            }
        }

        private async void PicSaveDir_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text;

            try
            {
                _ = await StorageFolder.GetFolderFromPathAsync(text);
                ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] = text;
            }
            catch (UnauthorizedAccessException)
            {
                FSProblem_LinkBtn.Visibility = Visibility.Visible;
                sender.Text = ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] as string;
            }
        }

        private async void SelectSaveDir_QS(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                sender.Text = folder.Path;
            }
        }

        private void PicName_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(sender.Text))
            {
                ApplicationData.Current.LocalSettings.Values["PictureSaveName"] = sender.Text;
            }
        }
        private void PicNameHelp_QS(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(sender);
        }

        private void FileSystemHelp_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        // 检查文件系统访问权限
        private static async Task<ValueTuple<bool, bool>> FileAccessPermissionsCheck()
        {
            // 看我这神奇的写法 用捕捉异常来实现
            // 主要是因为我不会用别的方式检查...

            (bool PictureLibrary, bool FileSystem) result;
            try
            {
                _ = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                result.PictureLibrary = true;
            }
            catch (UnauthorizedAccessException)
            {
                result.PictureLibrary = false;
            }
            try
            {
                _ = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.DoNotVerify));
                result.FileSystem = true;
            }
            catch (UnauthorizedAccessException)
            {
                result.FileSystem = false;
            }
            return result;
        }

        private void AutoSave_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked ?? false)
            {
                AutoSavePanel.Visibility = Visibility.Visible;
                ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] = true;
            }
            else
            {
                AutoSavePanel.Visibility = Visibility.Collapsed;
                ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] = false;
            }
        }
    }
}
