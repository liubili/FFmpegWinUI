using FFmpegWinUI.Page;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace FFmpegWinUI
{
    /// <summary>
    /// FFmpeg WinUI 主窗口
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private NavigationViewItem? SettingsNavItem;

        public MainWindow()
        {
            this.InitializeComponent();

            // 初始化 SettingsNavItem
            SettingsNavItem = FindSettingsNavItem();

            // 确保在添加事件处理程序之前 NavigationView 已经初始化
            if (nvSample != null)
            {
                nvSample.SelectionChanged += NvSample_SelectionChanged;
                // 默认导航到起始页（需要确保页面类已存在）
                contentFrame.Navigate(typeof(HomePage));
            }
        }

        // 查找 Tag="SettingsPage" 的 NavigationViewItem
        private NavigationViewItem? FindSettingsNavItem()
        {
            // 检查 FooterMenuItems
            foreach (var obj in nvSample.FooterMenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag?.ToString() == "SettingsPage"))
                    return item;
            }
            // 检查 MenuItems（如果设置放在了主菜单）
            foreach (var obj in nvSample.MenuItems)
            {
                if (obj is NavigationViewItem item && (item.Tag?.ToString() == "SettingsPage"))
                    return item;
            }
            return null;
        }

        private void NvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.SelectedItem is NavigationViewItem item)
                {
                    var pageTag = item.Tag?.ToString();
                    Type pageType = pageTag switch
                    {
                        "HomePage" => typeof(HomePage),
                        "QueuePage" => typeof(QueuePage),
                        "FilesPage" => typeof(FilesPage),
                        "ParametersPage" => typeof(ParametersPage),
                        "MediaInfoPage" => typeof(MediaInfoPage),
                        "MuxPage" => typeof(MuxPage),
                        "ConcatPage" => typeof(ConcatPage),
                        "MonitorPage" => typeof(MonitorPage),
                        "PluginsPage" => typeof(PluginsPage),
                        "SettingsPage" => typeof(SettingsPage),
                        _ => typeof(HomePage)  // 默认导航到起始页
                    };

                    // 确保 contentFrame 不为空
                    if (contentFrame != null)
                    {
                        // 使用 try-catch 包装导航操作
                        try
                        {
                            contentFrame.Navigate(pageType);
                        }
                        catch (Exception ex)
                        {
                            // 导航失败时的处理
                            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理选择改变事件中的异常
                System.Diagnostics.Debug.WriteLine($"Selection changed error: {ex.Message}");
            }
        }

        // 提供一个方法用于控制 InfoBadge 显示
        public void SetSettingsInfoBadgeVisible(bool visible)
        {
            if (SettingsNavItem != null && SettingsNavItem.InfoBadge is InfoBadge badge)
            {
                badge.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 提供给其他页面调用的导航方法
        /// </summary>
        public void NavigateToPage(string pageTag)
        {
            try
            {
                Type pageType = pageTag switch
                {
                    "HomePage" => typeof(HomePage),
                    "QueuePage" => typeof(QueuePage),
                    "FilesPage" => typeof(FilesPage),
                    "ParametersPage" => typeof(ParametersPage),
                    "MediaInfoPage" => typeof(MediaInfoPage),
                    "MuxPage" => typeof(MuxPage),
                    "ConcatPage" => typeof(ConcatPage),
                    "MonitorPage" => typeof(MonitorPage),
                    "PluginsPage" => typeof(PluginsPage),
                    "SettingsPage" => typeof(SettingsPage),
                    _ => typeof(HomePage)
                };

                if (contentFrame != null)
                {
                    contentFrame.Navigate(pageType);

                    // 更新 NavigationView 的选中项
                    foreach (var item in nvSample.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageTag)
                        {
                            nvSample.SelectedItem = navItem;
                            break;
                        }
                    }
                    foreach (var item in nvSample.FooterMenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageTag)
                        {
                            nvSample.SelectedItem = navItem;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示全局信息提示条
        /// </summary>
        public void ShowInfoBar(string title, string message, bool isError = false, int autoCloseDuration = 5000)
        {
            var infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success,
                IsOpen = true,
                IsClosable = true,
                Margin = new Thickness(12, 8, 12, 0)
            };

            InfoBarPanel.Children.Add(infoBar);

            // 自动关闭定时器
            if (autoCloseDuration > 0)
            {
                var timer = new System.Threading.Timer(_ =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        infoBar.IsOpen = false;
                        // 延迟一点再移除，让关闭动画完成
                        Task.Delay(300).ContinueWith(_ =>
                        {
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                InfoBarPanel.Children.Remove(infoBar);
                            });
                        });
                    });
                }, null, autoCloseDuration, System.Threading.Timeout.Infinite);
            }

            // InfoBar 关闭按钮点击后自动从容器中移除
            infoBar.CloseButtonClick += (s, e) =>
            {
                Task.Delay(300).ContinueWith(_ =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        InfoBarPanel.Children.Remove(infoBar);
                    });
                });
            };
        }
    }
}
