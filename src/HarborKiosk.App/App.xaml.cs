using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HarborKiosk.App.Services;
using HarborKiosk.App.ViewModels;
using HarborKiosk.Core.Data;
using HarborKiosk.Core.Hardware;
using HarborKiosk.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HarborKiosk.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = KioskOptions.FromArgs(e.Args);
        ErrorLog.Initialize(options.DataDirectory);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => ErrorLog.Write(args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLog.Write(args.Exception);
            args.SetObserved();
        };

        _services = ConfigureServices(options);

        var database = _services.GetRequiredService<KioskDatabase>();
        database.Initialize();
        if (options.SeedDemoAccounts)
        {
            database.SeedDemoAccounts();
        }

        var window = _services.GetRequiredService<MainWindow>();
        _services.GetRequiredService<NavigationService>().GoHome();
        _services.GetRequiredService<IdleTimeoutService>().Start(window);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices(KioskOptions options)
    {
        var services = new ServiceCollection();

        services.AddSingleton(options);
        services.AddSingleton(new KioskDatabase(Path.Combine(options.DataDirectory, "kiosk.db")));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(new DepositPolicy(options.MaxDepositCents, Path.Combine(options.DataDirectory, "cheque-images")));
        services.AddSingleton<AccountService>();
        services.AddSingleton<DepositService>();

        // Swap these for vendor implementations when the kiosk hardware is connected.
        services.AddSingleton<ICheckScanner, SimulatedCheckScanner>();
        services.AddSingleton<IReceiptPrinter>(_ => options.PrintToDefaultPrinter
            ? new WindowsReceiptPrinter()
            : new FileReceiptPrinter(Path.Combine(options.DataDirectory, "receipts")));

        services.AddSingleton<DepositSession>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<IdleTimeoutService>();

        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<AccountNumberViewModel>();
        services.AddTransient<AmountViewModel>();
        services.AddTransient<InsertChequeViewModel>();
        services.AddTransient<CompleteViewModel>();
        services.AddTransient<ThankYouViewModel>();
        services.AddTransient<HistoryViewModel>();

        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // A kiosk must never crash to the desktop: log, then reset to the welcome screen.
        ErrorLog.Write(e.Exception);
        e.Handled = true;
        _services?.GetService<NavigationService>()?.GoHome();
    }
}
