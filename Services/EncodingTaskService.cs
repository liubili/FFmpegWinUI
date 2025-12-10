using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFmpegWinUI.Models;

namespace FFmpegWinUI.Services
{
    /// <summary>
    /// 编码任务服务接口
    /// </summary>
    public interface IEncodingTaskService
    {
        event EventHandler<EncodingTask>? TaskStatusChanged;
        event EventHandler<EncodingTask>? TaskProgressUpdated;
        event EventHandler<string>? TaskOutputReceived;

        Task StartTaskAsync(EncodingTask task);
        void PauseTask(EncodingTask task);
        void ResumeTask(EncodingTask task);
        void StopTask(EncodingTask task);
        void RemoveTask(EncodingTask task);
        int GetConcurrentTaskLimit();
        void SetConcurrentTaskLimit(int limit);
    }

    /// <summary>
    /// 编码任务服务 - 对应VB项目的编码任务.vb
    /// 负责FFmpeg进程管理、输出解析、任务队列控制
    /// </summary>
    public class EncodingTaskService : IEncodingTaskService
    {
        // Windows API用于进程暂停/恢复
        [DllImport("ntdll.dll", PreserveSig = false)]
        private static extern void NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", PreserveSig = false)]
        private static extern void NtResumeProcess(IntPtr processHandle);

        private int _concurrentTaskLimit = 1;
        private readonly string _ffmpegPath;
        private readonly IPresetService _presetService;

        // 事件
        public event EventHandler<EncodingTask>? TaskStatusChanged;
        public event EventHandler<EncodingTask>? TaskProgressUpdated;
        public event EventHandler<string>? TaskOutputReceived;

        // 正则表达式用于解析FFmpeg输出
        private static readonly Regex DurationPattern = new Regex(@"Duration:\s*(\d{2}):(\d{2}):(\d{2}\.\d{2})", RegexOptions.Compiled);
        private static readonly Regex ProgressPattern = new Regex(@"frame=\s*(\d+)\s+fps=\s*([\d.]+)\s+q=([\d.-]+)\s+size=\s*(\d+)([KMG]i?B)\s+time=(\d{2}):(\d{2}):(\d{2}\.\d{2})\s+bitrate=\s*([\d.]+)kbits/s\s+speed=\s*([\d\.eE\+\-]+)x", RegexOptions.Compiled);

        // 错误关键词
        private static readonly string[] ErrorKeywords = {
            "Error", "Invalid", "cannot", "failed", "not supported",
            "require", "must be", "Could not", "is experimental",
            "if you want to use it", "Nothing was written"
        };

        public EncodingTaskService(IPresetService presetService, string ffmpegPath = "ffmpeg")
        {
            _presetService = presetService;
            _ffmpegPath = ffmpegPath;
        }

        /// <summary>
        /// 启动编码任务
        /// </summary>
        public async Task StartTaskAsync(EncodingTask task)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] StartTaskAsync开始 - 任务: {task.InputFile}");

            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Task.Run内部开始执行");

                    task.SetStatus(EncodingStatus.Processing);
                    task.StartTime = DateTime.Now;
                    task.ClearProgress();

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 状态已设置为Processing");

                    // 检查输入文件
                    if (!File.Exists(task.InputFile))
                    {
                        throw new FileNotFoundException($"输入文件不存在: {task.InputFile}");
                    }

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 输入文件检查通过: {task.InputFile}");

                    // 获取输入文件大小
                    task.InputFileSize = new FileInfo(task.InputFile).Length;

                    // 生成输出文件路径
                    if (string.IsNullOrEmpty(task.OutputFile))
                    {
                        var outputDir = string.IsNullOrEmpty(task.CustomOutputPath)
                            ? Path.GetDirectoryName(task.InputFile)
                            : task.CustomOutputPath;
                        var fileName = Path.GetFileNameWithoutExtension(task.InputFile);
                        var extension = string.IsNullOrEmpty(task.PresetData.OutputContainer)
                            ? Path.GetExtension(task.InputFile)
                            : "." + task.PresetData.OutputContainer;
                        task.OutputFile = Path.Combine(outputDir!, fileName + "_output" + extension);
                    }

                    // 生成FFmpeg命令行
                    task.CommandLine = _presetService.GenerateCommandLine(task.PresetData, task.InputFile, task.OutputFile);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 命令行生成: {task.CommandLine}");

