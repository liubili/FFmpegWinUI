using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegWinUI.Services
{
    /// <summary>
    /// 媒体信息服务接口
    /// </summary>
    public interface IMediaInfoService
    {
        Task<string> GetMediaInfoAsync(string filePath);
        Task<MediaInfo?> ParseMediaInfoAsync(string filePath);
        Task<TimeSpan?> GetDurationAsync(string filePath);
    }

    /// <summary>
    /// 媒体信息结构
    /// </summary>
    public class MediaInfo
    {
        public string FileName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string VideoCodec { get; set; } = string.Empty;
        public string AudioCodec { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public string FrameRate { get; set; } = string.Empty;
        public string BitRate { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string RawOutput { get; set; } = string.Empty;
    }

    /// <summary>
    /// 媒体信息服务
    /// 使用FFprobe获取媒体文件的详细信息
    /// </summary>
    public class MediaInfoService : IMediaInfoService
    {
        private readonly string _ffprobePath;

        public MediaInfoService(string ffprobePath = "ffprobe")
        {
            _ffprobePath = ffprobePath;
        }

        /// <summary>
        /// 获取媒体文件的完整信息（原始FFprobe输出）
        /// </summary>
        public async Task<string> GetMediaInfoAsync(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return $"错误: 文件不存在 - {filePath}";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return $"FFprobe错误:\n{error}";
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"获取媒体信息失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析媒体信息为结构化数据
        /// </summary>
        public async Task<MediaInfo?> ParseMediaInfoAsync(string filePath)
        {
            try
            {
                var rawInfo = await GetMediaInfoAsync(filePath);

                // 简化版解析 - 可以后续使用JSON反序列化完整解析
                var mediaInfo = new MediaInfo
                {
                    FileName = System.IO.Path.GetFileName(filePath),
                    RawOutput = rawInfo
                };

                // 解析文件大小
                if (System.IO.File.Exists(filePath))
                {
                    mediaInfo.FileSize = new System.IO.FileInfo(filePath).Length;
                }

                // TODO: 这里可以使用System.Text.Json解析FFprobe的JSON输出
                // 获取更详细的媒体信息（编解码器、分辨率、帧率等）

                return mediaInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析媒体信息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取媒体文件时长
        /// </summary>
        public async Task<TimeSpan?> GetDurationAsync(string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (double.TryParse(output.Trim(), out var seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取时长失败: {ex.Message}");
                return null;
            }
        }
    }
}
