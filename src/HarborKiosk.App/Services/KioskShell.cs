using System;

namespace HarborKiosk.App.Services;

/// <summary>App-level actions the admin screen can request without holding a reference to the window.</summary>
public sealed class KioskShell
{
    public event EventHandler? ExitRequested;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

    public string Version { get; } =
        typeof(KioskShell).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);
}