                    // 创建FFmpeg进程
                    var psi = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = task.CommandLine,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardInputEncoding = Encoding.UTF8
                    };

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ProcessStartInfo创建完成");

                    task.FFmpegProcess = new Process { StartInfo = psi };
                    task.FFmpegProcess.EnableRaisingEvents = true;

                    // 绑定事件
                    task.FFmpegProcess.OutputDataReceived += (s, e) => OnFfmpegOutputReceived(task, e);
                    task.FFmpegProcess.ErrorDataReceived += (s, e) => OnFfmpegOutputReceived(task, e);
                    task.FFmpegProcess.Exited += (s, e) => OnFfmpegProcessExited(task);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 事件绑定完成，准备启动进程");

                    // 启动进程
                    try
                    {
                        task.FFmpegProcess.Start();
                        task.FFmpegProcess.BeginOutputReadLine();
                        task.FFmpegProcess.BeginErrorReadLine();

                        System.Diagnostics.Debug.WriteLine($"[DEBUG] FFmpeg进程启动成功，PID: {task.FFmpegProcess.Id}");

                        // 触发状态变化事件
                        TaskStatusChanged?.Invoke(this, task);

                        System.Diagnostics.Debug.WriteLine($"[DEBUG] TaskStatusChanged事件已触发");
                    }
                    catch (System.ComponentModel.Win32Exception winEx)
                    {
                        // FFmpeg进程启动失败
                        var errorMsg = $"FFmpeg进程启动失败 (错误代码: {winEx.NativeErrorCode})\n";
                        errorMsg += $"详细信息: {winEx.Message}\n";
                        errorMsg += $"FFmpeg路径: {_ffmpegPath}\n";
                        errorMsg += $"完整命令: {_ffmpegPath} {task.CommandLine}\n";
                        errorMsg += "\n可能原因:\n";
                        errorMsg += "1. FFmpeg未安装或不在系统PATH环境变量中\n";
                        errorMsg += "2. FFmpeg路径配置错误\n";
                        errorMsg += "3. 缺少必要的运行时依赖\n";
                        errorMsg += "\n解决方法:\n";
                        errorMsg += "1. 下载FFmpeg: https://www.gyan.dev/ffmpeg/builds/\n";
                        errorMsg += "2. 将ffmpeg.exe所在目录添加到系统PATH环境变量\n";
                        errorMsg += "3. 或将ffmpeg.exe放置在程序同级目录";

                        task.SetStatus(EncodingStatus.Error);
                        task.ErrorMessages.Add(errorMsg);
                        task.AppendLog($"[错误] {errorMsg}");
                        TaskStatusChanged?.Invoke(this, task);
                    }
                }
                catch (Exception ex)
                {
                    task.SetStatus(EncodingStatus.Error);
                    task.ErrorMessages.Add(ex.Message);
                    task.AppendLog($"[错误] {ex.Message}");
                    TaskStatusChanged?.Invoke(this, task);
                }
            });
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public void PauseTask(EncodingTask task)
        {
            try
            {
                if (task.Status == EncodingStatus.Processing && task.FFmpegProcess != null && !task.FFmpegProcess.HasExited)
                {
                    NtSuspendProcess(task.FFmpegProcess.Handle);
                    task.SetStatus(EncodingStatus.Paused);
                    TaskStatusChanged?.Invoke(this, task);
                }
            }
            catch (Exception ex)
            {
                task.AppendLog($"[错误] 暂停任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public void ResumeTask(EncodingTask task)
        {
            try
            {
                if (task.Status == EncodingStatus.Paused && task.FFmpegProcess != null && !task.FFmpegProcess.HasExited)
                {
                    NtResumeProcess(task.FFmpegProcess.Handle);
                    task.SetStatus(EncodingStatus.Processing);
                    TaskStatusChanged?.Invoke(this, task);
                }
            }
            catch (Exception ex)
            {
                task.AppendLog($"[错误] 恢复任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        public void StopTask(EncodingTask task)
        {
            try
            {
                if (task.FFmpegProcess != null && !task.FFmpegProcess.HasExited)
                {
                    task.FFmpegProcess.Kill();
                    task.SetStatus(EncodingStatus.Stopped);
                    task.EndTime = DateTime.Now;
                    TaskStatusChanged?.Invoke(this, task);
                }
            }
            catch (Exception ex)
            {
                task.AppendLog($"[错误] 停止任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public void RemoveTask(EncodingTask task)
        {
            try
            {
                // 如果任务正在运行，先停止
                if (task.Status == EncodingStatus.Processing || task.Status == EncodingStatus.Paused)
                {
                    StopTask(task);
                }

                // 释放进程资源
                task.FFmpegProcess?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"移除任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// FFmpeg输出处理
        /// </summary>
        private void OnFfmpegOutputReceived(EncodingTask task, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            // 添加到输出日志（不触发属性通知）
            task.AppendLog(e.Data);

            // 触发输出事件
            TaskOutputReceived?.Invoke(this, e.Data);

            // 检查错误关键词
            if (ErrorKeywords.Any(keyword => e.Data.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                task.ErrorMessages.Add(e.Data);
            }

            // 解析Duration（总时长）
            if (!task.HasDuration && e.Data.Contains("Duration"))
            {
                var match = DurationPattern.Match(e.Data);
                if (match.Success)
                {
                    var hours = int.Parse(match.Groups[1].Value);
                    var minutes = int.Parse(match.Groups[2].Value);
                    var seconds = double.Parse(match.Groups[3].Value);
                    task.TotalDuration = new TimeSpan(hours, minutes, 0).Add(TimeSpan.FromSeconds(seconds));
                    task.HasDuration = true;
                }
            }

            // 解析进度信息（不触发属性通知，由定时器统一更新UI）
            if (e.Data.Contains("frame=") && e.Data.Contains("time="))
            {
                ParseProgress(task, e.Data);
                // 不在这里触发事件，让定时器统一更新
            }
        }

        /// <summary>
        /// 解析FFmpeg进度输出（不触发UI更新）
        /// </summary>
        private void ParseProgress(EncodingTask task, string output)
        {
            try
            {
                var match = ProgressPattern.Match(output);
                if (match.Success)
                {
                    task.SetProgressData(
                        frame: match.Groups[1].Value,
                        fps: match.Groups[2].Value,
                        quality: match.Groups[3].Value,
                        sizeValue: long.Parse(match.Groups[4].Value),
                        sizeUnit: match.Groups[5].Value,
                        hours: int.Parse(match.Groups[6].Value),
                        minutes: int.Parse(match.Groups[7].Value),
                        seconds: double.Parse(match.Groups[8].Value),
                        bitrate: match.Groups[9].Value + " kbits/s",
                        speed: match.Groups[10].Value + "x"
                    );
                }
                else
                {
                    // 简化版解析（备用方案）
                    var frameMatch = Regex.Match(output, @"frame=\s*(\d+)");
                    if (frameMatch.Success)
                        task.SetFrame(frameMatch.Groups[1].Value);

                    var fpsMatch = Regex.Match(output, @"fps=\s*([\d.]+)");
                    if (fpsMatch.Success)
                        task.SetFPS(fpsMatch.Groups[1].Value);

                    var timeMatch = Regex.Match(output, @"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
                    if (timeMatch.Success)
                    {
                        var h = int.Parse(timeMatch.Groups[1].Value);
                        var m = int.Parse(timeMatch.Groups[2].Value);
                        var s = double.Parse(timeMatch.Groups[3].Value);
                        task.SetCurrentTime(new TimeSpan(h, m, 0).Add(TimeSpan.FromSeconds(s)));
                    }

                    var sizeMatch = Regex.Match(output, @"size=\s*(\d+)([KMG]i?B)");
                    if (sizeMatch.Success)
                    {
                        var sizeValue = long.Parse(sizeMatch.Groups[1].Value);
                        var sizeUnit = sizeMatch.Groups[2].Value;
                        var sizeKB = ConvertToKB(sizeValue, sizeUnit);
                        task.SetSize(FormatSize(sizeKB), sizeKB);
                    }

                    var bitrateMatch = Regex.Match(output, @"bitrate=\s*([\d.]+)kbits/s");
                    if (bitrateMatch.Success)
                        task.SetBitrate(bitrateMatch.Groups[1].Value + " kbits/s");

                    var speedMatch = Regex.Match(output, @"speed=\s*([\d\.eE\+\-]+)x");
                    if (speedMatch.Success)
                        task.SetSpeed(speedMatch.Groups[1].Value + "x");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析进度失败: {ex.Message}");
            }
        }

        /// <summary>
        /// FFmpeg进程退出处理
        /// </summary>
        private void OnFfmpegProcessExited(EncodingTask task)
        {
            task.EndTime = DateTime.Now;

            if (task.FFmpegProcess?.ExitCode == 0)
            {
                task.SetStatus(EncodingStatus.Completed);

                // 保留文件时间戳
                try
                {
                    if (File.Exists(task.OutputFile) && File.Exists(task.InputFile))
                    {
                        if (task.PresetData.PreserveCreationTime)
                            File.SetCreationTime(task.OutputFile, File.GetCreationTime(task.InputFile));

                        if (task.PresetData.PreserveModifiedTime)
                            File.SetLastWriteTime(task.OutputFile, File.GetLastWriteTime(task.InputFile));

                        if (task.PresetData.PreserveAccessTime)
                            File.SetLastAccessTime(task.OutputFile, File.GetLastAccessTime(task.InputFile));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保留时间戳失败: {ex.Message}");
                }
            }
            else
            {
                task.SetStatus(EncodingStatus.Error);

                // 删除失败的输出文件
                try
                {
                    if (File.Exists(task.OutputFile))
                    {
                        File.Delete(task.OutputFile);
                    }
                }
                catch { }
            }

            TaskStatusChanged?.Invoke(this, task);
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatSize(long sizeKB)
        {
            if (sizeKB >= 1024 * 1024)
                return $"{sizeKB / 1024.0 / 1024.0:F2} GB";
            else if (sizeKB >= 1024)
                return $"{sizeKB / 1024.0:F0} MB";
            else
                return $"{sizeKB} KB";
        }

        /// <summary>
        /// 将FFmpeg输出的大小单位转换为KB
        /// 支持 kB, MB, GB (十进制) 和 KiB, MiB, GiB (二进制)
        /// </summary>
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

        /// <summary>
        /// 获取并发任务数限制
        /// </summary>
        public int GetConcurrentTaskLimit() => _concurrentTaskLimit;

        /// <summary>
        /// 设置并发任务数限制
        /// </summary>
        public void SetConcurrentTaskLimit(int limit)
        {
            _concurrentTaskLimit = Math.Max(1, Math.Min(limit, 10));
        }
    }
}
