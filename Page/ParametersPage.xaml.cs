using System;
using FFmpegWinUI.ViewModels;
using FFmpegWinUI.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 参数面板页 - 对应VB项目的"参数面板"标签页
    /// </summary>
    public sealed partial class ParametersPage : Microsoft.UI.Xaml.Controls.Page
    {
        public ParametersPageViewModel ViewModel { get; }

        public ParametersPage()
        {
            this.InitializeComponent();

            // 使用服务容器中的共享 ViewModel
            ViewModel = Services.ServiceContainer.Instance.ParametersPageViewModel;
        }

        /// <summary>
        /// 打开帧插值设置窗口
        /// </summary>
        private async void OpenInterpolationWindow(object sender, RoutedEventArgs e)
        {
            var dialog = new InterpolationWindow(ViewModel.CurrentPreset)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// 打开超分辨率设置窗口
        /// </summary>
        private async void OpenUpscaleWindow(object sender, RoutedEventArgs e)
        {
            var dialog = new UpscaleWindow(ViewModel.CurrentPreset)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// 打开画面裁剪窗口
        /// </summary>
        private void OpenCropWindow(object sender, RoutedEventArgs e)
        {
            // TODO: 实现画面裁剪窗口
            ShowInfoDialog("画面裁剪", "此功能即将推出");
        }

        /// <summary>
        /// 打开帧混合设置窗口
        /// </summary>
        private void OpenBlendWindow(object sender, RoutedEventArgs e)
        {
            // TODO: 实现帧混合窗口
            ShowInfoDialog("帧混合设置", "此功能即将推出");
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        private async void ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
