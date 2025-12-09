using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace FFmpegWinUI.Models
{
    /// <summary>
    /// Encoding status enumeration
    /// 编码状态枚举
    /// </summary>
    public enum EncodingStatus
    {
        Pending = 0,       // 未处理
        Processing = 1,    // 正在处理
        Paused = 2,        // 已暂停
        Completed = 10,    // 已完成
        Stopped = 20,      // 已停止
        Error = 99         // 错误
    }

    /// <summary>
    /// Single encoding task
    /// 单个编码任务 - 对应VB项目的单片任务
    /// </summary>
    public partial class EncodingTask : ObservableObject
    {
        // Basic information
        public PresetData PresetData { get; set; } = new PresetData();

        [ObservableProperty]
        private string _inputFile = string.Empty;

        public long InputFileSize { get; set; } = 0;

        [ObservableProperty]
        private string _outputFile = string.Empty;

        public string CustomOutputPath { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;

        // Status
        [ObservableProperty]
        private EncodingStatus _status = EncodingStatus.Pending;

        // Progress information
        public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero;
        public bool HasDuration { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
        [NotifyPropertyChangedFor(nameof(EstimatedTimeRemaining))]
        private string _currentFrame = string.Empty;

        [ObservableProperty]
        private string _currentFPS = string.Empty;

        [ObservableProperty]
        private string _currentQuality = string.Empty;

        [ObservableProperty]
        private string _currentSize = string.Empty;

        public long CurrentSizeBytes { get; set; } = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
        [NotifyPropertyChangedFor(nameof(EstimatedTimeRemaining))]
        private TimeSpan _currentTime = TimeSpan.Zero;

        [ObservableProperty]
        private string _currentBitrate = string.Empty;

        [ObservableProperty]
        private string _currentSpeed = string.Empty;

        // Calculated properties
        public double ProgressPercentage
        {
            get
            {
                if (!HasDuration || TotalDuration.TotalSeconds == 0)
                    return 0;
                return (CurrentTime.TotalSeconds / TotalDuration.TotalSeconds) * 100.0;
            }
        }

        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (!HasDuration || CurrentTime.TotalSeconds == 0)
                    return TimeSpan.Zero;

                var progress = CurrentTime.TotalSeconds / TotalDuration.TotalSeconds;
                if (progress == 0) return TimeSpan.Zero;

                var elapsed = DateTime.Now - StartTime;
                var totalEstimated = elapsed.TotalSeconds / progress;
                var remaining = totalEstimated - elapsed.TotalSeconds;

                return TimeSpan.FromSeconds(Math.Max(0, remaining));
            }
        }

        // Timestamps
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        // Process and error info
        [JsonIgnore]
        public Process? FFmpegProcess { get; set; }

        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// 错误消息的格式化字符串（用于XAML绑定）
        /// Returns the most recent error message or joined errors
        /// </summary>
        [JsonIgnore]
        public string ErrorMessage => ErrorMessages.Count > 0
            ? ErrorMessages[^1]  // 返回最后一条错误消息
            : string.Empty;

        public string OutputLog { get; set; } = string.Empty;

        /// <summary>
        /// Clear progress information
        /// 清空进度信息
        /// </summary>
        public void ClearProgress()
        {
            CurrentFrame = string.Empty;
            CurrentFPS = string.Empty;
            CurrentQuality = string.Empty;
            CurrentSize = string.Empty;
            CurrentSizeBytes = 0;
            CurrentTime = TimeSpan.Zero;
            CurrentBitrate = string.Empty;
            CurrentSpeed = string.Empty;
            ErrorMessages.Clear();
            OutputLog = string.Empty;
        }

        /// <summary>
        /// Refresh calculated properties
        /// 刷新计算属性（用于定时器更新）
        /// </summary>
        public void RefreshCalculatedProperties()
        {
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
        }
    }
}
