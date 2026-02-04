using Microsoft.Extensions.DependencyInjection;
using Recom.SQLConsole.Services;
using Recom.SQLConsole.ViewModels;
using Recom.SQLConsole.Views;

namespace Recom.SQLConsole.DI;

// https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/ioc

public static class Dependencies
{
    static Dependencies() => Services = ConfigureServices().ConfigureNavigation();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public static IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IGitService, GitService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ResultViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private static IServiceProvider ConfigureNavigation(this IServiceProvider services)
    {
        var nav = services.GetService<INavigationService>()!;

        nav.Register<MainViewModel, MainWindow>();
        nav.Register<ResultViewModel, ResultWindow>();
        nav.Register<SettingsViewModel, SettingsWindow>();

        return services;
    }

    /// <summary>
    /// Gets a service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The service instance or null if not found.</returns>
    public static T? Get<T>() => Services.GetService<T>();
}