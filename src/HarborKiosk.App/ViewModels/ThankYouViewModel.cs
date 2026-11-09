using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Hardware;
using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public sealed partial class ThankYouViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly IReceiptPrinter _printer;
    private readonly DepositService _deposits;
    private readonly KioskOptions _options;
    private readonly Deposit? _deposit;

    [ObservableProperty]
    private string _message = "Printing your receipt…";

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private bool _isReceiptReady;

    public ThankYouViewModel(NavigationService navigation, IReceiptPrinter printer, DepositService deposits,
        DepositSession session, KioskOptions options)
    {
        _navigation = navigation;
        _printer = printer;
        _deposits = deposits;
        _options = options;
        _deposit = session.Deposit;
    }

    public string AmountText => _deposit is null ? string.Empty : KioskFormat.Money(_deposit.AmountCents);

    public string ReferenceText => _deposit is null ? string.Empty : $"REF {_deposit.Reference}";

    public override TimeSpan? IdleTimeoutOverride => _options.ReceiptScreenTimeout;

    public override void OnNavigatedTo()
    {
        if (_deposit is null)
        {
            _navigation.GoHome();
            return;
        }

        _ = PrintReceiptAsync(_deposit);
    }

    [RelayCommand]
    private void Done() => _navigation.GoHome();

    private async Task PrintReceiptAsync(Deposit deposit)
    {
        try
        {
            await _printer.PrintAsync(deposit.Reference, ReceiptFormatter.Build(deposit, _options.KioskId));
            await Task.Run(() => _deposits.MarkReceiptPrinted(deposit.Id));
            Message = "Your receipt is ready. Please take it from the slot below.";
            Prompt = "Please take your receipt";
            IsReceiptReady = true;
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            Message = $"We couldn't print your receipt. Your deposit was received — keep reference {deposit.Reference} for your records.";
            Prompt = string.Empty;
        }
    }
}
