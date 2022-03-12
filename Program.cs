using Avalonia;
using Avalonia.ReactiveUI;
using Mapster.ViewModels;
using Mapster.Views;
using ReactiveUI;
using Serilog;
using Splat;
using System;

namespace Mapster
{
    internal class Program
    {

        [STAThread]
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File("logs/log.txt", rollOnFileSizeLimit: true, fileSizeLimitBytes: 1_000_000).CreateLogger();
            Log.Information("Starting the application.");

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {

            Locator.CurrentMutable.Register(() => new LoadingScreenView(), typeof(IViewFor<LoadingScreenViewModel>));

            return AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }
    }
}
