#define MOCKING
//#define DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using PhotoPasser.Primitive;
using PhotoPasser.Service;
using PhotoPasser.Service.Primitive;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        
        private MainWindow? _window;
        private static IHost? _host;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            _host = Host.CreateDefaultBuilder()
                        .UseContentRoot(AppContext.BaseDirectory)
                        .UseEnvironment("Development")
                        .ConfigureServices((services) =>
                        {
                            services.AddSingleton<ConvertingService>(x =>
                            {
                                return new ConvertingService()
                                    .Register<string, SortBy>(x => Enum.Parse<SortBy>(x))
                                    .Register<string, DisplayView>(x => Enum.Parse<DisplayView>(x))
                                    .Register<string, SortOrder>(x => Enum.Parse<SortOrder>(x));
                            });
#if MOCKING
                            services.AddSingleton<ITaskItemProviderService, MockTaskItemProviderService>();
                            services.AddScoped<ITaskDetailPhysicalManagerService, MockTaskDetailPhysicalManagerService>();
#else
                            services.AddSingleton<ITaskItemProviderService, TaskItemProviderService>();
                            services.AddScoped<ITaskDetailPhysicalManagerService, TaskDetailPhysicalManagerService>();
#endif
                            services.AddTransient<IDialogService, DialogService>();
                            services.AddTransient<IClipboardService, ClipboardService>();
                            services.AddSingleton<TaskOverview>()
                                    .AddSingleton<TaskOverviewViewModel>();
                        })
                        .Build();

            UnhandledException += App_UnhandledException;
            Current = this;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (e.Exception != null)
            {
                ContentDialog exceptionDialog = new()
                {
                    Content = e.Exception.ToString(),
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = MainWindow.Content.XamlRoot
                };

                await exceptionDialog.ShowAsync();
            }
        }

        public static T? GetService<T>()
            where T : class
        {
            return _host?.Services.GetService<T>();
        }

        public static IServiceScope CreateScope()
        {
            return _host!.Services.CreateScope();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            SettingProvider.ApplySetting();
            _window.Closed += (s, e) =>
            {
                m_exitProcess.Invoke(this, EventArgs.Empty);
                CancellationToken cancellationToken = CancellationToken.None;
                cancellationToken.WaitHandle.WaitOne(100);
            };
            _window.Activate();
        }

        private EventHandlerWrapper<EventHandler> m_exitProcess = EventHandlerWrapper<EventHandler>.Create();
        public event EventHandler ExitProcess
        {
            add
            {
                m_exitProcess.AddHandler(value);
            }
            remove
            {
                m_exitProcess.RemoveHandler(value);
            }
        }

        public static new App Current { get; private set;  }
        public MainWindow MainWindow => _window;
        public IHost HostInstance => _host!;
    }
}
