using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegWinUI.Models;
using Microsoft.UI.Dispatching;
using System;
using System.IO;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 设置页ViewModel - 对应VB项目的"软件设置"
    /// </summary>
    public partial class SettingsPageViewModel : ObservableObject
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly string _settingsFilePath;

        // 用户设置
        [ObservableProperty]
        private UserSettings _currentSettings;

        public SettingsPageViewModel(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

            // 加载或创建默认设置
            _currentSettings = LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private UserSettings LoadSettings()
        {
            try
            {
                return UserSettings.LoadFromFile(_settingsFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                return new UserSettings();
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                CurrentSettings.SaveToFile(_settingsFilePath);
                ShowInfoBar("设置已保存", "设置已成功保存到本地");
            }
            catch (Exception ex)
            {
                ShowInfoBar("保存失败", $"保存设置时出错: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 重置所有设置
        /// </summary>
        [RelayCommand]
        private void ResetSettings()
        {
            CurrentSettings = new UserSettings();
            SaveSettings();
        }

        /// <summary>
        /// 选择默认输出路径
        /// </summary>
        [RelayCommand]
        private async System.Threading.Tasks.Task SelectDefaultOutputPathAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add("*");

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    CurrentSettings.默认输出路径 = folder.Path;
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("选择路径失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme(string theme)
        {
            CurrentSettings.主题设置 = theme;
            SaveSettings();

            // 应用主题到当前窗口
            if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme switch
                {
                    "Light" => Microsoft.UI.Xaml.ElementTheme.Light,
                    "Dark" => Microsoft.UI.Xaml.ElementTheme.Dark,
                    _ => Microsoft.UI.Xaml.ElementTheme.Default
                };
            }
        }

        /// <summary>
        /// 显示信息栏
        /// </summary>
        private void ShowInfoBar(string title, string message, bool isError = false)
        {
            System.Diagnostics.Debug.WriteLine($"[{(isError ? "错误" : "信息")}] {title}: {message}");

            _dispatcherQueue.TryEnqueue(() =>
            {
                (App.MainWindow as MainWindow)?.ShowInfoBar(title, message, isError);
            });
        }

        /// <summary>
        /// 当设置改变时自动保存
        /// </summary>
        partial void OnCurrentSettingsChanged(UserSettings value)
        {
            // 可以在此处实现自动保存逻辑
        }
    }
}
