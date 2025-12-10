using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FFmpegWinUI.Models;

namespace FFmpegWinUI.Services
{
    /// <summary>
    /// 预设服务接口
    /// </summary>
    public interface IPresetService
    {
        Task SavePresetAsync(PresetData preset, string filePath);
        Task<PresetData?> LoadPresetAsync(string filePath);
        Task<List<string>> GetPresetListAsync(string directory);
        Task DeletePresetAsync(string filePath);
        string GenerateCommandLine(PresetData preset, string inputFile, string outputFile);
        string GetDefaultPresetDirectory();
    }

    /// <summary>
    /// 预设服务 - 对应VB项目的预设管理.vb
    /// 负责预设的保存、加载、管理和FFmpeg命令行生成
    /// </summary>
    public class PresetService : IPresetService
    {
        private readonly string _defaultPresetDirectory;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public PresetService()
        {
            // 默认预设目录：应用数据目录下的Presets文件夹
            // 对于非打包应用，使用 LocalApplicationData 文件夹
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "FFmpegWinUI");
            _defaultPresetDirectory = Path.Combine(appFolder, "Presets");

            // 确保预设目录存在
            if (!Directory.Exists(_defaultPresetDirectory))
            {
                Directory.CreateDirectory(_defaultPresetDirectory);
            }
        }

