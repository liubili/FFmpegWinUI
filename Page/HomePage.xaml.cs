using FFmpegWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 起始页 - 对应VB项目的"3FUI"标签页
    /// </summary>
    public sealed partial class HomePage : Microsoft.UI.Xaml.Controls.Page
    {
        public HomePageViewModel ViewModel { get; }

        public HomePage()
        {
            this.InitializeComponent();

            // 使用服务容器中的共享 ViewModel
            ViewModel = Services.ServiceContainer.Instance.HomePageViewModel;

            // 定期更新统计信息
            this.Loaded += (s, e) =>
            {
                ViewModel?.UpdateStatistics();
            };
        }

        private void OnAddFilesClicked(object sender, PointerRoutedEventArgs e)
        {
            // 导航到准备文件页面
            var mainWindow = (MainWindow)App.MainWindow;
            mainWindow.NavigateToPage("FilesPage");
        }

        private void OnViewQueueClicked(object sender, PointerRoutedEventArgs e)
        {
            // 导航到编码队列页面
            var mainWindow = (MainWindow)App.MainWindow;
            mainWindow.NavigateToPage("QueuePage");
        }

        private void OnMediaInfoClicked(object sender, PointerRoutedEventArgs e)
        {
            // 导航到媒体信息页面
            var mainWindow = (MainWindow)App.MainWindow;
            mainWindow.NavigateToPage("MediaInfoPage");
        }

        private void OnSettingsClicked(object sender, PointerRoutedEventArgs e)
        {
            // 导航到设置页面
            var mainWindow = (MainWindow)App.MainWindow;
            mainWindow.NavigateToPage("SettingsPage");
        }
    }
}
