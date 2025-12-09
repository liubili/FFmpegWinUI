using FFmpegWinUI.ViewModels;
using FFmpegWinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 准备文件页 - 对应VB项目的"准备文件"标签页
    /// </summary>
    public sealed partial class FilesPage : Microsoft.UI.Xaml.Controls.Page
    {
        public FilesPageViewModel ViewModel { get; }

        public FilesPage()
        {
            this.InitializeComponent();

            // 使用服务容器中的共享 ViewModel
            ViewModel = Services.ServiceContainer.Instance.FilesPageViewModel;
        }

        private async void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖放文件到此处";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                await ViewModel.HandleDroppedFilesAsync(items);
            }
        }
    }
}
