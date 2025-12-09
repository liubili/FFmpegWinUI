using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegWinUI.Models;
using FFmpegWinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 文件信息项
    /// </summary>
    public partial class FileItem : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _fileSize = string.Empty;

        [ObservableProperty]
        private string _format = string.Empty;

        [ObservableProperty]
        private string _duration = string.Empty;

        public long FileSizeBytes { get; set; }
    }

    /// <summary>
    /// 准备文件页ViewModel - 对应VB项目的"准备文件"标签页
    /// </summary>
    public partial class FilesPageViewModel : ObservableObject
    {
        private readonly IMediaInfoService _mediaInfoService;
        private readonly DispatcherQueue _dispatcherQueue;

        // 文件列表
        [ObservableProperty]
        private ObservableCollection<FileItem> _files = new();

        // 选中的文件
        [ObservableProperty]
        private FileItem? _selectedFile;

        // 是否显示拖放提示
        [ObservableProperty]
        private bool _showDropHint = true;

        // 统计信息
        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private string _totalSize = "0 MB";

        // 默认输出路径
        [ObservableProperty]
        private string _defaultOutputPath = string.Empty;

        public FilesPageViewModel(IMediaInfoService mediaInfoService, DispatcherQueue dispatcherQueue)
        {
            _mediaInfoService = mediaInfoService;
            _dispatcherQueue = dispatcherQueue;

            // 监听文件列表变化
            Files.CollectionChanged += (s, e) =>
            {
                ShowDropHint = Files.Count == 0;
                UpdateStatistics();
            };
        }

        /// <summary>
        /// 添加文件命令
        /// </summary>
        [RelayCommand]
        private async Task AddFileAsync()
        {
            // 使用文件选择器
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
                    await AddFileInternalAsync(file.Path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加文件夹命令
        /// </summary>
        [RelayCommand]
        private async Task AddFolderAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    // 扫描文件夹中的媒体文件
                    var files = Directory.GetFiles(folder.Path, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsMediaFile(f))
                        .ToList();

                    foreach (var file in files)
                    {
                        await AddFileInternalAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加文件夹失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理拖放文件
        /// </summary>
        public async Task HandleDroppedFilesAsync(System.Collections.Generic.IReadOnlyList<Windows.Storage.IStorageItem> items)
        {
            foreach (var item in items)
            {
                if (item is Windows.Storage.StorageFile file)
                {
                    await AddFileInternalAsync(file.Path);
                }
                else if (item is Windows.Storage.StorageFolder folder)
                {
                    // 扫描文件夹
                    var files = Directory.GetFiles(folder.Path, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsMediaFile(f))
                        .ToList();

                    foreach (var filePath in files)
                    {
                        await AddFileInternalAsync(filePath);
                    }
                }
            }
        }

        /// <summary>
        /// 内部添加文件方法
        /// </summary>
        private async Task AddFileInternalAsync(string filePath)
        {
            try
            {
                // 检查是否已存在
                if (Files.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return;
                }

                var fileItem = new FileItem
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSizeBytes = fileInfo.Length,
                    FileSize = FormatFileSize(fileInfo.Length),
                    Format = fileInfo.Extension.TrimStart('.')
                };

                // 异步获取时长
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var duration = await _mediaInfoService.GetDurationAsync(filePath);
                        if (duration.HasValue)
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                fileItem.Duration = duration.Value.ToString(@"hh\:mm\:ss");
                            });
                        }
                    }
                    catch { }
                });

                _dispatcherQueue.TryEnqueue(() =>
                {
                    Files.Add(fileItem);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除选中文件命令
        /// </summary>
        [RelayCommand]
        private void RemoveSelectedFile()
        {
            if (SelectedFile != null)
            {
                Files.Remove(SelectedFile);
                SelectedFile = null;
            }
        }

        /// <summary>
        /// 清空列表命令
        /// </summary>
        [RelayCommand]
        private void ClearFiles()
        {
            Files.Clear();
            SelectedFile = null;
        }

        /// <summary>
        /// 添加到队列命令 - 将文件添加到编码队列
        /// </summary>
        [RelayCommand]
        private void AddToQueue()
        {
            if (Files.Count == 0)
            {
                ShowInfoBar("没有文件", "请先添加要处理的文件", true);
                return;
            }

            // 从服务容器获取参数页面的当前预设
            var parametersViewModel = Services.ServiceContainer.Instance.ParametersPageViewModel;
            var currentPreset = parametersViewModel.GetCurrentPreset();

            // 验证预设是否有效
            if (string.IsNullOrEmpty(currentPreset.VideoEncoder) &&
                string.IsNullOrEmpty(currentPreset.AudioEncoder))
            {
                ShowInfoBar("预设未配置", "请先在参数面板配置编码参数", true);
                return;
            }

            // 创建编码任务列表
            var tasks = Files.Select(file => new EncodingTask
            {
                InputFile = file.FilePath,
                InputFileSize = file.FileSizeBytes,
                PresetData = currentPreset, // 使用参数页面的当前预设
                CustomOutputPath = DefaultOutputPath // 使用用户选择的输出路径
            }).ToList();

            // 添加到队列
            var queueViewModel = Services.ServiceContainer.Instance.QueuePageViewModel;
            queueViewModel.AddTasks(tasks);

            ShowInfoBar("添加成功", $"已将 {tasks.Count} 个文件添加到编码队列", false);

            // 自动导航到队列页面
            _dispatcherQueue.TryEnqueue(() =>
            {
                (App.MainWindow as MainWindow)?.NavigateToPage("QueuePage");
            });
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TotalFiles = Files.Count;
            var totalBytes = Files.Sum(f => f.FileSizeBytes);
            TotalSize = FormatFileSize(totalBytes);
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
        /// 判断是否为媒体文件
        /// </summary>
        private bool IsMediaFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var mediaExtensions = new[]
            {
                ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v",
                ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".wma",
                ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"
            };

            return mediaExtensions.Contains(ext);
        }

        /// <summary>
        /// 选择输出路径命令
        /// </summary>
        [RelayCommand]
        private async Task SelectOutputPathAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    DefaultOutputPath = folder.Path;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"选择输出路径失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示信息提示条
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
