using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegWinUI.Models;
using FFmpegWinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 编码队列页ViewModel - 对应VB项目的编码队列管理
    /// </summary>
    public partial class QueuePageViewModel : ObservableObject
    {
        private readonly IEncodingTaskService _encodingService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        // 任务列表
        [ObservableProperty]
        private ObservableCollection<EncodingTask> _tasks = new();

        // 选中的任务
        [ObservableProperty]
        private EncodingTask? _selectedTask;

        // 输出日志
        [ObservableProperty]
        private string _outputLog = string.Empty;

        // 统计信息
        [ObservableProperty]
        private int _totalTasks;

        [ObservableProperty]
        private int _runningTasks;

        [ObservableProperty]
        private int _completedTasks;

        [ObservableProperty]
        private int _errorTasks;

        // 按钮可用状态
        [ObservableProperty]
        private bool _canStart;

        [ObservableProperty]
        private bool _canPause;

        [ObservableProperty]
        private bool _canResume;

        [ObservableProperty]
        private bool _canStop;

        // 实时信息面板属性
        [ObservableProperty]
        private string _currentTaskStatus = "--";

        [ObservableProperty]
        private string _currentTaskProgress = "--";

        [ObservableProperty]
        private string _currentTaskFPS = "--";

        [ObservableProperty]
        private string _currentTaskSize = "--";

        [ObservableProperty]
        private string _currentTaskQuality = "--";

        [ObservableProperty]
        private string _currentTaskBitrate = "--";

        [ObservableProperty]
        private string _currentTaskETA = "--";

        public QueuePageViewModel(IEncodingTaskService encodingService, DispatcherQueue dispatcherQueue)
        {
            _encodingService = encodingService;
            _dispatcherQueue = dispatcherQueue;

            // 初始化定时器，每秒刷新一次
            _refreshTimer = _dispatcherQueue.CreateTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            // 订阅服务事件
            _encodingService.TaskStatusChanged += OnTaskStatusChanged;
            _encodingService.TaskProgressUpdated += OnTaskProgressUpdated;
            _encodingService.TaskOutputReceived += OnTaskOutputReceived;

            // 监听选中任务变化
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedTask))
                {
                    UpdateButtonStates();
                    UpdateCurrentTaskInfo();
                }
            };
        }

        /// <summary>
        /// 定时器Tick事件 - 定期刷新计算属性
        /// </summary>
        private void OnRefreshTimerTick(DispatcherQueueTimer sender, object args)
        {
            try
            {
                // 刷新所有正在处理的任务的计算属性
                var processingTasks = Tasks.Where(t => t.Status == EncodingStatus.Processing).ToList();
                foreach (var task in processingTasks)
                {
                    // 触发计算属性更新
                    task.RefreshCalculatedProperties();
                }

                // 更新实时信息面板
                UpdateCurrentTaskInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"定时器刷新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加任务到队列
        /// </summary>
        public void AddTask(EncodingTask task)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Tasks.Add(task);
                UpdateStatistics();
            });
        }

        /// <summary>
        /// 批量添加任务
        /// </summary>
        public void AddTasks(System.Collections.Generic.IEnumerable<EncodingTask> tasks)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
                UpdateStatistics();
            });
        }

        /// <summary>
        /// 开始任务命令
        /// </summary>
        [RelayCommand]
        private async Task StartTaskAsync()
        {
            if (SelectedTask != null && SelectedTask.Status == EncodingStatus.Pending)
            {
                await _encodingService.StartTaskAsync(SelectedTask);
            }
        }

        /// <summary>
        /// 开始所有待处理任务命令
        /// </summary>
        [RelayCommand]
        private async Task StartAllTasksAsync()
        {
            if (Tasks.Count == 0)
            {
                ShowInfoBar("队列为空", "请先添加要处理的文件", true);
                return;
            }

            var pendingTasks = Tasks.Where(t => t.Status == EncodingStatus.Pending).ToList();

            if (pendingTasks.Count == 0)
            {
                ShowInfoBar("没有待处理任务", "所有任务已完成或正在处理中", false);
                return;
            }

            // 根据并发限制启动任务
            var limit = _encodingService.GetConcurrentTaskLimit();
            var tasksToStart = pendingTasks.Take(limit);

            foreach (var task in tasksToStart)
            {
                await _encodingService.StartTaskAsync(task);
            }

            ShowInfoBar("任务已启动", $"开始处理 {tasksToStart.Count()} 个任务", false);
        }

        /// <summary>
        /// 暂停任务命令
        /// </summary>
        [RelayCommand]
        private void PauseTask()
        {
            if (SelectedTask != null && SelectedTask.Status == EncodingStatus.Processing)
            {
                _encodingService.PauseTask(SelectedTask);
            }
        }

        /// <summary>
        /// 恢复任务命令
        /// </summary>
        [RelayCommand]
        private void ResumeTask()
        {
            if (SelectedTask != null && SelectedTask.Status == EncodingStatus.Paused)
            {
                _encodingService.ResumeTask(SelectedTask);
            }
        }

        /// <summary>
        /// 停止任务命令
        /// </summary>
        [RelayCommand]
        private void StopTask()
        {
            if (SelectedTask != null &&
                (SelectedTask.Status == EncodingStatus.Processing || SelectedTask.Status == EncodingStatus.Paused))
            {
                _encodingService.StopTask(SelectedTask);
            }
        }

        /// <summary>
        /// 停止所有任务命令
        /// </summary>
        [RelayCommand]
        private void StopAllTasks()
        {
            var runningTasks = Tasks.Where(t =>
                t.Status == EncodingStatus.Processing || t.Status == EncodingStatus.Paused).ToList();

            foreach (var task in runningTasks)
            {
                _encodingService.StopTask(task);
            }
        }

        /// <summary>
        /// 移除任务命令
        /// </summary>
        [RelayCommand]
        private void RemoveTask()
        {
            if (SelectedTask != null)
            {
                _encodingService.RemoveTask(SelectedTask);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Tasks.Remove(SelectedTask);
                    SelectedTask = null;
                    UpdateStatistics();
                });
            }
        }

        /// <summary>
        /// 移除已完成任务命令
        /// </summary>
        [RelayCommand]
        private void RemoveCompletedTasks()
        {
            var completedTasks = Tasks.Where(t => t.Status == EncodingStatus.Completed).ToList();

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var task in completedTasks)
                {
                    _encodingService.RemoveTask(task);
                    Tasks.Remove(task);
                }
                UpdateStatistics();
            });
        }

        /// <summary>
        /// 清空队列命令
        /// </summary>
        [RelayCommand]
        private async Task ClearQueueAsync()
        {
            // 先停止所有正在运行的任务
            StopAllTasks();

            // 等待一小段时间确保任务已停止
            await Task.Delay(500);

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var task in Tasks.ToList())
                {
                    _encodingService.RemoveTask(task);
                }
                Tasks.Clear();
                SelectedTask = null;
                UpdateStatistics();
            });
        }

        /// <summary>
        /// 重置任务命令（将已停止/错误的任务重置为未处理）
        /// </summary>
        [RelayCommand]
        private void ResetTask()
        {
            if (SelectedTask != null &&
                (SelectedTask.Status == EncodingStatus.Stopped || SelectedTask.Status == EncodingStatus.Error))
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    SelectedTask.Status = EncodingStatus.Pending;
                    SelectedTask.ClearProgress();
                    UpdateStatistics();
                });
            }
        }

        /// <summary>
        /// 任务状态变化事件处理
        /// </summary>
        private void OnTaskStatusChanged(object? sender, EncodingTask task)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateStatistics();
                UpdateButtonStates();

                // 任务完成或出错时的通知
                if (task.Status == EncodingStatus.Completed)
                {
                    var inputFileName = System.IO.Path.GetFileName(task.InputFile);
                    ShowInfoBar("编码完成", $"{inputFileName} 已成功编码", false);
                    AutoStartNextTask();
                }
                else if (task.Status == EncodingStatus.Error)
                {
                    var inputFileName = System.IO.Path.GetFileName(task.InputFile);
                    ShowInfoBar("编码失败", $"{inputFileName} 编码出错", true);
                    AutoStartNextTask();
                }
            });
        }

        /// <summary>
        /// 任务进度更新事件处理
        /// </summary>
        private void OnTaskProgressUpdated(object? sender, EncodingTask task)
        {
            // 进度更新频率较高，这里可以做节流处理
            // 暂时不需要额外操作，因为EncodingTask本身是ObservableObject
            // WinUI会自动更新绑定的UI
        }

        /// <summary>
        /// 任务输出接收事件处理
        /// </summary>
        private void OnTaskOutputReceived(object? sender, string output)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // 限制日志长度
                if (OutputLog.Length > 50000)
                {
                    OutputLog = OutputLog.Substring(OutputLog.Length - 25000);
                }
                OutputLog += output + "\n";
            });
        }

        /// <summary>
        /// 自动启动下一个任务
        /// </summary>
        private async void AutoStartNextTask()
        {
            var runningCount = Tasks.Count(t => t.Status == EncodingStatus.Processing);
            var limit = _encodingService.GetConcurrentTaskLimit();

            if (runningCount < limit)
            {
                var nextTask = Tasks.FirstOrDefault(t => t.Status == EncodingStatus.Pending);
                if (nextTask != null)
                {
                    await _encodingService.StartTaskAsync(nextTask);
                }
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TotalTasks = Tasks.Count;
            RunningTasks = Tasks.Count(t => t.Status == EncodingStatus.Processing);
            CompletedTasks = Tasks.Count(t => t.Status == EncodingStatus.Completed);
            ErrorTasks = Tasks.Count(t => t.Status == EncodingStatus.Error);
        }

        /// <summary>
        /// 更新按钮可用状态
        /// </summary>
        private void UpdateButtonStates()
        {
            if (SelectedTask == null)
            {
                CanStart = false;
                CanPause = false;
                CanResume = false;
                CanStop = false;
            }
            else
            {
                CanStart = SelectedTask.Status == EncodingStatus.Pending;
                CanPause = SelectedTask.Status == EncodingStatus.Processing;
                CanResume = SelectedTask.Status == EncodingStatus.Paused;
                CanStop = SelectedTask.Status == EncodingStatus.Processing || SelectedTask.Status == EncodingStatus.Paused;
            }
        }

        /// <summary>
        /// 更新当前任务实时信息
        /// </summary>
        private void UpdateCurrentTaskInfo()
        {
            if (SelectedTask == null)
            {
                CurrentTaskStatus = "--";
                CurrentTaskProgress = "--";
                CurrentTaskFPS = "--";
                CurrentTaskSize = "--";
                CurrentTaskQuality = "--";
                CurrentTaskBitrate = "--";
                CurrentTaskETA = "--";
            }
            else
            {
                CurrentTaskStatus = SelectedTask.Status.ToString();
                CurrentTaskProgress = SelectedTask.ProgressPercentage.ToString("F1");
                CurrentTaskFPS = string.IsNullOrEmpty(SelectedTask.CurrentFPS) ? "--" : SelectedTask.CurrentFPS;
                CurrentTaskSize = string.IsNullOrEmpty(SelectedTask.CurrentSize) ? "--" : SelectedTask.CurrentSize;
                CurrentTaskQuality = string.IsNullOrEmpty(SelectedTask.CurrentQuality) ? "--" : SelectedTask.CurrentQuality;
                CurrentTaskBitrate = string.IsNullOrEmpty(SelectedTask.CurrentBitrate) ? "--" : SelectedTask.CurrentBitrate;
                CurrentTaskETA = SelectedTask.EstimatedTimeRemaining == TimeSpan.Zero
                    ? "--"
                    : $"{SelectedTask.EstimatedTimeRemaining.Hours:D2}:{SelectedTask.EstimatedTimeRemaining.Minutes:D2}:{SelectedTask.EstimatedTimeRemaining.Seconds:D2}";
            }
        }

        /// <summary>
        /// 定位输出文件命令
        /// </summary>
        [RelayCommand]
        private void LocateOutput()
        {
            if (SelectedTask != null && !string.IsNullOrEmpty(SelectedTask.OutputFile))
            {
                if (System.IO.File.Exists(SelectedTask.OutputFile))
                {
                    // 在资源管理器中定位文件
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{SelectedTask.OutputFile}\"");
                }
                else
                {
                    ShowInfoBar("文件不存在", "输出文件尚未生成", true);
                }
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
