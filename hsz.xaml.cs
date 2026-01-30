using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;

namespace _123云盘UWP
{
    public sealed partial class hsz : Page
    {
        private Stack<string> _navigationHistory = new Stack<string>();
        private string _currentFolderId = "0";
        public string SelectedFileId { get; private set; }
        private bool _isLoadingNextPage = false;
        private bool _hasMorePages = true;
        private long _currentLastFileId = 0;
        private ObservableCollection<FileInfoModel> _currentFileItems;

        public hsz()
        {
            this.InitializeComponent();
            this.Loaded += HszPage_Loaded;
        }

        private void HszPage_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = FindChildOfType<ScrollViewer>(FileListView);
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }
        }
        private T FindChildOfType<T>(DependencyObject root) where T : DependencyObject
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is T) return (T)child;
                    queue.Enqueue(child);
                }
            }
            return null;
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate || !_hasMorePages || _isLoadingNextPage) return;

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - scrollViewer.ViewportHeight)
            {
                _isLoadingNextPage = true;
                FileListView.Footer = new Windows.UI.Xaml.Controls.ProgressRing { IsActive = true, Width = 20, Height = 20, Margin = new Thickness(0, 10, 0, 10) };
                await LoadNextPageAsync();
            }
        }

        private async Task LoadNextPageAsync()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                client.DefaultRequestHeaders.Add("Platform", "open_platform");

                string url = $"https://open-api.123pan.com/api/v2/file/list?parentFileId=0&limit=100&lastFileId={_currentLastFileId}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var root = JsonConvert.DeserializeObject<RootObject>(content);

                    if (root?.data != null)
                    {
                        _currentLastFileId = root.data.lastFileId;
                        _hasMorePages = _currentLastFileId != -1;

                        foreach (var file in root.data.fileList)
                        {
                            if (file.trashed == "1")
                            {
                                _currentFileItems.Add(new FileInfoModel
                                {
                                    FileName = file.fileName,
                                    FileDate = file.updateAt,
                                    IconPath = GetIconPath(file.type, file.category),
                                    FileId = file.fileId
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 加载下一页失败
            }
            finally
            {
                _isLoadingNextPage = false;
                FileListView.Footer = null;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = LoadFileListAsync("0");
        }

        private async Task LoadFileListAsync(string parentFileId)
        {
            _isLoadingNextPage = false;
            _hasMorePages = true;
            _currentLastFileId = 0;

            _currentFileItems = new ObservableCollection<FileInfoModel>();
            FileListView.ItemsSource = _currentFileItems;

            try
            {
                jindu.Visibility = Visibility.Visible;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                client.DefaultRequestHeaders.Add("Platform", "open_platform");

                var response = await client.GetAsync($"https://open-api.123pan.com/api/v2/file/list?parentFileId={parentFileId}&limit=100");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var root = JsonConvert.DeserializeObject<RootObject>(content);

                    if (root?.data != null)
                    {
                        _currentLastFileId = root.data.lastFileId;
                        _hasMorePages = _currentLastFileId != -1;

                        foreach (var file in root.data.fileList)
                        {
                            if (file.trashed == "1")
                            {
                                _currentFileItems.Add(new FileInfoModel
                                {
                                    FileName = file.fileName,
                                    FileDate = file.updateAt,
                                    IconPath = GetIconPath(file.type, file.category),
                                    FileId = file.fileId
                                });
                            }
                        }
                    }

                    if (_currentFileItems.Any())
                    {
                        qk.Visibility = Visibility.Visible;
                        sc.Visibility = Visibility.Collapsed;
                        hy.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        qk.Visibility = Visibility.Collapsed;
                        sc.Visibility = Visibility.Collapsed;
                        hy.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    Frame.Navigate(typeof(mistake));
                }
            }
            catch (Exception)
            {
                Frame.Navigate(typeof(mistake));
            }
            finally
            {
                jindu.Visibility = Visibility.Collapsed;
            }
        }

        private async void FileListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "回收站内不支持查看，请还原后再试";
            dialog.PrimaryButtonText = "确定";
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();
        }

        private void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as FileInfoModel;
            if (clickedItem != null)
            {
                SelectedFileId = clickedItem.FileId;
                App.File = clickedItem.FileId;
                if (_currentFileItems.Any())
                {
                    qk.Visibility = Visibility.Collapsed;
                    sc.Visibility = Visibility.Visible;
                    hy.Visibility = Visibility.Visible;
                }
            }
            else
            {
                sc.Visibility = Visibility.Collapsed;
                hy.Visibility = Visibility.Collapsed;
                if (_currentFileItems.Any())
                {
                    qk.Visibility = Visibility.Visible;
                }
            }
        }

        private string GetIconPath(string type, string category)
        {
            if (type == "1")
                return "ms-appx:///Assets/文件夹.png";

            switch (category)
            {
                case "1": return "ms-appx:///Assets/音频.png";
                case "2": return "ms-appx:///Assets/视频.png";
                case "3": return "ms-appx:///Assets/图片.png";
                case "4": return "ms-appx:///Assets/PDF.png";
                case "5": return "ms-appx:///Assets/文档.png";
                case "6": return "ms-appx:///Assets/记事本.png";
                case "10": return "ms-appx:///Assets/压缩文件.png";
                case "13": return "ms-appx:///Assets/安卓.png";
                default: return "ms-appx:///Assets/未知.png";
            }
        }

        private async void sc_Click(object sender, RoutedEventArgs e)
        {
            await GetscInfo();
        }

        private async Task GetscInfo()
        {
            sc.IsEnabled = false;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string url = $"https://open-api.123pan.com/api/v1/file/delete?fileIDs={App.File}";
                    var content = new StringContent("");
                    httpClient.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    ContentDialog dialog = new ContentDialog();
                    dialog.Title = "已成功删除";
                    dialog.Content = "系统清理需要一些时间，请稍后查看";
                    dialog.PrimaryButtonText = "确定";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var result = await dialog.ShowAsync();
                    await LoadFileListAsync("0");
                }
            }
            catch (Exception)
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = "删除失败，请重试";
                dialog.PrimaryButtonText = "确定";
                dialog.DefaultButton = ContentDialogButton.Primary;
                var result = await dialog.ShowAsync();
            }
            finally
            {
                sc.IsEnabled = true;
            }
        }

        private async void hy_Click(object sender, RoutedEventArgs e)
        {
            hy.IsEnabled = false;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string url = $"https://open-api.123pan.com/api/v1/file/recover?fileIDs={App.File}";
                    var content = new StringContent("");
                    httpClient.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    ContentDialog dialog = new ContentDialog();
                    dialog.Title = "还原成功";
                    dialog.PrimaryButtonText = "确定";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var result = await dialog.ShowAsync();
                    await LoadFileListAsync("0");
                }
            }
            catch (Exception)
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = "还原失败，请稍后再试";
                dialog.PrimaryButtonText = "确定";
                dialog.DefaultButton = ContentDialogButton.Primary;
                var result = await dialog.ShowAsync();
            }
            finally
            {
                hy.IsEnabled = true;
            }
        }
    }
}