using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using YouTube_Video_Downloader.Views;

namespace YouTube_Video_Downloader
{
    public partial class App : Application
    {
        public static ServiceProvider Services
        {
            get;
            private set;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            Services = serviceCollection.BuildServiceProvider();

            var mainWindow = new MainView
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };

            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(
            IServiceCollection services)
        {
            // SERVICES

            services.AddSingleton<IYouTubeService, YouTubeService>();
            services.AddSingleton<IFfmpegService, FfmpegService>();
            services.AddSingleton<IDialogService, DialogService>();

            // VIEWMODELS

            services.AddSingleton<MainViewModel>();

            // VIEWS

            services.AddSingleton<MainView>();
        }
    }
}