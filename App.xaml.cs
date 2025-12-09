using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using FFmpegWinUI.FFmpegService;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FFmpegWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static IFfmpegService FfmpegService { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                InitializeComponent();
                UnhandledException += App_UnhandledException;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App构造函数异常: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // 首先初始化服务
                FfmpegService = new FfmpegService(AppContext.BaseDirectory);

                MainWindow = new MainWindow();

                // 初始化服务容器（在窗口创建后，因为需要 DispatcherQueue）
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                Services.ServiceContainer.Instance.Initialize(dispatcherQueue);

                MainWindow.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnLaunched异常: {ex}");
                System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // 输出详细的异常信息
            System.Diagnostics.Debug.WriteLine($"未处理的异常: {e.Exception}");
            System.Diagnostics.Debug.WriteLine($"异常消息: {e.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {e.Exception.StackTrace}");
            if (e.Exception.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"内部异常: {e.Exception.InnerException}");
            }
            e.Handled = true;
        }
    }
}
