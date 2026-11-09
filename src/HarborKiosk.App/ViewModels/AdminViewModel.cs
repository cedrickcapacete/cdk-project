using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public enum ReportPeriod
{
    Today,
    Last7Days,
    Last30Days,
}

public sealed record AdminDepositRow(string Amount, string When, string Reference, string Account, string Status);

public sealed partial class AdminViewModel : StepViewModelBase
{
    private const int RecentRowCount = 5;

    private readonly DepositService _deposits;
    private readonly IClock _clock;
    private readonly KioskOptions _options;
    private readonly KioskShell _shell;
    private DepositReport? _report;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsToday), nameof(IsLast7Days), nameof(IsLast30Days))]
    private ReportPeriod _period = ReportPeriod.Today;

    [ObservableProperty]
    private string _periodText = string.Empty;

    [ObservableProperty]
    private string _countText = "0";

    [ObservableProperty]
    private string _totalText = KioskFormat.Money(0);

    [ObservableProperty]
    private string _averageText = KioskFormat.Money(0);

    [ObservableProperty]
    private string _pendingText = "0";

    [ObservableProperty]
    private string _receiptsText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentDeposits))]
    private IReadOnlyList<AdminDepositRow> _recentDeposits = Array.Empty<AdminDepositRow>();

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CloseButtonText))]
    private bool _isConfirmingClose;

    public AdminViewModel(NavigationService navigation, DepositService deposits, IClock clock, KioskOptions options, KioskShell shell)
        : base(navigation)
    {
        _deposits = deposits;
        _clock = clock;
        _options = options;
        _shell = shell;
    }

    public override string FlowLabel => "STAFF ACCESS";

    public override string StepText => "KIOSK ADMIN";

    public override double Progress => 1;

    public override TimeSpan? IdleTimeoutOverride => TimeSpan.FromMinutes(2);

    public bool IsToday => Period == ReportPeriod.Today;

    public bool IsLast7Days => Period == ReportPeriod.Last7Days;

    public bool IsLast30Days => Period == ReportPeriod.Last30Days;

    public bool HasRecentDeposits => RecentDeposits.Count > 0;

    public string CloseButtonText => IsConfirmingClose ? "Tap again to close" : "Close app";

    public string KioskInfo =>
        $"{_options.KioskId}  ·  v{_shell.Version}  ·  {(_options.KioskMode ? "Kiosk mode" : "Windowed")}  ·  up since {_shell.StartedAt:MMM d, h:mm tt}";

    public string PinWarning => _options.AdminPin == KioskOptions.DefaultAdminPin
        ? "Default admin PIN in use. Set --admin-pin or HARBOR_ADMIN_PIN before deployment."
        : string.Empty;

    public bool HasPinWarning => PinWarning.Length > 0;

    public override void OnNavigatedTo() => Refresh();

    partial void OnPeriodChanged(ReportPeriod value) => Refresh();

    [RelayCommand]
    private void SelectPeriod(ReportPeriod period)
    {
        StatusMessage = null;
        Period = period;
    }

    [RelayCommand]
    private void Refresh()
    {
        IsConfirmingClose = false;
        var to = DateOnly.FromDateTime(_clock.Now.Date);
        var from = Period switch
        {
            ReportPeriod.Last7Days => to.AddDays(-6),
            ReportPeriod.Last30Days => to.AddDays(-29),
            _ => to,
        };

        var report = DepositReport.Build(_deposits.GetDepositsBetween(from, to), from, to);
        _report = report;

        PeriodText = from == to
            ? KioskFormat.Date(to)
            : $"{KioskFormat.Date(from)} – {KioskFormat.Date(to)}";
        CountText = report.Count.ToString("N0");
        TotalText = KioskFormat.Money(report.TotalCents);
        AverageText = KioskFormat.Money(report.AverageCents);
        PendingText = report.PendingCount.ToString("N0");
        ReceiptsText = report.ReceiptsPrinted.ToString("N0");

        RecentDeposits = _deposits.GetLatestDeposits(RecentRowCount)
            .Select(d => new AdminDepositRow(
                KioskFormat.Money(d.AmountCents),
                KioskFormat.DateTime(d.CreatedAt),
                d.Reference,
                KioskFormat.MaskAccountNumber(d.AccountNumber),
                d.Status == DepositStatus.Pending ? "Pending" : d.Status.ToString()))
            .ToList();
    }

    [RelayCommand]
    private void ExportReport()
    {
        IsConfirmingClose = false;
        if (_report is null)
        {
            return;
        }

        var directory = Path.Combine(_options.DataDirectory, "reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"deposits-{_report.From:yyyyMMdd}-{_report.To:yyyyMMdd}-{_clock.Now:HHmmss}.csv");
        File.WriteAllText(path, _report.ToCsv());
        StatusMessage = $"Exported {_report.Count} deposit(s) to {path}";
    }

    [RelayCommand]
    private void CloseApp()
    {
        if (!IsConfirmingClose)
        {
            IsConfirmingClose = true;
            return;
        }

        _shell.RequestExit();
    }

    [RelayCommand]
    private void ReturnToKiosk() => Navigation.GoHome();
}
