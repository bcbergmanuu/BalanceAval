using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
}
