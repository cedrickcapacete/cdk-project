using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using HarborKiosk.App.ViewModels;

namespace HarborKiosk.App.Views;

public partial class WelcomeView : UserControl
{
    private readonly DispatcherTimer _holdTimer = new() { Interval = TimeSpan.FromSeconds(3) };

    public WelcomeView()
    {
        InitializeComponent();
        _holdTimer.Tick += OnHoldElapsed;
        Unloaded += (_, _) => _holdTimer.Stop();
    }

    private void OnHotspotDown(object sender, MouseButtonEventArgs e)
    {
        AdminHotspot.CaptureMouse();
        _holdTimer.Start();
    }

    private void OnHotspotUp(object sender, RoutedEventArgs e)
    {
        _holdTimer.Stop();
        if (AdminHotspot.IsMouseCaptured)
        {
            AdminHotspot.ReleaseMouseCapture();
        }
    }

    private void OnHoldElapsed(object? sender, EventArgs e)
    {
        OnHotspotUp(this, new RoutedEventArgs());
        (DataContext as WelcomeViewModel)?.OpenAdminCommand.Execute(null);
    }
}
