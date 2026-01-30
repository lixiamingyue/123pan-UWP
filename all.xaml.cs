using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace _123云盘UWP
{
    public sealed partial class all : Page
    {
        private Stack<string> _navigationHistory = new Stack<string>();
        private string _currentFolderId = "0";
        private StorageFolder _saveFolder;
        private string _Id;
        private string parentFileId;
        private List<string> _allFileNames = new List<string>();
        private string _currentClickedFileName;
        public string SelectedFileId { get; private set; }
        private ObservableCollection<Folder> _folderBreadcrumbs = new ObservableCollection<Folder>();
        public static List<StorageFile> SelectedFiles = new List<StorageFile>();
        public static List<StorageFolder> SelectedFolders = new List<StorageFolder>();
        public static bool IsFolderUpload = false;
        private bool _isLoadingNextPage = false;
        private bool _hasMorePages = true;
        private long _currentLastFileId = 0;
        private string _currentSearchKeyword = "";
        private ObservableCollection<FileInfoModel> _currentFileItems;

        public all()
        {
            this.InitializeComponent();
            this.Loaded += AllPage_Loaded;
        }

        private void AllPage_Loaded(object sender, RoutedEventArgs e)
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

                string url;
                if (!string.IsNullOrEmpty(_currentSearchKeyword))
                {
                    url = $"https://open-api.123pan.com/api/v2/file/list?parentFileId=0&limit=100&searchData={Uri.EscapeDataString(_currentSearchKeyword)}&searchMode=0&lastFileId={_currentLastFileId}";
                }
                else
                {
                    url = $"https://open-api.123pan.com/api/v2/file/list?parentFileId={_currentFolderId}&limit=100&lastFileId={_currentLastFileId}";
                }

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
                            if (file.trashed != "1")
                            {
                                _currentFileItems.Add(new FileInfoModel
                                {
                                    FileName = file.fileName,
                                    FileDate = file.updateAt,
                                    IconPath = GetIconPath(file.type, file.category),
                                    FileId = file.fileId,
                                    Type = file.type
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 加载下一页失败·
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
            _folderBreadcrumbs.Clear();
            _folderBreadcrumbs.Add(new Folder { Name = "我的文件", FolderId = "0" });
            BreadcrumbBar.ItemsSource = _folderBreadcrumbs;
            if (App.from == "search")
            {
                _ = search();
            }
            else
            {
                _ = LoadFileListAsync("0");
            }
        }

        private async Task LoadFileListAsync(string parentFileId)
        {
            _currentSearchKeyword = "";
            _currentFileItems = new ObservableCollection<FileInfoModel>();
            FileListView.ItemsSource = _currentFileItems;

            try
            {
                jindu.Visibility = Visibility.Visible;
                _allFileNames.Clear();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                client.DefaultRequestHeaders.Add("Platform", "open_platform");

                var response = await client.GetAsync($"https://open-api.123pan.com/api/v2/file/list?parentFileId={parentFileId}&limit=100");
                App.ID = Convert.ToInt32(parentFileId);
                App.ID1 = parentFileId;
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
                            if (file.trashed != "1")
                            {
                                _allFileNames.Add(file.fileName);
                                _currentFileItems.Add(new FileInfoModel
                                {
                                    FileName = file.fileName,
                                    FileDate = file.updateAt,
                                    IconPath = GetIconPath(file.type, file.category),
                                    FileId = file.fileId,
                                    Type = file.type
                                });
                            }
                        }
                    }

                    shang.Visibility = Visibility.Visible;
                    add.Visibility = Visibility.Visible;
                    Down.Visibility = Visibility.Collapsed;
                    sc.Visibility = Visibility.Collapsed;
                    nodown.Visibility = Visibility.Visible;
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

        private async Task search()
        {
            _currentSearchKeyword = App.searchText;
            _currentFileItems = new ObservableCollection<FileInfoModel>();
            FileListView.ItemsSource = _currentFileItems;

            App.from = "";
            try
            {
                _allFileNames.Clear();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                client.DefaultRequestHeaders.Add("Platform", "open_platform");

                var response = await client.GetAsync("https://open-api.123pan.com/api/v2/file/list?parentFileId=0&limit=100&searchData=" + Uri.EscapeDataString(App.searchText) + "&searchMode=0");
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
                            if (file.trashed != "1")
                            {
                                _allFileNames.Add(file.fileName);
                                _currentFileItems.Add(new FileInfoModel
                                {
                                    FileName = file.fileName,
                                    FileDate = file.updateAt,
                                    IconPath = GetIconPath(file.type, file.category),
                                    FileId = file.fileId,
                                    Type = file.type
                                });
                            }
                        }
                    }

                    shang.Visibility = Visibility.Visible;
                    add.Visibility = Visibility.Visible;
                    Down.Visibility = Visibility.Collapsed;
                    sc.Visibility = Visibility.Collapsed;
                    nodown.Visibility = Visibility.Visible;
                    jindu.Visibility = Visibility.Collapsed;
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
        }


        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            var files = await picker.PickMultipleFilesAsync();

            if (files.Count > 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "准备上传",
                    Content = "正在计算文件MD5，请稍候...",
                    CloseButtonText = "取消"
                };
                var dialogTask = dialog.ShowAsync();

                try
                {
                    var uploadFiles = new ObservableCollection<FileUploadModel>();
                    var fileTasks = files.Select(async file =>
                    {
                        var fileModel = new FileUploadModel
                        {
                            FileName = file.Name,
                            FilePath = file.Path,
                            FileSize = (long)(await file.GetBasicPropertiesAsync()).Size,
                            Status = "等待中",
                            StorageFile = file
                        };
                        fileModel.MD5 = await Task.Run(() => CalculateFileMd5Async(file));
                        return fileModel;
                    });

                    var fileModels = await Task.WhenAll(fileTasks);
                    foreach (var model in fileModels)
                    {
                        uploadFiles.Add(model);
                    }

                    dialog.Hide();
                }
                catch (Exception)
                {
                    dialog.Hide();
                }
            }
        }

        private async void BtnUploadFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "准备上传",
                    Content = "正在扫描文件夹并计算文件MD5，请稍候...",
                    CloseButtonText = "取消"
                };
                var dialogTask = dialog.ShowAsync();

                try
                {
                    var uploadFiles = new ObservableCollection<FileUploadModel>();
                    var allFiles = await GetAllFilesInFolderAsync(folder);

                    var fileTasks = allFiles.Select(async file =>
                    {
                        var fileModel = new FileUploadModel
                        {
                            FileName = file.Name,
                            FilePath = file.Path,
                            FileSize = (long)(await file.GetBasicPropertiesAsync()).Size,
                            Status = "等待中",
                            StorageFile = file
                        };
                        fileModel.MD5 = await Task.Run(() => CalculateFileMd5Async(file));
                        return fileModel;
                    });

                    var fileModels = await Task.WhenAll(fileTasks);
                    foreach (var model in fileModels)
                    {
                        uploadFiles.Add(model);
                    }

                    dialog.Hide();
                    if (uploadFiles.Any())
                    {
                        
                    }
                    else
                    {
                        var emptyDialog = new ContentDialog
                        {
                            Title = "提示",
                            Content = "所选文件夹中不包含任何文件。",
                            CloseButtonText = "确定"
                        };
                        await emptyDialog.ShowAsync();
                    }
                }
                catch (Exception)
                {
                    dialog.Hide();
                }
            }
        }

        private async Task<List<StorageFile>> GetAllFilesInFolderAsync(StorageFolder folder)
        {
            var files = new List<StorageFile>();
            files.AddRange(await folder.GetFilesAsync());
            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
            {
                files.AddRange(await GetAllFilesInFolderAsync(subFolder));
            }
            return files;
        }

        private string CalculateFileMd5Async(StorageFile file)
        {
            using (var md5 = MD5.Create())
            using (var stream = file.OpenStreamForReadAsync().Result)
            {
                var hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private async void BreadcrumbBar_ItemClicked(Microsoft.UI.Xaml.Controls.BreadcrumbBar sender, Microsoft.UI.Xaml.Controls.BreadcrumbBarItemClickedEventArgs args)
        {
            var clickedFolder = args.Item as Folder;
            if (clickedFolder != null)
            {
                _currentFolderId = clickedFolder.FolderId;
                await LoadFileListAsync(_currentFolderId);
                var currentIndex = _folderBreadcrumbs.IndexOf(clickedFolder);
                while (_folderBreadcrumbs.Count > currentIndex + 1)
                {
                    _folderBreadcrumbs.RemoveAt(_folderBreadcrumbs.Count - 1);
                }
            }
        }

        private void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as FileInfoModel;
            if (clickedItem != null)
            {
                SelectedFileId = clickedItem.FileId;
                shang.Visibility = Visibility.Collapsed;
                add.Visibility = Visibility.Collapsed;
                Down.Visibility = Visibility.Visible;
                sc.Visibility = Visibility.Visible;
                nodown.Visibility = Visibility.Collapsed;
                App.File = clickedItem.FileId;
            }
        }

        private async void down_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await termsOfUseContentDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 用户点击了“取消”
            }
            else
            {
                // 用户点击了“下载”
                if (_saveFolder != null)
                {
                    await GetDownloadInfo();
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "提示",
                        Content = "请先选择一个保存文件的文件夹。",
                        CloseButtonText = "确定"
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
                folderPicker.FileTypeFilter.Add("*");

                _saveFolder = await folderPicker.PickSingleFolderAsync();
                if (_saveFolder != null)
                {
                    App.SaveFolder = _saveFolder;
                    xvan.Content = $"已选择: {_saveFolder.Name}";
                }
            }
            catch (Exception)
            {
                xvan.Content = "选择文件夹时出错，请重试";
            }
        }

        private async Task GetDownloadInfo()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", App.AccessToken);
                    string url = $"https://open-api.123pan.com/api/v1/file/download_info?fileId={App.File}";
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseBody);
                    if (json["data"] != null && json["data"]["downloadUrl"] != null)
                    {
                        App.url = json["data"]["downloadUrl"].ToString();
                        Frame.Navigate(typeof(cs));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = "下载失败：";
                if (ex.Message.Contains("403"))
                {
                    errorMessage += "可能是今天的下载流量已达上限。";
                }
                else if (ex.Message.Contains("404"))
                {
                    errorMessage += "文件未找到。";
                }
                else
                {
                    errorMessage += "网络错误或服务器问题，请稍后再试。";
                }

                var dialog = new ContentDialog
                {
                    Title = "出错了",
                    Content = errorMessage,
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
            catch (Exception)
            {
                var dialog = new ContentDialog
                {
                    Title = "出错了",
                    Content = "处理下载信息时发生未知错误。",
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
        }

        private async void add_Click(object sender, RoutedEventArgs e)
        {
            termsOfUseContentDialog1.IsSecondaryButtonEnabled = string.IsNullOrEmpty(InputTextBox.Text) ? false : true;
            ContentDialogResult result = await termsOfUseContentDialog1.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                await addInfo();
            }
        }

        private async Task addInfo()
        {
            add.IsEnabled = false;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string url = "https://open-api.123pan.com/upload/v1/file/mkdir";
                    var bodyParams = new
                    {
                        name = InputTextBox.Text,
                        parentID = App.ID
                    };
                    string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(bodyParams);
                    var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                    await LoadFileListAsync(App.ID1);
                }
            }
            catch (Exception)
            {
                var dialog = new ContentDialog
                {
                    Title = "创建失败",
                    Content = "创建文件夹失败，请稍后再试。",
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                add.IsEnabled = true;
                InputTextBox.Text = "";
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (termsOfUseContentDialog1 != null)
            {
                termsOfUseContentDialog1.IsSecondaryButtonEnabled = !string.IsNullOrWhiteSpace(InputTextBox.Text);
            }
        }

        private async void sc_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string url = $"https://open-api.123pan.com/api/v1/file/trash?fileIDs={App.File}";
                    var content = new StringContent("");
                    httpClient.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                    await LoadFileListAsync(App.ID1);
                }
            }
            catch (Exception)
            {
                var dialog = new ContentDialog
                {
                    Title = "删除失败",
                    Content = "删除文件失败，请稍后再试。",
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private string GetIconPath(string type, string category)
        {
            if (type == "1")
            {
                return "ms-appx:///Assets/文件夹.png";
            }

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

        private async void FileListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var listView = sender as ListView;
            var tappedItem = listView.SelectedItem as FileInfoModel;

            if (tappedItem != null && tappedItem.Type == "1")
            {
                _navigationHistory.Push(_currentFolderId);
                _currentFolderId = tappedItem.FileId;

                await LoadFileListAsync(_currentFolderId);

                _folderBreadcrumbs.Add(new Folder { Name = tappedItem.FileName, FolderId = tappedItem.FileId });
            }
        }
    }
}