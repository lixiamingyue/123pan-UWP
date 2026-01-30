using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace _123云盘UWP
{
    public sealed partial class home : Page
    {
        public home()
        {
            this.InitializeComponent();
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(AppTitleBar);
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MySearchBox.QuerySubmitted += SearchBox_QuerySubmitted;
        }

        private async Task GetUserInfoAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", App.AccessToken);
                    httpClient.DefaultRequestHeaders.Add("Platform", "open_platform");
                    var response = await httpClient.GetAsync(new Uri("https://open-api.123pan.com/api/v1/user/info"));
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseContent);
                    if (jsonObject["data"] != null && jsonObject["data"]["nickname"] != null)
                    {
                        string nickname = jsonObject["data"]["nickname"].ToString();
                        Contact.Content = nickname;
                    }
                    else
                    {
                        Contact.Content = "个人中心";
                    }
                }
            }
            catch (HttpRequestException)
            {
                Contact.Content = "个人中心";
            }
            catch (Exception)
            {
                Contact.Content = "个人中心";
            }
        }

        private void NavigationView_ItemInvoked(global::Microsoft.UI.Xaml.Controls.NavigationView sender, global::Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer;
            switch (item.Name)
            {
                case "all":
                    ContentFrame.Navigate(typeof(all));
                    break;
                case "down":
                    ContentFrame.Navigate(typeof(cs));
                    break;
                case "Setting":
                    ContentFrame.Navigate(typeof(Setting));
                    break;
                case "VIP":
                    ContentFrame.Navigate(typeof(VIP));
                    break;
                case "hsz":
                    ContentFrame.Navigate(typeof(hsz));
                    break;
                case "up":
                    ContentFrame.Navigate(typeof(UploadPage));
                    break;
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (MySearchBox.Text != null)
            {
                App.searchText = MySearchBox.Text;
                App.from = "search";
                ContentFrame.Navigate(typeof(all));
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await GetUserInfoAsync();
            ContentFrame.Navigate(typeof(all));
            base.OnNavigatedTo(e);
            navigationView.SelectedItem = all;
        }
    }
}
