using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegWinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 媒体信息页ViewModel - 对应VB项目的"媒体信息"标签页
    /// </summary>
    public partial class MediaInfoPageViewModel : ObservableObject
    {
        private readonly IMediaInfoService _mediaInfoService;
        private readonly DispatcherQueue _dispatcherQueue;

        // 媒体信息文本
        [ObservableProperty]
        private string _mediaInfoText = "拖放媒体文件到此处，或使用下方按钮选择文件...";

        // 当前文件路径
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        // 是否正在加载
        [ObservableProperty]
        private bool _isLoading = false;

        public MediaInfoPageViewModel(IMediaInfoService mediaInfoService, DispatcherQueue dispatcherQueue)
        {
            _mediaInfoService = mediaInfoService;
            _dispatcherQueue = dispatcherQueue;
        }

        /// <summary>
        /// 选择文件命令
        /// </summary>
        [RelayCommand]
        private async Task SelectFileAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    await LoadMediaInfoAsync(file.Path);
                }
            }
            catch (Exception ex)
            {
                MediaInfoText = $"选择文件失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理拖放文件
        /// </summary>
        public async Task HandleDroppedFileAsync(string filePath)
        {
            await LoadMediaInfoAsync(filePath);
        }

        /// <summary>
        /// 加载媒体信息
        /// </summary>
        private async Task LoadMediaInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MediaInfoText = "文件不存在！";
                return;
            }

            CurrentFilePath = filePath;
            IsLoading = true;
            MediaInfoText = "正在加载媒体信息...";

            try
            {
                var info = await _mediaInfoService.GetMediaInfoAsync(filePath);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    MediaInfoText = FormatMediaInfo(info, filePath);
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    MediaInfoText = $"获取媒体信息失败:\n{ex.Message}";
                    IsLoading = false;
                });
            }
        }

        /// <summary>
        /// 格式化媒体信息
        /// </summary>
        private string FormatMediaInfo(string rawInfo, string filePath)
        {
            if (string.IsNullOrEmpty(rawInfo))
            {
                return "无法获取媒体信息";
            }

            var fileInfo = new FileInfo(filePath);
            var header = $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"文件: {fileInfo.Name}\n" +
                        $"路径: {filePath}\n" +
                        $"大小: {FormatFileSize(fileInfo.Length)}\n" +
                        $"修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            return header + rawInfo;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / 1024.0 / 1024 / 1024:F2} GB";
            else if (bytes >= 1024 * 1024)
                return $"{bytes / 1024.0 / 1024:F2} MB";
            else if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes} B";
        }

        /// <summary>
        /// 复制信息命令
        /// </summary>
        [RelayCommand]
        private void CopyInfo()
        {
            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(MediaInfoText);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                ShowInfoBar("复制成功", "媒体信息已复制到剪贴板");
            }
            catch (Exception ex)
            {
                ShowInfoBar("复制失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 导出到文件命令
        /// </summary>
        [RelayCommand]
        private async Task ExportToFileAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("文本文件", new System.Collections.Generic.List<string> { ".txt" });
                picker.SuggestedFileName = "媒体信息";

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await File.WriteAllTextAsync(file.Path, MediaInfoText);
                    ShowInfoBar("导出成功", $"已导出到 {file.Path}");
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("导出失败", ex.Message, true);
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
    }
}
