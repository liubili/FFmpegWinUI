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
        // 进度缓冲数据（后台线程写入，UI线程读取）
        private readonly object _progressLock = new object();
        private ProgressBuffer _buffer = new ProgressBuffer();

        public class ProgressBuffer
        {
            public string Frame { get; set; } = string.Empty;
            public string FPS { get; set; } = string.Empty;
            public string Quality { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public long SizeBytes { get; set; } = 0;
            public TimeSpan CurrentTime { get; set; } = TimeSpan.Zero;
            public string Bitrate { get; set; } = string.Empty;
            public string Speed { get; set; } = string.Empty;
            public EncodingStatus? PendingStatus { get; set; } = null;  // 待更新的状态
            public bool HasData { get; set; } = false;
        }

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

        /// <summary>
        /// 刷新所有进度显示属性（从UI线程调用）
        /// </summary>
        public void RefreshProgressDisplay()
        {
            lock (_progressLock)
            {
                // 优先更新状态
                if (_buffer.PendingStatus.HasValue)
                {
                    Status = _buffer.PendingStatus.Value;
                    _buffer.PendingStatus = null;
                }

                if (!_buffer.HasData)
                    return;

                // 从缓冲区复制数据到属性（在UI线程触发PropertyChanged）
                CurrentFrame = _buffer.Frame;
                CurrentFPS = _buffer.FPS;
                CurrentQuality = _buffer.Quality;
                CurrentSize = _buffer.Size;
                CurrentSizeBytes = _buffer.SizeBytes;
                CurrentTime = _buffer.CurrentTime;
                CurrentBitrate = _buffer.Bitrate;
                CurrentSpeed = _buffer.Speed;

                // 清除标记
                _buffer.HasData = false;
            }

            // 触发计算属性更新
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
        }

        // === 线程安全的数据设置方法（从后台线程调用，写入缓冲区） ===

        /// <summary>
        /// 追加日志（线程安全，不触发通知）
        /// </summary>
        public void AppendLog(string text)
        {
            lock (_progressLock)
            {
                OutputLog += text + "\n";
                if (OutputLog.Length > 50000)
                {
                    OutputLog = OutputLog.Substring(OutputLog.Length - 25000);
                }
            }
        }

        /// <summary>
        /// 设置进度数据（批量设置到缓冲区）
        /// </summary>
        public void SetProgressData(string frame, string fps, string quality, long sizeValue, string sizeUnit,
            int hours, int minutes, double seconds, string bitrate, string speed)
        {
            lock (_progressLock)
            {
                _buffer.Frame = frame;
                _buffer.FPS = fps;
                _buffer.Quality = quality;
                _buffer.SizeBytes = ConvertToKB(sizeValue, sizeUnit);
                _buffer.Size = FormatSize(_buffer.SizeBytes);
                _buffer.CurrentTime = new TimeSpan(hours, minutes, 0).Add(TimeSpan.FromSeconds(seconds));
                _buffer.Bitrate = bitrate;
                _buffer.Speed = speed;
                _buffer.HasData = true;
            }
        }

        public void SetFrame(string value)
        {
            lock (_progressLock)
            {
                _buffer.Frame = value;
                _buffer.HasData = true;
            }
        }

        public void SetFPS(string value)
        {
            lock (_progressLock)
            {
                _buffer.FPS = value;
                _buffer.HasData = true;
            }
        }

        public void SetQuality(string value)
        {
            lock (_progressLock)
            {
                _buffer.Quality = value;
                _buffer.HasData = true;
            }
        }

        public void SetSize(string displayValue, long bytes)
        {
            lock (_progressLock)
            {
                _buffer.Size = displayValue;
                _buffer.SizeBytes = bytes;
                _buffer.HasData = true;
            }
        }

        public void SetCurrentTime(TimeSpan value)
        {
            lock (_progressLock)
            {
                _buffer.CurrentTime = value;
                _buffer.HasData = true;
            }
        }

        public void SetBitrate(string value)
        {
            lock (_progressLock)
            {
                _buffer.Bitrate = value;
                _buffer.HasData = true;
            }
        }

        public void SetSpeed(string value)
        {
            lock (_progressLock)
            {
                _buffer.Speed = value;
                _buffer.HasData = true;
            }
        }

        /// <summary>
        /// 设置状态（线程安全，缓冲到下次UI刷新）
        /// </summary>
        public void SetStatus(EncodingStatus status)
        {
            lock (_progressLock)
            {
                _buffer.PendingStatus = status;
            }
        }

        private long ConvertToKB(long value, string unit)
        {
            return unit.ToUpper() switch
            {
                "KIB" or "KB" => value,
                "MIB" => value * 1024,
                "GIB" => value * 1024 * 1024,
                "MB" => value * 1000,
                "GB" => value * 1000 * 1000,
                _ => value
            };
        }

        private string FormatSize(long sizeKB)
        {
            if (sizeKB >= 1024 * 1024)
                return $"{sizeKB / 1024.0 / 1024.0:F2} GB";
            else if (sizeKB >= 1024)
                return $"{sizeKB / 1024.0:F0} MB";
            else
                return $"{sizeKB} KB";
        }
    }
}
