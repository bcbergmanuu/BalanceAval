using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BalanceAval.Service;
using BalanceAval.ViewModels;
using BalanceAval.Views;
using Splat;

namespace BalanceAval
{
    public class App : Application
    {
        public override void Initialize()
        {
            Bootstrapper.Register(Locator.CurrentMutable, Locator.Current);
            AvaloniaXamlLoader.Load(this);

        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DataContext = GetRequiredService<IMainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = DataContext
                                                                                    //DataContext = new MainWindowViewModel(),
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
        private static T GetRequiredService<T>() => Locator.Current.GetRequiredService<T>();
    }



    public static class Bootstrapper
    {
        public static void Register(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver)
        {
            services.Register<IReadNidaq>(() => new ReadNidaq());  // Call services.Register<T> and pass it lambda that creates instance of your service

            services.Register<IMainWindowViewModel>(() => new MainWindowViewModel(
                resolver.GetRequiredService<IReadNidaq>()
            ));

        }

        public static TService GetRequiredService<TService>(this IReadonlyDependencyResolver resolver)
        {
            var service = resolver.GetService<TService>();
            if (service is null) // Splat is not able to resolve type for us
            {
                throw new InvalidOperationException($"Failed to resolve object of type {typeof(TService)}"); // throw error with detailed description
            }

            return service; // return instance if not null
        }

    }
}
