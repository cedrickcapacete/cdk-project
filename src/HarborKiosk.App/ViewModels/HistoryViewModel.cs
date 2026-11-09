using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public sealed record DepositRow(string Amount, string Date, string Reference, string Status);

public sealed partial class HistoryViewModel : StepViewModelBase
{
    private readonly DepositService _depositService;
    private readonly DepositSession _session;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeposits))]
    private IReadOnlyList<DepositRow> _deposits = Array.Empty<DepositRow>();

    public HistoryViewModel(NavigationService navigation, DepositService deposits, DepositSession session)
        : base(navigation)
    {
        _depositService = deposits;
        _session = session;
    }

    public bool HasDeposits => Deposits.Count > 0;

    public string AccountText => _session.Account is { } account
        ? $"Account {KioskFormat.MaskAccountNumber(account.AccountNumber)}"
        : string.Empty;

    public override string FlowLabel => "DEPOSIT HISTORY";

    public override string StepText => "RECENT ACTIVITY";

    public override double Progress => 1;

    public override void OnNavigatedTo()
    {
        if (_session.Account is not { } account)
        {
            Navigation.GoHome();
            return;
        }

        Deposits = _depositService.GetRecentDeposits(account, limit: 6)
            .Select(d => new DepositRow(
                KioskFormat.Money(d.AmountCents),
                KioskFormat.Date(d.CreatedAt),
                d.Reference,
                d.Status switch
                {
                    DepositStatus.Accepted => "Deposited",
                    DepositStatus.Returned => "Returned",
                    _ => $"Available {KioskFormat.Date(d.FundsAvailableOn)}",
                }))
            .ToList();
    }

    [RelayCommand]
    private void Done() => Navigation.GoHome();
}
