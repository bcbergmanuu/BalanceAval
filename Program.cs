using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using Splat;

namespace BalanceAval
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.

        public static readonly string UserPath =  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BalanceApp");
        public static void Main(string[] args)
        {
            if (!Directory.Exists(UserPath))
                Directory.CreateDirectory(UserPath);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
            
                ;

        
    }
}
