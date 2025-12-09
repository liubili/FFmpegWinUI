using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using FFmpegWinUI.FFmpegService;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 软件设置页 - 对应VB项目的"软件设置"标签页，合并了原Settings页面
    /// </summary>
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly IFfmpegService _ffmpegService = null!;
        private CancellationTokenSource _downloadCts = null!;
        private const string FfmpegPathKey = "FfmpegBinPath";

        public SettingsPage()
        {
            try
            {
                InitializeComponent();

                // 使用App中的服务实例，如果没有就创建新实例
                _ffmpegService = App.FfmpegService ?? new FfmpegService(AppContext.BaseDirectory);
                _downloadCts = new CancellationTokenSource();

                // 异步初始化
                DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await InitializeAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Settings initialization error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings constructor error: {ex.Message}");
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                // 1. 先检查保存的路径
                LoadSavedPath();

                // 2. 如果没有保存的路径，尝试自动查找
                if (string.IsNullOrEmpty(FfmpegPathBox.Text))
                {
                    var binPath = await _ffmpegService.FindFfmpegBinPathAsync();
                    if (!string.IsNullOrEmpty(binPath))
                    {
                        FfmpegPathBox.Text = binPath;
                        await CheckFfmpegPathAndShowInfoAsync(binPath);
                    }
                }

                // 3. 检查环境变量
                if (await _ffmpegService.CheckFfmpegInPathAsync())
                {
                    StatusInfoBar.Message = "FFmpeg 已在系统 PATH 中";
                    StatusInfoBar.Severity = InfoBarSeverity.Success;
                    StatusInfoBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialize async error: {ex.Message}");
            }
        }

        private void LoadSavedPath()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                if (settings.Values.TryGetValue(FfmpegPathKey, out var pathObj) &&
                    pathObj is string path &&
                    !string.IsNullOrWhiteSpace(path))
                {
                    FfmpegPathBox.Text = path;
                    _ = CheckFfmpegPathAndShowInfoAsync(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load saved path error: {ex.Message}");
            }
        }

        private IntPtr GetWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        }

        private async void OnSelectFfmpegPath(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = GetWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                FfmpegPathBox.Text = folder.Path;
                await CheckFfmpegPathAndShowInfoAsync(folder.Path);
            }
        }

        private async Task CheckFfmpegPathAndShowInfoAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (await _ffmpegService.CheckFfmpegExistsAsync(path))
            {
                var version = await _ffmpegService.GetFfmpegVersionAsync(path);
                ShowInfo($"找到 FFmpeg: {version}", false);
                OpenFfmpegFolderBtn.Visibility = Visibility.Visible;

                // 保存有效路径
                ApplicationData.Current.LocalSettings.Values[FfmpegPathKey] = path;
            }
            else
            {
                ShowInfo("无效的 FFmpeg 路径", true);
                OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnDownloadFfmpeg(object sender, RoutedEventArgs e)
        {
            DownloadFfmpegBtn.IsEnabled = false;
            ProgressGrid.Visibility = Visibility.Visible;
            OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;

            try
            {
                var progress = new Progress<FFmpegService.DownloadProgress>(p =>
                {
                    DownloadProgressBar.Value = p.Percentage;
                    ShowInfo(p.Status, false);
                });

                var result = await _ffmpegService.DownloadAndExtractAsync(
                    RegisterToPathCheckBox.IsChecked ?? false,
                    progress,
                    _downloadCts.Token);

                if (result.success)
                {
                    FfmpegPathBox.Text = result.path;
                    OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
                    ShowInfo(result.message, false);

                    // 保存路径
                    ApplicationData.Current.LocalSettings.Values[FfmpegPathKey] = result.path;
                }
                else
                {
                    ShowInfo(result.message, true);
                }
            }
            finally
            {
                DownloadFfmpegBtn.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void OnOpenFfmpegFolder(object sender, RoutedEventArgs e)
        {
            var binPath = FfmpegPathBox.Text;
            if (Directory.Exists(binPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = binPath,
                    UseShellExecute = true
                });
            }
            else
            {
                ShowInfo("目录不存在: " + binPath, true);
            }
        }

        private async void OnWingetInstall(object sender, RoutedEventArgs e)
        {
            WingetInstallBtn.IsEnabled = false;

            try
            {
                // 检查是否安装了 winget
                var wingetCheck = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "winget",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var checkProcess = Process.Start(wingetCheck);
                await checkProcess!.WaitForExitAsync();

                if (checkProcess.ExitCode != 0)
                {
                    ShowInfo("未找到 winget，请确认已安装 Windows Package Manager", true);
                    return;
                }

                // 使用 winget 安装 FFmpeg
                ShowInfo("正在安装 FFmpeg...", false);

                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install FFmpeg",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                await process!.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    ShowInfo("FFmpeg 安装成功", false);
                    // 刷新路径检测
                    await InitializeAsync();
                }
                else
                {
                    ShowInfo("FFmpeg 安装失败", true);
                }
            }
            catch (Exception ex)
            {
                ShowInfo($"安装错误: {ex.Message}", true);
            }
            finally
            {
                WingetInstallBtn.IsEnabled = true;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // 取消可能正在运行的后台下载操作
        }

        private void ShowInfo(string message, bool isError)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
            StatusInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;
            StatusInfoBar.Title = isError ? "错误" : "FFmpeg 状态";

            // 更新主窗口的设置页 InfoBadge
            try
            {
                var mainWindow = (FFmpegWinUI.MainWindow)App.MainWindow;
                mainWindow.SetSettingsInfoBadgeVisible(isError);
            }
            catch
            {
                // 忽略主窗口更新错误
            }
        }
    }
}
