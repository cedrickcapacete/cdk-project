using System;
using System.Windows;
using System.Windows.Threading;

namespace HarborKiosk.App.Services;

public sealed class IdleTimeoutService
{
    private readonly NavigationService _navigation;
    private readonly KioskOptions _options;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _lastActivityUtc = DateTime.UtcNow;

    public IdleTimeoutService(NavigationService navigation, KioskOptions options)
    {
        _navigation = navigation;
        _options = options;
    }

    public void Start(UIElement root)
    {
        // Only deliberate actions count; mouse-move and cursor queries fire constantly and would keep sessions alive.
        root.PreviewMouseDown += (_, _) => RecordActivity();
        root.PreviewTouchDown += (_, _) => RecordActivity();
        root.PreviewKeyDown += (_, _) => RecordActivity();
        _navigation.PropertyChanged += (_, _) => RecordActivity();
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void RecordActivity() => _lastActivityUtc = DateTime.UtcNow;

    private void OnTick(object? sender, EventArgs e)
    {
        var current = _navigation.CurrentViewModel;
        if (current is null || !current.IsIdleTimeoutEnabled)
        {
            _lastActivityUtc = DateTime.UtcNow;
            return;
        }

        var limit = current.IdleTimeoutOverride ?? _options.IdleTimeout;
        if (DateTime.UtcNow - _lastActivityUtc >= limit)
        {
            _navigation.GoHome();
        }
    }
}
