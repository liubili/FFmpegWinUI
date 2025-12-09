using System.Collections.Generic;
using System.Linq;

namespace FFmpegWinUI.Models
{
    /// <summary>
    /// 单个编码器的数据结构
    /// </summary>
    public class EncoderData
    {
        public List<string> Preset { get; set; } = new List<string>();
        public List<string> Profile { get; set; } = new List<string>();
        public List<string> Tune { get; set; } = new List<string>();
        public List<string> PixFmt { get; set; } = new List<string>();
    }

    /// <summary>
    /// 视频编码器数据库 - 对应VB项目的视频编码器数据库
    /// 包含所有支持的编码器及其可用的预设、配置文件等
    /// </summary>
    public class EncoderDatabase
    {
        public static Dictionary<string, EncoderData> Dictionary { get; private set; } = new Dictionary<string, EncoderData>();

        /// <summary>
        /// 初始化编码器数据库
        /// </summary>
        public static void Initialize()
        {
            Dictionary.Clear();

            // Copy - 直接复制流
            Dictionary.Add("copy", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = new List<string> { "" }
            });

            // VVC编码器
            Dictionary.Add("libx266", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast", "superfast", "ultrafast" },
                Profile = new List<string> { "main", "main10" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv420p10le".Split(' ').ToList()
            });

            Dictionary.Add("libvvenc", new EncoderData
            {
                Preset = new List<string> { "slower", "slow", "medium", "fast", "faster" },
                Profile = new List<string> { "main", "main10" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv420p10le".Split(' ').ToList()
            });

            // AV1编码器
            Dictionary.Add("libaom-av1", new EncoderData
            {
                Preset = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8" },
                Profile = new List<string> { "0", "1", "2" },
                Tune = new List<string> { "psnr", "ssim", "qmt" },
                PixFmt = "yuv420p yuv422p yuv444p gbrp yuv420p10le yuv422p10le yuv444p10le yuv420p12le yuv422p12le yuv444p12le gbrp10le gbrp12le gray gray10le gray12le".Split(' ').ToList()
            });

            Dictionary.Add("libsvtav1", new EncoderData
            {
                Preset = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" },
                Profile = new List<string> { "main", "high", "professional" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv420p10le".Split(' ').ToList()
            });

            Dictionary.Add("av1_nvenc", new EncoderData
            {
                Preset = new List<string> { "p7", "p6", "p5", "p4", "p3", "p2", "p1" },
                Profile = new List<string> { "main", "high", "professional" },
                Tune = new List<string> { "hq", "uhq", "ll", "ull", "lossless" },
                PixFmt = "yuv420p nv12 p010le yuv444p p016le nv16 p210le p216le yuv444p10le yuv444p16le bgr0 bgra rgb0 rgba x2rgb10le x2bgr10le gbrp gbrp10le gbrp16le cuda d3d11".Split(' ').ToList()
            });

            Dictionary.Add("av1_amf", new EncoderData
            {
                Preset = new List<string> { "high_quality", "quality", "balanced", "speed" },
                Profile = new List<string> { "main" },
                Tune = new List<string> { "" },
                PixFmt = "nv12 yuv420p d3d11 dxva2_vld p010le amf bgr0 rgb0 bgra argb rgba x2bgr10le rgbaf16le".Split(' ').ToList()
            });

            Dictionary.Add("av1_qsv", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" },
                Profile = new List<string> { "main", "main10" },
                Tune = new List<string> { "" },
                PixFmt = "nv12 p010le qsv".Split(' ').ToList()
            });

            Dictionary.Add("librav1e", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuvj420p yuv420p10le yuv420p12le yuv422p yuvj422p yuv422p10le yuv422p12le yuv444p yuvj444p yuv444p10le yuv444p12le".Split(' ').ToList()
            });

            // HEVC/H.265编码器
            Dictionary.Add("libx265", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast", "superfast", "ultrafast" },
                Profile = new List<string> { "main", "mainstillpicture" },
                Tune = new List<string> { "psnr", "ssim", "grain", "fastdecode", "zerolatency", "animation", "stillimage" },
                PixFmt = "yuv420p yuvj420p yuv422p yuvj422p yuv444p yuvj444p gbrp yuv420p10le yuv422p10le yuv444p10le gbrp10le yuv420p12le yuv422p12le yuv444p12le gbrp12le gray gray10le gray12le yuva420p yuva420p10le".Split(' ').ToList()
            });

