using System;
using BalanceAval.Service;
using BalanceAval.ViewModels;
using Splat;

namespace BalanceAval
{
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