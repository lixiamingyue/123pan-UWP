using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace _123云盘UWP
{
    public enum ThemeType
    {
        Light,
        Dark,
        System
    }

    public sealed partial class Setting : Page
    {
        private const string ThemeSettingKey = "AppThemePreference";
        private UISettings _uiSettings;

        public Setting()
        {
            this.InitializeComponent();
            InitializeThemeSystem();
            LoadSavedTheme();
        }

        // 初始化主题系统和系统主题监听
        private void InitializeThemeSystem()
        {
            _uiSettings = new UISettings();
            _uiSettings.ColorValuesChanged += OnSystemThemeChanged;
        }

        // 加载本地保存的主题设置
        private void LoadSavedTheme()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            
            // 读取保存的设置，默认使用系统主题
            if (localSettings.Values.TryGetValue(ThemeSettingKey, out var savedTheme))
            {
                switch (savedTheme.ToString())
                {
                    case "Light":
                        LightRadio.IsChecked = true;
                        ApplyTheme(ElementTheme.Light);
                        break;
                    case "Dark":
                        DarkRadio.IsChecked = true;
                        ApplyTheme(ElementTheme.Dark);
                        break;
                    default:
                        SystemRadio.IsChecked = true;
                        ApplySystemTheme();
                        break;
                }
            }
            else
            {
                // 首次启动，默认使用系统主题
                SystemRadio.IsChecked = true;
                ApplySystemTheme();
            }
        }

        // 单选框点击事件处理
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                switch (radioButton.Name)
                {
                    case nameof(LightRadio):
                        SaveThemeSetting("Light");
                        ApplyTheme(ElementTheme.Light);
                        break;
                    case nameof(DarkRadio):
                        SaveThemeSetting("Dark");
                        ApplyTheme(ElementTheme.Dark);
                        break;
                    case nameof(SystemRadio):
                        SaveThemeSetting("System");
                        ApplySystemTheme();
                        break;
                }
            }
        }

        // 保存主题设置到本地存储
        private void SaveThemeSetting(string theme)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[ThemeSettingKey] = theme;
        }

        // 应用指定的主题
        private void ApplyTheme(ElementTheme theme)
        {
            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }

        // 应用系统主题
        private void ApplySystemTheme()
        {
            var systemTheme = GetSystemTheme();
            ApplyTheme(systemTheme);
        }

        // 获取当前系统主题
        private ElementTheme GetSystemTheme()
        {
            // 通过系统背景色判断主题
            var backgroundColor = _uiSettings.GetColorValue(UIColorType.Background);
            return (backgroundColor.R + backgroundColor.G + backgroundColor.B) < 128 
                ? ElementTheme.Dark 
                : ElementTheme.Light;
        }

        // 系统主题变化时触发
        private void OnSystemThemeChanged(UISettings sender, object args)
        {
            // 如果当前设置为跟随系统，则更新主题
            if (SystemRadio.IsChecked == true)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, 
                    ApplySystemTheme
                );
            }
        }

        // 应用启动时加载主题设置（供App.xaml.cs调用）
        public static void ApplySavedThemeOnStartup()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ThemeSettingKey, out var savedTheme))
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    switch (savedTheme.ToString())
                    {
                        case "Light":
                            rootElement.RequestedTheme = ElementTheme.Light;
                            break;
                        case "Dark":
                            rootElement.RequestedTheme = ElementTheme.Dark;
                            break;
                        default:
                            // 系统主题，不需要额外设置，使用默认值
                            break;
                    }
                }
            }
        }
    }
}
