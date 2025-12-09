using System;
using System.IO;
using System.Threading.Tasks;
using FFmpegWinUI.Models;
using Windows.Storage;

namespace FFmpegWinUI.Services
{
    /// <summary>
    /// 设置服务接口
    /// </summary>
    public interface ISettingsService
    {
        Task<UserSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(UserSettings settings);
        Task<string> GetSettingsFilePathAsync();
        void ApplyTheme(string theme);
    }

    /// <summary>
    /// 设置服务 - 对应VB项目的用户设置.vb
    /// 负责用户设置的加载、保存和应用
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private UserSettings? _cachedSettings;

        public SettingsService()
        {
            // 设置文件路径：应用数据目录下的Settings.json
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _settingsFilePath = Path.Combine(localFolder, "Settings.json");
        }

        /// <summary>
        /// 加载用户设置
        /// </summary>
        public async Task<UserSettings> LoadSettingsAsync()
        {
            try
            {
                // 如果有缓存，直接返回
                if (_cachedSettings != null)
                    return _cachedSettings;

                // 尝试从文件加载
                if (File.Exists(_settingsFilePath))
                {
                    _cachedSettings = await Task.Run(() => UserSettings.LoadFromFile(_settingsFilePath));
                }
                else
                {
                    // 创建默认设置
                    _cachedSettings = CreateDefaultSettings();
                    await SaveSettingsAsync(_cachedSettings);
                }

                return _cachedSettings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                // 返回默认设置
                _cachedSettings = CreateDefaultSettings();
                return _cachedSettings;
            }
        }

        /// <summary>
        /// 保存用户设置
        /// </summary>
        public async Task SaveSettingsAsync(UserSettings settings)
        {
            try
            {
                await Task.Run(() => settings.SaveToFile(_settingsFilePath));
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                throw new Exception($"保存设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取设置文件路径
        /// </summary>
        public async Task<string> GetSettingsFilePathAsync()
        {
            return await Task.FromResult(_settingsFilePath);
        }

        /// <summary>
        /// 应用主题设置
        /// </summary>
        public void ApplyTheme(string theme)
        {
            try
            {
                var requestedTheme = theme switch
                {
                    "Light" => Microsoft.UI.Xaml.ElementTheme.Light,
                    "Dark" => Microsoft.UI.Xaml.ElementTheme.Dark,
                    _ => Microsoft.UI.Xaml.ElementTheme.Default
                };

                // 获取主窗口并应用主题
                if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = requestedTheme;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建默认设置
        /// </summary>
        private UserSettings CreateDefaultSettings()
        {
            var settings = new UserSettings
            {
                字体 = "Segoe UI",
                自动同时运行任务数量选项 = 0, // 默认1个任务
                有任务时系统保持状态选项 = 0, // 默认保持唤醒
                提示音选项 = 0, // 默认启用提示音
                自动开始任务选项 = 0, // 默认自动开始
                主题设置 = "System", // 默认跟随系统
                启用硬件加速 = true,
                自动检查更新 = true,
                是否参与用户统计 = true
            };

            return settings;
        }

        /// <summary>
        /// 清除缓存（用于重新加载设置）
        /// </summary>
        public void ClearCache()
        {
            _cachedSettings = null;
        }

        /// <summary>
        /// 获取当前缓存的设置（如果有）
        /// </summary>
        public UserSettings? GetCachedSettings()
        {
            return _cachedSettings;
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public async Task ResetToDefaultAsync()
        {
            _cachedSettings = CreateDefaultSettings();
            await SaveSettingsAsync(_cachedSettings);
        }
    }
}
