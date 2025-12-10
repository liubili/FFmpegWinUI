using CommunityToolkit.Mvvm.ComponentModel;
using FFmpegWinUI.Services;
using Microsoft.UI.Dispatching;
using System;

namespace FFmpegWinUI.ViewModels
{
    /// <summary>
    /// 起始页ViewModel - 对应VB项目的"3FUI"标签页
    /// </summary>
    public partial class HomePageViewModel : ObservableObject
    {
        private readonly FilesPageViewModel _filesPageViewModel;
        private readonly QueuePageViewModel _queuePageViewModel;

        // 欢迎标题
        [ObservableProperty]
        private string _welcomeTitle = "欢迎使用 FFmpeg WinUI";

        // 欢迎副标题
        [ObservableProperty]
        private string _welcomeSubtitle = "拖放媒体文件到此处开始编码";

        // 待处理文件数
        [ObservableProperty]
        private int _pendingFilesCount;

        // 队列任务数
        [ObservableProperty]
        private int _queueTasksCount;

        // 已完成任务数
        [ObservableProperty]
        private int _completedTasksCount;

        public HomePageViewModel(
            FilesPageViewModel filesPageViewModel,
            QueuePageViewModel queuePageViewModel)
        {
            _filesPageViewModel = filesPageViewModel;
            _queuePageViewModel = queuePageViewModel;

            // 订阅数据更新
            _filesPageViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilesPageViewModel.TotalFiles))
                {
                    PendingFilesCount = _filesPageViewModel.TotalFiles;
                }
            };

            _queuePageViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(QueuePageViewModel.TotalTasks))
                {
                    QueueTasksCount = _queuePageViewModel.TotalTasks;
                }
                else if (e.PropertyName == nameof(QueuePageViewModel.CompletedTasks))
                {
                    CompletedTasksCount = _queuePageViewModel.CompletedTasks;
                }
            };

            // 初始化统计数据
            UpdateStatistics();
        }

        /// <summary>
        /// 更新统计数据
        /// </summary>
        public void UpdateStatistics()
        {
            PendingFilesCount = _filesPageViewModel.TotalFiles;
            QueueTasksCount = _queuePageViewModel.TotalTasks;
            CompletedTasksCount = _queuePageViewModel.CompletedTasks;
        }

        /// <summary>
        /// 加载个性化设置
        /// </summary>
        public void LoadPersonalizationSettings(Models.UserSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.个性化_起始页标题))
            {
                WelcomeTitle = settings.个性化_起始页标题;
            }

            if (!string.IsNullOrEmpty(settings.个性化_起始页副标题))
            {
                WelcomeSubtitle = settings.个性化_起始页副标题;
            }
        }
    }
}