        /// <summary>
        /// 保存预设到JSON文件
        /// </summary>
        public async Task SavePresetAsync(PresetData preset, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(preset, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存预设失败: {ex.Message}");
                throw new Exception($"保存预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从JSON文件加载预设
        /// </summary>
        public async Task<PresetData?> LoadPresetAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<PresetData>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载预设失败: {ex.Message}");
                throw new Exception($"加载预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取指定目录下的所有预设文件列表
        /// </summary>
        public async Task<List<string>> GetPresetListAsync(string directory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var targetDir = string.IsNullOrEmpty(directory) ? _defaultPresetDirectory : directory;

                    if (!Directory.Exists(targetDir))
                        return new List<string>();

                    return Directory.GetFiles(targetDir, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .OrderBy(name => name)
                        .ToList()!;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取预设列表失败: {ex.Message}");
                    return new List<string>();
                }
            });
        }

        /// <summary>
        /// 删除预设文件
        /// </summary>
        public async Task DeletePresetAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"删除预设失败: {ex.Message}");
                    throw new Exception($"删除预设失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 生成FFmpeg命令行
        /// </summary>
        public string GenerateCommandLine(PresetData preset, string inputFile, string outputFile)
        {
            // 完全自定义命令行模式（覆盖所有自动生成）
            if (!string.IsNullOrEmpty(preset.完全自定义命令行))
            {
                var customCmd = preset.完全自定义命令行;
                customCmd = ProcessCustomParamWildcards(customCmd, inputFile);
                // 替换输出文件占位符（如果有）
                customCmd = customCmd.Replace("<OutputFile>", outputFile);
                return customCmd;
            }

            var args = new List<string>();

            // FFmpeg标准参数（隐藏横幅和禁用stdin）
            args.Add("-hide_banner");
            args.Add("-nostdin");

            // 自定义开头参数（支持通配符）
            if (!string.IsNullOrEmpty(preset.自定义开头参数))
            {
                var processedParam = ProcessCustomParamWildcards(preset.自定义开头参数, inputFile);
                if (!string.IsNullOrEmpty(processedParam))
                    args.Add(processedParam);
            }

            // Pre-input 自定义参数
            if (preset.PreInputParams != null && preset.PreInputParams.Count > 0)
            {
                foreach (var param in preset.PreInputParams)
                {
                    if (!string.IsNullOrEmpty(param))
                        args.Add(param);
                }
            }

            // 硬件加速（在输入文件之前）
            if (!string.IsNullOrEmpty(preset.HardwareAccelName))
            {
                args.Add($"-hwaccel {preset.HardwareAccelName}");

                if (!string.IsNullOrEmpty(preset.HardwareAccelParams))
                    args.Add($"-hwaccel_device {preset.HardwareAccelParams}");
            }

            // 解码器设置（在输入文件之前）
            if (!string.IsNullOrEmpty(preset.DecoderName))
                args.Add($"-c:v {preset.DecoderName}");

            if (!string.IsNullOrEmpty(preset.DecoderThreads))
                args.Add($"-threads {preset.DecoderThreads}");

            if (!string.IsNullOrEmpty(preset.DecoderFormat))
                args.Add($"-pix_fmt {preset.DecoderFormat}");

            // 剪辑参数 - 根据方法选择（关键修复）
            switch (preset.ClipMethod)
            {
                case 1:  // 粗剪：在输入前添加-ss和-to（快速但不精确）
                    if (!string.IsNullOrEmpty(preset.ClipStartTime))
                        args.Add($"-ss {preset.ClipStartTime}");
                    if (!string.IsNullOrEmpty(preset.ClipEndTime))
                        args.Add($"-to {preset.ClipEndTime}");
                    break;

                case 2:  // 精剪标准：在输入后添加-ss（精确但慢）
                    // -ss参数将在输入文件之后添加
                    break;

                case 3:  // 精剪快速响应：计算向前解码时间
                    if (!string.IsNullOrEmpty(preset.ClipStartTime) &&
                        !string.IsNullOrEmpty(preset.PreDecodeSeconds))
                    {
                        try
                        {
                            var startTime = TimeSpan.Parse(preset.ClipStartTime);
                            var decodeAhead = TimeSpan.Parse(preset.PreDecodeSeconds);
                            var calculatedStart = startTime - decodeAhead;
                            if (calculatedStart < TimeSpan.Zero) calculatedStart = TimeSpan.Zero;
                            args.Add($"-ss {calculatedStart:hh\\:mm\\:ss\\.ff}");
                        }
                        catch
                        {
                            // 解析失败时回退到粗剪
                            if (!string.IsNullOrEmpty(preset.ClipStartTime))
                                args.Add($"-ss {preset.ClipStartTime}");
                        }
                    }
                    else if (!string.IsNullOrEmpty(preset.ClipStartTime))
                    {
                        // 如果没有指定向前解码时间，使用粗剪
                        args.Add($"-ss {preset.ClipStartTime}");
                    }
                    break;

                default:  // 默认使用粗剪
                    if (!string.IsNullOrEmpty(preset.ClipStartTime))
                        args.Add($"-ss {preset.ClipStartTime}");
                    break;
            }

            // 输入文件
            args.Add($"-i \"{inputFile}\"");

            // 精剪标准模式：在输入后添加-ss
            if (preset.ClipMethod == 2 && !string.IsNullOrEmpty(preset.ClipStartTime))
            {
                args.Add($"-ss {preset.ClipStartTime}");
            }

            // 字幕自动混流（关键新增功能）
            var subtitleFiles = new List<string>();
            if (preset.AutoMuxSRT)
            {
                var srtPath = Path.ChangeExtension(inputFile, ".srt");
                if (File.Exists(srtPath)) subtitleFiles.Add(srtPath);
            }
            if (preset.AutoMuxASS)
            {
                var assPath = Path.ChangeExtension(inputFile, ".ass");
                if (File.Exists(assPath)) subtitleFiles.Add(assPath);
            }
            if (preset.AutoMuxSSA)
            {
                var ssaPath = Path.ChangeExtension(inputFile, ".ssa");
                if (File.Exists(ssaPath)) subtitleFiles.Add(ssaPath);
            }

            // 添加字幕文件为额外输入
            foreach (var subFile in subtitleFiles)
            {
                args.Add($"-i \"{subFile}\"");
            }

            // 应用字幕编码（自动混流的字幕）
            for (int i = 1; i <= subtitleFiles.Count; i++)
            {
                var codec = preset.ConvertSubtitleToMovText ? "mov_text" : "copy";
                args.Add($"-map {i}:s? -c:s:{i - 1} {codec}");
            }

            // 流选择和多流处理
            // 多流处理：保留其他视频流
            if (preset.EnableKeepOtherVideoStreams && !string.IsNullOrEmpty(preset.VideoEncoder))
            {
                // 首先映射所有视频流并复制
                args.Add("-map 0:v? -c:v copy");

                // 如果指定了要应用参数的流，排除这些流
                if (preset.VideoStreamParamsList != null && preset.VideoStreamParamsList.Count > 0)
                {
                    foreach (var streamIndex in preset.VideoStreamParamsList)
                    {
                        if (!string.IsNullOrEmpty(streamIndex))
                            args.Add($"-map -0:v:{streamIndex}?");
                    }
                }
                else if (!string.IsNullOrEmpty(preset.VideoStreamIndex))
                {
                    // 如果没有指定列表但有VideoStreamIndex，排除这个流
                    args.Add($"-map -0:v:{preset.VideoStreamIndex}?");
                }
                else
                {
                    // 默认排除第一个流
                    args.Add("-map -0:v:0?");
                }
            }

            // 简单流选择（如果没有启用多流处理）
            if (!string.IsNullOrEmpty(preset.VideoStreamIndex) && !preset.EnableKeepOtherVideoStreams)
                args.Add($"-map 0:v:{preset.VideoStreamIndex}");

            if (!string.IsNullOrEmpty(preset.AudioStreamIndex))
                args.Add($"-map 0:a:{preset.AudioStreamIndex}");

            if (!string.IsNullOrEmpty(preset.SubtitleStreamIndex))
                args.Add($"-map 0:s:{preset.SubtitleStreamIndex}");

            // 视频编码器
            if (!string.IsNullOrEmpty(preset.VideoEncoder))
            {
                args.Add($"-c:v {preset.VideoEncoder}");

                // 编码预设
                if (!string.IsNullOrEmpty(preset.VideoEncoderPreset))
                    args.Add($"-preset {preset.VideoEncoderPreset}");

                // 配置文件
                if (!string.IsNullOrEmpty(preset.VideoEncoderProfile))
                    args.Add($"-profile:v {preset.VideoEncoderProfile}");

                // Tune
                if (!string.IsNullOrEmpty(preset.VideoEncoderTune))
                    args.Add($"-tune {preset.VideoEncoderTune}");

                // GPU选择
                if (!string.IsNullOrEmpty(preset.VideoEncoderGPU))
                    args.Add($"-gpu {preset.VideoEncoderGPU}");

                // 编码线程数
                if (!string.IsNullOrEmpty(preset.VideoEncoderThreads))
                    args.Add($"-threads {preset.VideoEncoderThreads}");
            }

            // 质量控制
            if (!string.IsNullOrEmpty(preset.QualityControlParamName) &&
                !string.IsNullOrEmpty(preset.QualityControlValue))
            {
                args.Add($"-{preset.QualityControlParamName} {preset.QualityControlValue}");
            }

            // 比特率控制
            if (!string.IsNullOrEmpty(preset.VideoBitrate))
            {
                var videoBitrate = preset.VideoBitrate;
                if (!videoBitrate.EndsWith("k", StringComparison.OrdinalIgnoreCase) &&
                    !videoBitrate.EndsWith("M", StringComparison.OrdinalIgnoreCase) &&
                    !videoBitrate.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    videoBitrate += "k";
                }
                args.Add($"-b:v {videoBitrate}");
            }

            if (!string.IsNullOrEmpty(preset.VideoMinBitrate))
            {
                var minBitrate = preset.VideoMinBitrate;
                if (!minBitrate.EndsWith("k", StringComparison.OrdinalIgnoreCase) &&
                    !minBitrate.EndsWith("M", StringComparison.OrdinalIgnoreCase) &&
                    !minBitrate.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    minBitrate += "k";
                }
                args.Add($"-minrate {minBitrate}");
            }

            if (!string.IsNullOrEmpty(preset.VideoMaxBitrate))
            {
                var maxBitrate = preset.VideoMaxBitrate;
                if (!maxBitrate.EndsWith("k", StringComparison.OrdinalIgnoreCase) &&
                    !maxBitrate.EndsWith("M", StringComparison.OrdinalIgnoreCase) &&
                    !maxBitrate.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    maxBitrate += "k";
                }
                args.Add($"-maxrate {maxBitrate}");
            }

            if (!string.IsNullOrEmpty(preset.VideoBufferSize))
            {
                var bufSize = preset.VideoBufferSize;
                if (!bufSize.EndsWith("k", StringComparison.OrdinalIgnoreCase) &&
                    !bufSize.EndsWith("M", StringComparison.OrdinalIgnoreCase) &&
                    !bufSize.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    bufSize += "k";
                }
                args.Add($"-bufsize {bufSize}");
            }

            // 高级质量控制参数
            if (preset.AdvancedQualityParams != null && preset.AdvancedQualityParams.Count > 0)
            {
                foreach (var param in preset.AdvancedQualityParams)
                {
                    if (!string.IsNullOrEmpty(param))
                        args.Add(param);
                }
            }

            // 像素格式
            if (!string.IsNullOrEmpty(preset.PixelFormat))
                args.Add($"-pix_fmt {preset.PixelFormat}");

            // 自定义视频参数（支持通配符）
            if (!string.IsNullOrEmpty(preset.自定义视频参数))
            {
                var processedParam = ProcessCustomParamWildcards(preset.自定义视频参数, inputFile);
                if (!string.IsNullOrEmpty(processedParam))
                    args.Add(processedParam);
            }

            // 构建视频滤镜链
            var videoFilters = new List<string>();

            // 1. 裁剪滤镜
            if (!string.IsNullOrEmpty(preset.VideoCropFilter))
                videoFilters.Add(preset.VideoCropFilter);

            // 2. 插帧滤镜
            if (!string.IsNullOrEmpty(preset.InterpolationTargetFPS) &&
                !string.IsNullOrEmpty(preset.InterpolationMode))
            {
                var minterpolateFilter = $"minterpolate=fps={preset.InterpolationTargetFPS}";

                if (!string.IsNullOrEmpty(preset.InterpolationMode))
                    minterpolateFilter += $":mi_mode={preset.InterpolationMode}";

                if (!string.IsNullOrEmpty(preset.MotionEstimationMode))
                    minterpolateFilter += $":me_mode={preset.MotionEstimationMode}";

                if (!string.IsNullOrEmpty(preset.MotionEstimationAlgorithm))
                    minterpolateFilter += $":me={preset.MotionEstimationAlgorithm}";

                if (!string.IsNullOrEmpty(preset.MotionCompensationMode))
                    minterpolateFilter += $":mc_mode={preset.MotionCompensationMode}";

                if (preset.VariableBlockSizeMC)
                    minterpolateFilter += ":vsbmc=1";

                if (!string.IsNullOrEmpty(preset.BlockSize))
                    minterpolateFilter += $":mb_size={preset.BlockSize}";

                if (!string.IsNullOrEmpty(preset.SearchRange))
                    minterpolateFilter += $":search_param={preset.SearchRange}";

                if (!string.IsNullOrEmpty(preset.SceneChangeThreshold))
                    minterpolateFilter += $":scd=fdiff:scd_threshold={preset.SceneChangeThreshold}";

                videoFilters.Add(minterpolateFilter);
            }

            // 3. 帧混合滤镜
            if (!string.IsNullOrEmpty(preset.BlendingTargetFPS) &&
                !string.IsNullOrEmpty(preset.BlendingMode))
            {
                var tmixFilter = $"tmix=frames={preset.BlendingRatio ?? "2"}";
                videoFilters.Add(tmixFilter);
                videoFilters.Add($"fps=fps={preset.BlendingTargetFPS}");
            }

            // 4. 超分辨率/缩放滤镜
            if (!string.IsNullOrEmpty(preset.UpscaleTargetWidth) ||
                !string.IsNullOrEmpty(preset.UpscaleTargetHeight))
            {
                var width = preset.UpscaleTargetWidth ?? "-1";
                var height = preset.UpscaleTargetHeight ?? "-1";

                // 使用着色器链（如果有）
                if (preset.ShaderList != null && preset.ShaderList.Count > 0)
                {
                    // 关键修复：所有shader添加到同一个libplacebo滤镜
                    var libplaceboFilter = $"libplacebo=w={width}:h={height}";

                    // 添加上采样算法
                    if (!string.IsNullOrEmpty(preset.UpsampleAlgorithm))
                        libplaceboFilter += $":upscaler={preset.UpsampleAlgorithm}";

                    // 添加下采样算法
                    if (!string.IsNullOrEmpty(preset.DownsampleAlgorithm))
                        libplaceboFilter += $":downscaler={preset.DownsampleAlgorithm}";

                    // 添加抗振铃强度
                    if (!string.IsNullOrEmpty(preset.AntiRingingStrength))
                        libplaceboFilter += $":antiringing={preset.AntiRingingStrength}";

                    // 关键：所有shader作为custom_shader_path参数添加到同一个libplacebo
                    foreach (var shader in preset.ShaderList)
                    {
                        if (!string.IsNullOrEmpty(shader))
                            libplaceboFilter += $":custom_shader_path='{ConvertPathForFFmpegFilter(shader)}'";
                    }

                    videoFilters.Add(libplaceboFilter);
                }
                else
                {
                    // 使用传统缩放算法
                    var scaleFilter = $"scale={width}:{height}";

                    if (!string.IsNullOrEmpty(preset.UpsampleAlgorithm))
                        scaleFilter += $":flags={preset.UpsampleAlgorithm}";

                    videoFilters.Add(scaleFilter);
                }
            }
            // 分辨率设置（如果没有使用超分）
            else if (!string.IsNullOrEmpty(preset.VideoResolutionWidth) ||
                     !string.IsNullOrEmpty(preset.VideoResolutionHeight))
            {
                var width = preset.VideoResolutionWidth ?? "-1";
                var height = preset.VideoResolutionHeight ?? "-1";
                videoFilters.Add($"scale={width}:{height}");
            }
            else if (!string.IsNullOrEmpty(preset.VideoResolution))
            {
                videoFilters.Add($"scale={preset.VideoResolution}");
            }

            // 5. 色彩管理滤镜
            var colorFilters = new List<string>();

            if (!string.IsNullOrEmpty(preset.ColorMatrix))
                colorFilters.Add($"colormatrix={preset.ColorMatrix}");

            if (!string.IsNullOrEmpty(preset.ColorPrimaries))
                colorFilters.Add($"setparams=color_primaries={preset.ColorPrimaries}");

            if (!string.IsNullOrEmpty(preset.ColorTransfer))
                colorFilters.Add($"setparams=color_trc={preset.ColorTransfer}");

            if (!string.IsNullOrEmpty(preset.ColorRange))
                colorFilters.Add($"setparams=range={preset.ColorRange}");

            if (!string.IsNullOrEmpty(preset.TonemapAlgorithm))
                colorFilters.Add($"zscale=t=linear:npl=100,tonemap={preset.TonemapAlgorithm},zscale=t=bt709:m=bt709:r=tv");

            // 亮度/对比度/饱和度/伽马调整
            var eqParams = new List<string>();
            if (!string.IsNullOrEmpty(preset.Brightness))
                eqParams.Add($"brightness={preset.Brightness}");
            if (!string.IsNullOrEmpty(preset.Contrast))
                eqParams.Add($"contrast={preset.Contrast}");
            if (!string.IsNullOrEmpty(preset.Saturation))
                eqParams.Add($"saturation={preset.Saturation}");
            if (!string.IsNullOrEmpty(preset.Gamma))
                eqParams.Add($"gamma={preset.Gamma}");

            if (eqParams.Count > 0)
                colorFilters.Add($"eq={string.Join(":", eqParams)}");

            videoFilters.AddRange(colorFilters);

            // 6. 降噪滤镜 - 使用详细参数
            if (!string.IsNullOrEmpty(preset.DenoiseFilter))
            {
                if (preset.DenoiseFilter == "hqdn3d")
                {
                    var p1 = preset.DenoiseParam1 ?? "4";
                    var p2 = preset.DenoiseParam2 ?? "3";
                    var p3 = preset.DenoiseParam3 ?? "6";
                    var p4 = preset.DenoiseParam4 ?? "4.5";
                    videoFilters.Add($"hqdn3d={p1}:{p2}:{p3}:{p4}");
                }
                else if (preset.DenoiseFilter == "nlmeans")
                {
                    var strength = preset.DenoiseParam1 ?? "3.0";
                    videoFilters.Add($"nlmeans=s={strength}");
                }
                else
                    videoFilters.Add(preset.DenoiseFilter);
            }

            // 7. 锐化滤镜 - 使用详细参数
            if (!string.IsNullOrEmpty(preset.SharpenFilter))
            {
                if (preset.SharpenFilter == "unsharp")
                {
                    var lumaH = preset.SharpenLumaWidth ?? "5";
                    var lumaV = preset.SharpenLumaHeight ?? "5";
                    var lumaS = preset.SharpenStrength ?? "1.0";
                    videoFilters.Add($"unsharp={lumaH}:{lumaV}:{lumaS}:5:5:0.0");
                }
                else if (preset.SharpenFilter == "cas")
                    videoFilters.Add("cas=0.5");
                else
                    videoFilters.Add(preset.SharpenFilter);
            }

            // 8. 画面翻转
            if (preset.RotationAngle > 0)
            {
                var transposeValue = preset.RotationAngle switch
                {
                    1 => "1",  // 90度顺时针
                    2 => "2,transpose=2",  // 180度（两次90度）
                    3 => "2",  // 90度逆时针
                    _ => ""
                };
                if (!string.IsNullOrEmpty(transposeValue))
                    videoFilters.Add($"transpose={transposeValue}");
            }

            if (preset.FlipMirrorMode == 1)
                videoFilters.Add("hflip");  // 水平镜像
            else if (preset.FlipMirrorMode == 2)
                videoFilters.Add("vflip");  // 垂直镜像

            // 9. 去隔行
            if (!string.IsNullOrEmpty(preset.DeinterlaceMode))
                videoFilters.Add($"{preset.DeinterlaceMode}=mode=send_frame:parity=auto");

            // 10. 自定义视频滤镜
            if (!string.IsNullOrEmpty(preset.VideoFilter))
                videoFilters.Add(preset.VideoFilter);

            // 应用视频滤镜链
            if (videoFilters.Count > 0)
                args.Add($"-vf \"{string.Join(",", videoFilters)}\"");

            // 复杂滤镜（会覆盖-vf）
            if (!string.IsNullOrEmpty(preset.ComplexFilter))
                args.Add($"-filter_complex \"{preset.ComplexFilter}\"");

            // 帧率（如果没在滤镜中设置）
            if (!string.IsNullOrEmpty(preset.VideoFrameRate) &&
                string.IsNullOrEmpty(preset.InterpolationTargetFPS) &&
                string.IsNullOrEmpty(preset.BlendingTargetFPS))
            {
                args.Add($"-r {preset.VideoFrameRate}");
            }

            // 帧率最大变化
            if (!string.IsNullOrEmpty(preset.VideoFrameRateMaxChange))
                args.Add($"-vsync {preset.VideoFrameRateMaxChange}");

            // 音频编码器
            if (!string.IsNullOrEmpty(preset.AudioEncoder))
            {
                args.Add($"-c:a {preset.AudioEncoder}");

                // 音频比特率
                if (!string.IsNullOrEmpty(preset.AudioBitrate))
                {
                    var audioBitrate = preset.AudioBitrate;
                    // 如果已经包含单位后缀（k/K/M/G），则不再添加'k'
                    if (!audioBitrate.EndsWith("k", StringComparison.OrdinalIgnoreCase) &&
                        !audioBitrate.EndsWith("M", StringComparison.OrdinalIgnoreCase) &&
                        !audioBitrate.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                    {
                        audioBitrate += "k";
                    }
                    args.Add($"-b:a {audioBitrate}");
                }

                // 音频质量
                if (!string.IsNullOrEmpty(preset.AudioQualityParamName) &&
                    !string.IsNullOrEmpty(preset.AudioQualityValue))
                {
                    args.Add($"{preset.AudioQualityParamName} {preset.AudioQualityValue}");
                }

                // 采样率
                if (!string.IsNullOrEmpty(preset.AudioSampleRate))
                    args.Add($"-ar {preset.AudioSampleRate}");

                // 声道布局
                if (!string.IsNullOrEmpty(preset.AudioChannelLayout))
                    args.Add($"-channel_layout {preset.AudioChannelLayout}");

                // 声道数
                if (!string.IsNullOrEmpty(preset.AudioChannels))
                    args.Add($"-ac {preset.AudioChannels}");
            }

            // 自定义音频参数（支持通配符）
            if (!string.IsNullOrEmpty(preset.自定义音频参数))
            {
                var processedParam = ProcessCustomParamWildcards(preset.自定义音频参数, inputFile);
                if (!string.IsNullOrEmpty(processedParam))
                    args.Add(processedParam);
            }

            // 音频滤镜链
            var audioFilters = new List<string>();

            // 音量调整
            if (!string.IsNullOrEmpty(preset.AudioVolume))
                audioFilters.Add($"volume={preset.AudioVolume}");

            // 响度标准化
            if (!string.IsNullOrEmpty(preset.AudioNormalization))
                audioFilters.Add($"loudnorm=I={preset.AudioNormalization}:TP=-1.5:LRA=11");

            // 自定义音频滤镜
            if (!string.IsNullOrEmpty(preset.AudioFilter))
                audioFilters.Add(preset.AudioFilter);

            // 应用音频滤镜链
            if (audioFilters.Count > 0)
                args.Add($"-af \"{string.Join(",", audioFilters)}\"");

            // 字幕编码器
            if (!string.IsNullOrEmpty(preset.SubtitleEncoder))
                args.Add($"-c:s {preset.SubtitleEncoder}");

            // 字幕滤镜
            if (!string.IsNullOrEmpty(preset.SubtitleFilter))
                args.Add($"-vf \"{preset.SubtitleFilter}\"");

            // 字幕烧录模式
            if (!string.IsNullOrEmpty(preset.SubtitleBurnMode))
                args.Add($"-vf \"subtitles='{inputFile}':{preset.SubtitleBurnMode}\"");

            // 剪辑参数 - 结束时间和时长
            if (!string.IsNullOrEmpty(preset.ClipEndTime))
                args.Add($"-to {preset.ClipEndTime}");

            if (!string.IsNullOrEmpty(preset.ClipDuration))
                args.Add($"-t {preset.ClipDuration}");

            // 流控制
            if (preset.DiscardVideo)
                args.Add("-vn");

            if (preset.DiscardAudio)
                args.Add("-an");

            if (preset.DiscardSubtitle)
                args.Add("-sn");

            if (preset.DiscardData)
                args.Add("-dn");

            if (preset.DiscardAttachment)
                args.Add("-map -0:t");

            // 输出容器格式
            if (!string.IsNullOrEmpty(preset.OutputContainer))
                args.Add($"-f {preset.OutputContainer}");

            // 时间戳复制
            if (preset.CopyTimestamps)
                args.Add("-copyts");

            // Fast start（MP4优化）
            if (preset.FastStart)
                args.Add("-movflags +faststart");

            // 元数据
            if (!string.IsNullOrEmpty(preset.MetadataTitle))
                args.Add($"-metadata title=\"{preset.MetadataTitle}\"");

            if (!string.IsNullOrEmpty(preset.MetadataAuthor))
                args.Add($"-metadata author=\"{preset.MetadataAuthor}\"");

            if (!string.IsNullOrEmpty(preset.MetadataComment))
                args.Add($"-metadata comment=\"{preset.MetadataComment}\"");

            // 自定义参数（支持通配符）
            if (!string.IsNullOrEmpty(preset.AdditionalParams))
            {
                var processedParam = ProcessCustomParamWildcards(preset.AdditionalParams, inputFile);
                if (!string.IsNullOrEmpty(processedParam))
                    args.Add(processedParam);
            }

            // Post-output 自定义参数
            if (preset.PostOutputParams != null && preset.PostOutputParams.Count > 0)
            {
                foreach (var param in preset.PostOutputParams)
                {
                    if (!string.IsNullOrEmpty(param))
                        args.Add(param);
                }
            }

            // 覆盖输出文件
            if (preset.OverwriteOutput)
                args.Add("-y");

            // 输出文件
            args.Add($"\"{outputFile}\"");

            return string.Join(" ", args);
        }

        /// <summary>
        /// 获取默认预设目录
        /// </summary>
        public string GetDefaultPresetDirectory() => _defaultPresetDirectory;

        /// <summary>
        /// 将路径转换为FFmpeg滤镜接受的格式
        /// Windows反斜杠转换为正斜杠，并处理特殊字符
        /// </summary>
        private string ConvertPathForFFmpegFilter(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // 将Windows反斜杠转换为正斜杠
            var converted = path.Replace("\\", "/");

            // 移除单引号（会在外层添加）
            converted = converted.Replace("'", "");

            return converted;
        }

        /// <summary>
        /// 处理自定义参数中的通配符
        /// 支持：<InputFilePath>, <InputFilePathWithOutExtension>, <InputFileName>, <InputFileNameWithOutExtension>
        /// </summary>
        private string ProcessCustomParamWildcards(string customParam, string inputFile)
        {
            if (string.IsNullOrEmpty(customParam))
                return string.Empty;

            var result = customParam;

            // <InputFilePath> - 完整输入文件路径
            result = result.Replace("<InputFilePath>", inputFile);

            // <InputFilePathWithOutExtension> - 不含扩展名的完整路径
            result = result.Replace("<InputFilePathWithOutExtension>",
                Path.Combine(Path.GetDirectoryName(inputFile) ?? "",
                             Path.GetFileNameWithoutExtension(inputFile)));

            // <InputFileName> - 仅文件名（含扩展名）
            result = result.Replace("<InputFileName>", Path.GetFileName(inputFile));

            // <InputFileNameWithOutExtension> - 仅文件名（不含扩展名）
            result = result.Replace("<InputFileNameWithOutExtension>",
                Path.GetFileNameWithoutExtension(inputFile));

            return result;
        }
    }
}
