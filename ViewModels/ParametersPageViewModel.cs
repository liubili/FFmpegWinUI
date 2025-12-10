using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegWinUI.Models;
using FFmpegWinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 参数面板页ViewModel - 对应VB项目的"参数面板"标签页
    /// 管理所有编码参数配置和预设
    /// </summary>
    public partial class ParametersPageViewModel : ObservableObject
    {
        private readonly IPresetService _presetService;
        private readonly DispatcherQueue _dispatcherQueue;

        // 当前预设数据
        [ObservableProperty]
        private PresetData _currentPreset = new();

        // 预设列表
        [ObservableProperty]
        private ObservableCollection<string> _presetList = new();

        // 选中的预设名称
        [ObservableProperty]
        private string? _selectedPresetName;

        // 编码器列表
        [ObservableProperty]
        private ObservableCollection<string> _encoderList = new();

        // 选中的编码器
        [ObservableProperty]
        private string _selectedEncoder = string.Empty;

        // 编码预设列表（根据编码器动态变化）
        [ObservableProperty]
        private ObservableCollection<string> _encoderPresetList = new();

        // 配置文件列表（根据编码器动态变化）
        [ObservableProperty]
        private ObservableCollection<string> _profileList = new();

        // Tune列表（根据编码器动态变化）
        [ObservableProperty]
        private ObservableCollection<string> _tuneList = new();

        // 像素格式列表（根据编码器动态变化）
        [ObservableProperty]
        private ObservableCollection<string> _pixelFormatList = new();

        // 生成的命令行预览
        [ObservableProperty]
        private string _commandLinePreview = string.Empty;

        // 是否有未保存的更改
        [ObservableProperty]
        private bool _hasUnsavedChanges;

        public ParametersPageViewModel(IPresetService presetService, DispatcherQueue dispatcherQueue)
        {
            _presetService = presetService;
            _dispatcherQueue = dispatcherQueue;

            // 初始化默认预设
            InitializeDefaultPreset();

            // 初始化编码器列表
            InitializeEncoderDatabase();

            // 加载预设列表
            _ = LoadPresetListAsync();

            // 监听编码器选择变化
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedEncoder))
                {
                    OnEncoderChanged();
                }
                else if (e.PropertyName == nameof(CurrentPreset))
                {
                    // 当整个预设对象替换时，订阅其属性变化
                    SubscribeToCurrentPresetChanges();
                }
            };

            // 订阅当前预设的属性变化
            SubscribeToCurrentPresetChanges();
        }

        /// <summary>
        /// 订阅当前预设的属性变化以自动更新命令行预览
        /// </summary>
        private void SubscribeToCurrentPresetChanges()
        {
            if (CurrentPreset != null)
            {
                CurrentPreset.PropertyChanged += (s, e) =>
                {
                    HasUnsavedChanges = true;
                    UpdateCommandLinePreview();
                };
            }
        }

        /// <summary>
        /// 初始化默认预设
        /// </summary>
        private void InitializeDefaultPreset()
        {
            // 创建空的预设对象，不预填充任何默认值
            // 用户需要手动配置所有参数
            CurrentPreset = new PresetData();
        }

        /// <summary>
        /// 初始化编码器数据库
        /// </summary>
        private void InitializeEncoderDatabase()
        {
            EncoderDatabase.Initialize();
            var encoders = EncoderDatabase.GetEncoderList();

            _dispatcherQueue.TryEnqueue(() =>
            {
                EncoderList.Clear();
                foreach (var encoder in encoders)
                {
                    EncoderList.Add(encoder);
                }
            });
        }

        /// <summary>
        /// 编码器变化时更新相关列表
        /// </summary>
        private void OnEncoderChanged()
        {
            if (string.IsNullOrEmpty(SelectedEncoder))
            {
                // 编码器为空时清空所有相关列表
                _dispatcherQueue.TryEnqueue(() =>
                {
                    EncoderPresetList.Clear();
                    ProfileList.Clear();
                    TuneList.Clear();
                    PixelFormatList.Clear();
                });
                return;
            }

            var encoderData = EncoderDatabase.GetEncoderData(SelectedEncoder);
            if (encoderData != null)
            {
                // 保存当前选择的值
                var currentPreset = CurrentPreset.VideoEncoderPreset;
                var currentProfile = CurrentPreset.VideoEncoderProfile;
                var currentTune = CurrentPreset.VideoEncoderTune;
                var currentPixFmt = CurrentPreset.PixelFormat;

                _dispatcherQueue.TryEnqueue(() =>
                {
                    // 更新编码预设列表
                    EncoderPresetList.Clear();
                    foreach (var preset in encoderData.Preset)
                    {
                        EncoderPresetList.Add(preset);
                    }

                    // 更新配置文件列表
                    ProfileList.Clear();
                    foreach (var profile in encoderData.Profile)
                    {
                        ProfileList.Add(profile);
                    }

                    // 更新Tune列表
                    TuneList.Clear();
                    foreach (var tune in encoderData.Tune)
                    {
                        TuneList.Add(tune);
                    }

                    // 更新像素格式列表
                    PixelFormatList.Clear();
                    foreach (var pixFmt in encoderData.PixFmt)
                    {
                        PixelFormatList.Add(pixFmt);
                    }

                    // 验证并恢复之前的选择（如果在新列表中存在）
                    if (!string.IsNullOrEmpty(currentPreset) && !EncoderPresetList.Contains(currentPreset))
                    {
                        CurrentPreset.VideoEncoderPreset = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(currentProfile) && !ProfileList.Contains(currentProfile))
                    {
                        CurrentPreset.VideoEncoderProfile = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(currentTune) && !TuneList.Contains(currentTune))
                    {
                        CurrentPreset.VideoEncoderTune = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(currentPixFmt) && !PixelFormatList.Contains(currentPixFmt))
                    {
                        CurrentPreset.PixelFormat = string.Empty;
                    }

                    // 如果列表中只有一个空项或第一个项为空，选择第一个非空项作为默认值
                    if (EncoderPresetList.Count > 1 && string.IsNullOrEmpty(CurrentPreset.VideoEncoderPreset))
                    {
                        CurrentPreset.VideoEncoderPreset = EncoderPresetList.FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? string.Empty;
                    }

                    if (ProfileList.Count > 1 && string.IsNullOrEmpty(CurrentPreset.VideoEncoderProfile))
                    {
                        CurrentPreset.VideoEncoderProfile = ProfileList.FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? string.Empty;
                    }
                });

                // 更新当前预设
                CurrentPreset.VideoEncoder = SelectedEncoder;
                HasUnsavedChanges = true;
                UpdateCommandLinePreview();
            }
        }

        /// <summary>
        /// 加载预设列表
        /// </summary>
        private async Task LoadPresetListAsync()
        {
            try
            {
                var presets = await _presetService.GetPresetListAsync(string.Empty);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    PresetList.Clear();
                    foreach (var preset in presets)
                    {
                        PresetList.Add(preset);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载预设列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存预设命令
        /// </summary>
        [RelayCommand]
        private async Task SavePresetAsync()
        {
            try
            {
                // 弹出对话框输入预设名称
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "保存预设",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    XamlRoot = App.MainWindow.Content.XamlRoot,
                    Content = new Microsoft.UI.Xaml.Controls.TextBox
                    {
                        PlaceholderText = "请输入预设名称",
                        Width = 300
                    }
                };

                var result = await dialog.ShowAsync();
                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    var textBox = (Microsoft.UI.Xaml.Controls.TextBox)dialog.Content;
                    var presetName = textBox.Text;

                    if (!string.IsNullOrWhiteSpace(presetName))
                    {
                        var presetDir = _presetService.GetDefaultPresetDirectory();
                        var filePath = Path.Combine(presetDir, $"{presetName}.json");

                        await _presetService.SavePresetAsync(CurrentPreset, filePath);
                        await LoadPresetListAsync();

                        HasUnsavedChanges = false;

                        // 显示成功提示
                        ShowInfoBar("预设保存成功", $"预设 '{presetName}' 已保存");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("保存失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 加载预设命令
        /// </summary>
        [RelayCommand]
        private async Task LoadPresetAsync()
        {
            if (string.IsNullOrEmpty(SelectedPresetName))
                return;

            try
            {
                var presetDir = _presetService.GetDefaultPresetDirectory();
                var filePath = Path.Combine(presetDir, $"{SelectedPresetName}.json");

                var preset = await _presetService.LoadPresetAsync(filePath);
                if (preset != null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        CurrentPreset = preset;

                        // 先设置编码器，这会触发OnEncoderChanged更新相关列表
                        if (!string.IsNullOrEmpty(preset.VideoEncoder))
                        {
                            SelectedEncoder = preset.VideoEncoder;
                        }

                        HasUnsavedChanges = false;
                        UpdateCommandLinePreview();
                    });

                    ShowInfoBar("预设加载成功", $"已加载预设 '{SelectedPresetName}'");
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("加载失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 删除预设命令
        /// </summary>
        [RelayCommand]
        private async Task DeletePresetAsync()
        {
            if (string.IsNullOrEmpty(SelectedPresetName))
                return;

            try
            {
                // 确认对话框
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "删除预设",
                    Content = $"确定要删除预设 '{SelectedPresetName}' 吗？",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    XamlRoot = App.MainWindow.Content.XamlRoot,
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    var presetDir = _presetService.GetDefaultPresetDirectory();
                    var filePath = Path.Combine(presetDir, $"{SelectedPresetName}.json");

                    await _presetService.DeletePresetAsync(filePath);
                    await LoadPresetListAsync();

                    SelectedPresetName = null;
                    ShowInfoBar("删除成功", "预设已删除");
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("删除失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 重置参数命令
        /// </summary>
        [RelayCommand]
        private void ResetParameters()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                CurrentPreset = new PresetData();
                SelectedEncoder = string.Empty;
                SelectedPresetName = null;

                // 清空所有动态列表
                EncoderPresetList.Clear();
                ProfileList.Clear();
                TuneList.Clear();
                PixelFormatList.Clear();

                HasUnsavedChanges = false;
                UpdateCommandLinePreview();
            });
        }

        /// <summary>
        /// 预览命令行命令
        /// </summary>
        [RelayCommand]
        private async Task PreviewCommandLineAsync()
        {
            UpdateCommandLinePreview();

            // 显示命令行预览对话框
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "FFmpeg 命令行预览",
                CloseButtonText = "关闭",
                XamlRoot = App.MainWindow.Content.XamlRoot,
                Content = new Microsoft.UI.Xaml.Controls.ScrollViewer
                {
                    Content = new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = CommandLinePreview,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        IsTextSelectionEnabled = true
                    },
                    MaxHeight = 400
                }
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 更新命令行预览
        /// </summary>
        private void UpdateCommandLinePreview()
        {
            try
            {
                var inputFile = "<输入文件>";
                var outputFile = "<输出文件>";
                CommandLinePreview = _presetService.GenerateCommandLine(CurrentPreset, inputFile, outputFile);
            }
            catch (Exception ex)
            {
                CommandLinePreview = $"生成命令行失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取当前预设（供其他ViewModel使用）
        /// </summary>
        public PresetData GetCurrentPreset()
        {
            return CurrentPreset;
        }

        /// <summary>
        /// 刷新预设列表命令
        /// </summary>
        [RelayCommand]
        private async Task RefreshPresetListAsync()
        {
            await LoadPresetListAsync();
            ShowInfoBar("刷新成功", "预设列表已刷新");
        }

        /// <summary>
        /// 复制命令行命令
        /// </summary>
        [RelayCommand]
        private void CopyCommandLine()
        {
            try
            {
                UpdateCommandLinePreview();
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(CommandLinePreview);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                ShowInfoBar("复制成功", "命令行已复制到剪贴板");
            }
            catch (Exception ex)
            {
                ShowInfoBar("复制失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 浏览输出目录命令
        /// </summary>
        [RelayCommand]
        private async Task BrowseOutputDirectoryAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                // TODO: 将选中的目录路径设置到预设中
                ShowInfoBar("目录已选择", folder.Path);
            }
        }

        /// <summary>
        /// 添加进阶质量控制预制项命令
        /// </summary>
        [RelayCommand]
        private void AddAdvancedQualityControl()
        {
            // TODO: 实现添加进阶质量控制预制项
            ShowInfoBar("功能提示", "此功能即将推出");
        }

        /// <summary>
        /// 添加进阶质量控制空项命令
        /// </summary>
        [RelayCommand]
        private void AddAdvancedQualityControlEmpty()
        {
            // TODO: 实现添加进阶质量控制空项
            ShowInfoBar("功能提示", "此功能即将推出");
        }

        /// <summary>
        /// 清除全部进阶质量控制命令
        /// </summary>
        [RelayCommand]
        private void ClearAdvancedQualityControl()
        {
            // TODO: 实现清除全部进阶质量控制
            ShowInfoBar("功能提示", "此功能即将推出");
        }

        /// <summary>
        /// 导出预设命令
        /// </summary>
        [RelayCommand]
        private async Task ExportPresetAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("JSON 预设文件", new List<string> { ".json" });
                picker.SuggestedFileName = SelectedPresetName ?? "新预设";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await _presetService.SavePresetAsync(CurrentPreset, file.Path);
                    ShowInfoBar("导出成功", $"预设已导出到 {file.Path}");
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("导出失败", ex.Message, true);
            }
        }

        /// <summary>
        /// 导入预设命令
        /// </summary>
        [RelayCommand]
        private async Task ImportPresetAsync()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".json");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var preset = await _presetService.LoadPresetAsync(file.Path);
                    if (preset != null)
                    {
                        var presetDir = _presetService.GetDefaultPresetDirectory();
                        var destPath = Path.Combine(presetDir, file.Name);
                        await _presetService.SavePresetAsync(preset, destPath);
                        await LoadPresetListAsync();
                        ShowInfoBar("导入成功", $"预设已导入: {file.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("导入失败", ex.Message, true);
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
