using FFmpegWinUI.ViewModels;
using FFmpegWinUI.Services;
using Microsoft.UI.Xaml.Controls;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 编码队列页 - 对应VB项目的"编码队列"标签页
    /// </summary>
    public sealed partial class QueuePage : Microsoft.UI.Xaml.Controls.Page
    {
        public QueuePageViewModel ViewModel { get; }

        public QueuePage()
        {
            this.InitializeComponent();

            // 使用服务容器中的共享 ViewModel
            ViewModel = Services.ServiceContainer.Instance.QueuePageViewModel;
        }
    }
}