            Dictionary.Add("hevc_nvenc", new EncoderData
            {
                Preset = new List<string> { "p7", "p6", "p5", "p4", "p3", "p2", "p1" },
                Profile = new List<string> { "main", "rext" },
                Tune = new List<string> { "hq", "uhq", "ll", "ull", "lossless" },
                PixFmt = "yuv420p nv12 p010le yuv444p p016le nv16 p210le p216le yuv444p16le bgr0 bgra rgb0 rgba x2rgb10le x2bgr10le gbrp gbrp16le cuda d3d11".Split(' ').ToList()
            });

            Dictionary.Add("hevc_amf", new EncoderData
            {
                Preset = new List<string> { "quality", "balanced", "speed" },
                Profile = new List<string> { "main" },
                Tune = new List<string> { "transcoding", "ultralowlatency", "lowlatency", "webcam", "high_quality", "lowlatency_high_quality" },
                PixFmt = "nv12 yuv420p d3d11 dxva2_vld p010le amf bgr0 rgb0 bgra argb rgba x2bgr10le rgbaf16le".Split(' ').ToList()
            });

            Dictionary.Add("hevc_qsv", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" },
                Profile = new List<string> { "main", "mainsp", "rext", "scc" },
                Tune = new List<string> { "" },
                PixFmt = "nv12 p010le p012le yuyv422 y210le qsv bgra x2rgb10le vuyx xv30le".Split(' ').ToList()
            });

            Dictionary.Add("hevc_d3d12va", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "main", "main10" },
                Tune = new List<string> { "" },
                PixFmt = "d3d12".Split(' ').ToList()
            });

            Dictionary.Add("hevc_vulkan", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "main", "main10", "rext" },
                Tune = new List<string> { "hq", "ll", "ull", "lossless" },
                PixFmt = "vulkan".Split(' ').ToList()
            });

            // AVC/H.264编码器
            Dictionary.Add("libx264", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast", "superfast", "ultrafast" },
                Profile = new List<string> { "baseline", "main", "high", "high10", "high422", "high444" },
                Tune = new List<string> { "film", "animation", "grain", "stillimage", "psnr", "ssim", "fastdecode", "zerolatency" },
                PixFmt = "yuv420p yuvj420p yuv422p yuvj422p yuv444p yuvj444p nv12 nv16 nv21 yuv420p10le yuv422p10le yuv444p10le nv20le gray gray10le".Split(' ').ToList()
            });

            Dictionary.Add("h264_nvenc", new EncoderData
            {
                Preset = new List<string> { "p7", "p6", "p5", "p4", "p3", "p2", "p1" },
                Profile = new List<string> { "baseline", "main", "high", "high10", "high422", "high444p" },
                Tune = new List<string> { "hq", "ll", "ull", "lossless" },
                PixFmt = "yuv420p nv12 p010le yuv444p p016le nv16 p210le p216le yuv444p16le bgr0 bgra rgb0 rgba x2rgb10le x2bgr10le gbrp gbrp16le cuda d3d11".Split(' ').ToList()
            });

            Dictionary.Add("h264_amf", new EncoderData
            {
                Preset = new List<string> { "quality", "balanced", "speed" },
                Profile = new List<string> { "main", "high", "constrained_baseline", "constrained_high" },
                Tune = new List<string> { "transcoding", "ultralowlatency", "lowlatency", "webcam", "high_quality", "lowlatency_high_quality" },
                PixFmt = "nv12 yuv420p d3d11 dxva2_vld p010le amf bgr0 rgb0 bgra argb rgba x2bgr10le rgbaf16le".Split(' ').ToList()
            });

            Dictionary.Add("h264_qsv", new EncoderData
            {
                Preset = new List<string> { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" },
                Profile = new List<string> { "baseline", "main", "high" },
                Tune = new List<string> { "" },
                PixFmt = "nv12 qsv".Split(' ').ToList()
            });

            Dictionary.Add("h264_vulkan", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "main", "main10", "rext", "constrained_baseline" },
                Tune = new List<string> { "hq", "ll", "ull", "lossless" },
                PixFmt = "vulkan".Split(' ').ToList()
            });

            // ProRes编码器
            Dictionary.Add("prores_ks", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "auto", "proxy", "lt", "standard", "hq", "4444", "4444xq" },
                Tune = new List<string> { "" },
                PixFmt = "yuv422p10le yuv444p10le yuva444p10le".Split(' ').ToList()
            });

            Dictionary.Add("prores_aw", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "auto", "proxy", "lt", "standard", "hq", "4444", "4444xq" },
                Tune = new List<string> { "" },
                PixFmt = "yuv422p10le yuv444p10le yuva444p10le".Split(' ').ToList()
            });

            // EVC编码器
            Dictionary.Add("libxeve", new EncoderData
            {
                Preset = new List<string> { "default", "slow", "medium", "fast" },
                Profile = new List<string> { "main", "baseline" },
                Tune = new List<string> { "psnr", "zerolatency", "none" },
                PixFmt = "yuv420p yuv420p10le".Split(' ').ToList()
            });

            // VP9编码器
            Dictionary.Add("libvpx-vp9", new EncoderData
            {
                Preset = new List<string> { "0", "1", "2", "3", "4", "5" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "psnr", "ssim" },
                PixFmt = "yuv420p yuva420p yuv422p yuv440p yuv444p yuv420p10le yuv422p10le yuv440p10le yuv444p10le yuv420p12le yuv422p12le yuv440p12le yuv444p12le gbrp gbrp10le gbrp12le".Split(' ').ToList()
            });

            // VP8编码器
            Dictionary.Add("libvpx", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuva420p".Split(' ').ToList()
            });

            // 其他常用编码器
            Dictionary.Add("mpeg4", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv422p yuv444p".Split(' ').ToList()
            });

            Dictionary.Add("mjpeg", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuvj420p yuvj422p yuvj444p".Split(' ').ToList()
            });

            Dictionary.Add("png", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "rgb24 rgba rgb48be rgba64be pal8 gray ya8 gray16be ya16be monob".Split(' ').ToList()
            });

            // 专业无损/中间格式编码器
            Dictionary.Add("dnxhd", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "dnxhd", "dnxhr_lb", "dnxhr_sq", "dnxhr_hq", "dnxhr_hqx", "dnxhr_444" },
                Tune = new List<string> { "" },
                PixFmt = "yuv422p yuv422p10le yuv444p10le gbrp10le".Split(' ').ToList()
            });

            Dictionary.Add("utvideo", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv422p yuv444p bgra rgba gbrp gbrap".Split(' ').ToList()
            });

            Dictionary.Add("ffv1", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuva420p yuva422p yuv444p yuva444p yuv440p yuv422p yuv411p yuv410p bgr0 bgra yuv420p16le yuv422p16le yuv444p16le yuv444p9le yuv422p9le yuv420p9le yuv420p10le yuv422p10le yuv444p10le yuv420p12le yuv422p12le yuv444p12le yuv440p10le yuv440p12le yuva444p16le yuva422p16le yuva420p16le yuva444p10le yuva422p10le yuva420p10le yuva444p9le yuva422p9le yuva420p9le gray16le gray gbrp gbrp9le gbrp10le gbrp12le gbrp14le gbrp16le gbrap gbrap10le gbrap12le gbrap14le gbrap16le".Split(' ').ToList()
            });

            Dictionary.Add("huffyuv", new EncoderData
            {
                Preset = new List<string> { "" },
                Profile = new List<string> { "" },
                Tune = new List<string> { "" },
                PixFmt = "yuv420p yuv422p yuv444p yuva420p yuva422p yuva444p bgr24 bgra rgb24 rgba gbrap".Split(' ').ToList()
            });
        }

        /// <summary>
        /// 获取编码器列表
        /// </summary>
        public static List<string> GetEncoderList()
        {
            if (Dictionary.Count == 0)
                Initialize();

            return Dictionary.Keys.ToList();
        }

        /// <summary>
        /// 获取指定编码器的数据
        /// </summary>
        public static EncoderData? GetEncoderData(string encoderName)
        {
            if (Dictionary.Count == 0)
                Initialize();

            return Dictionary.TryGetValue(encoderName, out var data) ? data : null;
        }

        /// <summary>
        /// 检查编码器是否支持
        /// </summary>
        public static bool IsEncoderSupported(string encoderName)
        {
            if (Dictionary.Count == 0)
                Initialize();

            return Dictionary.ContainsKey(encoderName);
        }
    }
}
