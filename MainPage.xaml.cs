using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Devices.Enumeration;
using System.Threading;
using Windows.UI.Xaml.Documents;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace _123云盘UWP
{
    public sealed partial class MainPage : Page
    {
        private Windows.Web.Http.HttpClient _httpClient;
        private ContentDialog loginDialog;
        private string _accessToken;
        private string _accessToken1;

        public MainPage()
        {
            this.InitializeComponent();
            InitializeHttpClient();
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(AppTitleBar);
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            LoadAccessToken();
            if (!string.IsNullOrEmpty(_accessToken))
            {
                NavigateToHomePage();
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new Windows.Web.Http.HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new HttpMediaTypeWithQualityHeaderValue("application/json"));
        }

        private void LoadAccessToken()
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("AccessToken", out object tokenValue))
            {
                _accessToken = tokenValue as string;
                App.AccessToken = _accessToken;
            }
            else
            {
                _accessToken = null;
            }
        }

        private void NavigateToHomePage()
        {
            if (Frame != null && Frame.CurrentSourcePageType != typeof(home))
            {
                Frame.Navigate(typeof(home), _accessToken);
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                Title = "抱歉，此功能正在开发",
                Content = "未来更新中将包含此功能",
                CloseButtonText = "确定",
                XamlRoot = Window.Current.Content.XamlRoot
            };
            await contentDialog.ShowAsync();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (LoginDialog == null)
            {
                LoginDialog = new ContentDialog
                {
                    Title = "登录你的 123云盘 开发者账户",
                    PrimaryButtonText = "确定",
                    SecondaryButtonText = "取消",
                    XamlRoot = Window.Current.Content.XamlRoot
                };
                LoginDialog.PrimaryButtonClick += LoginDialog_PrimaryButtonClick;
            }

            ClientIDTextBox.Password = string.Empty;
            ClientSecretTextBox.Password = string.Empty;
            LoginProgressRing.IsActive = false;
            LoadingText.Visibility = Visibility.Collapsed;
            await LoginDialog.ShowAsync();
        }

        private void LoginDialog1_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private async void LoginDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_httpClient == null)
            {
                await new MessageDialog("网络连接初始化失败，请重试").ShowAsync();
                args.Cancel = false;
                return;
            }

            string clientId = ClientIDTextBox.Password;
            string clientSecret = ClientSecretTextBox.Password;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                await new MessageDialog("请输入Client ID和Client Secret").ShowAsync();
                args.Cancel = true;
                return;
            }

            LoginProgressRing.IsActive = true;
            LoadingText.Visibility = Visibility.Visible;
            args.Cancel = true;

            await LoginAsync(clientId, clientSecret);
        }

        private async Task LoginAsync(string clientId, string clientSecret)
        {
            try
            {
                var requestData = new
                {
                    clientID = clientId,
                    clientSecret = clientSecret
                };

                string jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new HttpStringContent(jsonContent);
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                var request = new Windows.Web.Http.HttpRequestMessage(
                    Windows.Web.Http.HttpMethod.Post,
                    new Uri("https://open-api.123pan.com/api/v1/access_token")
                );
                request.Headers.Add("Platform", "open_platform");
                request.Content = content;
                if (_httpClient == null)
                {
                    InitializeHttpClient();
                }

                var response = await _httpClient.SendRequestAsync(request);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                if (result.Code == 0 && result.Data != null)
                {
                    ApplicationData.Current.LocalSettings.Values["AccessToken"] = result.Data.AccessToken;
                    LoginDialog.Hide();
                    await new MessageDialog("登录成功！").ShowAsync();
                    App.AccessToken = result.Data.AccessToken;

                    if (Frame != null)
                    {
                        Frame.Navigate(typeof(home), result.Data.AccessToken);
                    }
                }
                else if (result.Code == 1)
                {
                    await new MessageDialog("无效的登录信息").ShowAsync();
                }
                else
                {
                    await new MessageDialog($"登录失败: {result?.Message ?? "未知错误"}").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"请求发生错误: {ex.Message}").ShowAsync();
            }
            finally
            {
                LoginProgressRing.IsActive = false;
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }

        private async void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (sender.NavigateUri != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(sender.NavigateUri);
            }
        }

        private class LoginResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public LoginData Data { get; set; }
        }

        private class LoginData
        {
            [JsonProperty("accessToken")]
            public string AccessToken { get; set; }
        }
    }
}
