using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FFmpegWinUI.Models
{
    /// <summary>
    /// 自动加载预设选项枚举
    /// </summary>
    public enum AutoLoadPresetOption
    {
        不自动加载预设 = 0,
        自动加载最后的预设文件 = 1,
        自动加载指定的预设文件 = 2,
        自动加载上次的全部改动 = 3
    }

    /// <summary>
    /// 用户设置 - 对应VB项目的用户设置数据结构
    /// </summary>
    [Serializable]
    public class UserSettings
    {
        // 界面设置
        public string 字体 { get; set; } = "Segoe UI";

        // 任务管理
        public string 指定处理器核心 { get; set; } = string.Empty;
        public int 自动同时运行任务数量选项 { get; set; } = 0; // 0=1个, 1=2个, etc.
        public int 有任务时系统保持状态选项 { get; set; } = 0; // 0=保持唤醒, 1=允许睡眠
        public int 提示音选项 { get; set; } = 0; // 0=启用, 1=禁用
        public int 自动开始任务选项 { get; set; } = 0; // 0=自动开始, 1=手动开始
        public int 自动重置参数面板的页面选择 { get; set; } = 0;
        public int 混淆任务名称 { get; set; } = 0;

        // FFmpeg设置
        public string 工作目录 { get; set; } = string.Empty;
        public string 替代进程文件名 { get; set; } = string.Empty;
        public string 覆盖参数传递 { get; set; } = string.Empty;
        public bool 转译模式 { get; set; } = false;

        // 预设管理
        public AutoLoadPresetOption 自动加载预设选项 { get; set; } = AutoLoadPresetOption.不自动加载预设;
        public string 自动加载预设文件路径 { get; set; } = string.Empty;
        public PresetData? 最后的预设数据 { get; set; }

        // 统计和隐私
        public bool 是否参与用户统计 { get; set; } = true;
        public DateTime 上次回报活跃信息的日期 { get; set; } = DateTime.MinValue;

        // 自定义编码器
        public List<string> 自定义视频编码器列表 { get; set; } = new List<string>();

        // 个性化
        public string 个性化_软件图标 { get; set; } = string.Empty;
        public string 个性化_任务完成音效 { get; set; } = string.Empty;
        public string 个性化_任务失败音效 { get; set; } = string.Empty;
        public string 个性化_起始页标题 { get; set; } = string.Empty;
        public string 个性化_起始页副标题 { get; set; } = string.Empty;
        public string 个性化_窗口标题栏 { get; set; } = string.Empty;
        public string 个性化_起始页背景图 { get; set; } = string.Empty;

        // WinUI特有设置
        public string 主题设置 { get; set; } = "System"; // System, Light, Dark
        public bool 启用硬件加速 { get; set; } = true;
        public bool 自动检查更新 { get; set; } = true;
        public string 默认输出路径 { get; set; } = string.Empty;

        // 计算属性
        public int 同时运行任务上限
        {
            get
            {
                return 自动同时运行任务数量选项 switch
                {
                    0 => 1,
                    1 => 2,
                    2 => 3,
                    3 => 4,
                    4 => 5,
                    5 => 6,
                    6 => 7,
                    7 => 8,
                    8 => 9,
                    9 => 10,
                    _ => 1
                };
            }
        }

        // JSON序列化选项
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        /// <summary>
        /// 保存设置到JSON文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(this, _jsonOptions);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从JSON文件加载设置
        /// </summary>
        public static UserSettings LoadFromFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return new UserSettings();

                var json = System.IO.File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<UserSettings>(json, _jsonOptions) ?? new UserSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                return new UserSettings();
            }
        }
    }
}
