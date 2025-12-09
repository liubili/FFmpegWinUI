using FFmpegWinUI.Services;
using FFmpegWinUI.ViewModels;
using Microsoft.UI.Dispatching;
using System;
using System.IO;

namespace FFmpegWinUI.Services
{
    /// <summary>
    /// 简单的服务容器 - 管理应用级单例服务
    /// 解决原项目中通过全局 Form1 访问所有页面的问题
    /// </summary>
    public class ServiceContainer
    {
        private static ServiceContainer? _instance;
        public static ServiceContainer Instance => _instance ??= new ServiceContainer();

        // 核心服务单例
        public IPresetService PresetService { get; private set; } = null!;
        public IEncodingTaskService EncodingTaskService { get; private set; } = null!;
        public IMediaInfoService MediaInfoService { get; private set; } = null!;

        // 共享的 ViewModel 实例（用于页面间通信）
        // 关键：让 FilesPage、ParametersPage、QueuePage 共享同一个实例
        public FilesPageViewModel FilesPageViewModel { get; private set; } = null!;
        public ParametersPageViewModel ParametersPageViewModel { get; private set; } = null!;
        public QueuePageViewModel QueuePageViewModel { get; private set; } = null!;

        private ServiceContainer()
        {
            // 私有构造函数，防止外部实例化
        }

        /// <summary>
        /// 初始化所有服务（在 App.xaml.cs 中调用）
        /// </summary>
        public void Initialize(DispatcherQueue dispatcherQueue)
        {
            // 1. 创建底层服务
            PresetService = new PresetService();
            MediaInfoService = new MediaInfoService();

            // 获取 ffmpeg 路径
            var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            EncodingTaskService = new EncodingTaskService(PresetService, ffmpegPath);

            // 2. 创建共享的 ViewModel 实例
            // 这样三个页面访问的都是同一个实例，状态自然共享
            FilesPageViewModel = new FilesPageViewModel(MediaInfoService, dispatcherQueue);
            ParametersPageViewModel = new ParametersPageViewModel(PresetService, dispatcherQueue);
            QueuePageViewModel = new QueuePageViewModel(EncodingTaskService, dispatcherQueue);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            // 停止所有运行中的任务
            QueuePageViewModel?.StopAllTasksCommand.Execute(null);
        }
    }
}
