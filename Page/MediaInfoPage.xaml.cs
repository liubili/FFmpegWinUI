using FFmpegWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 媒体信息页 - 对应VB项目的"媒体信息"标签页
    /// </summary>
    public sealed partial class MediaInfoPage : Microsoft.UI.Xaml.Controls.Page
    {
        public MediaInfoPageViewModel ViewModel { get; }

        public MediaInfoPage()
        {
            this.InitializeComponent();

            // 使用服务容器中的共享 ViewModel
            ViewModel = Services.ServiceContainer.Instance.MediaInfoPageViewModel;
        }

        private async void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖放媒体文件到此处";
            e.DragUIOverride.IsCaptionVisible = true;
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file)
                {
                    await ViewModel.HandleDroppedFileAsync(file.Path);
                }
            }
        }
    }
}
