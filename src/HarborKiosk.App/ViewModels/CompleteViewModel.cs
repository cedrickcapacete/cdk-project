using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;

namespace HarborKiosk.App.ViewModels;

public sealed partial class CompleteViewModel : StepViewModelBase
{
    private readonly Deposit? _deposit;

    public CompleteViewModel(NavigationService navigation, DepositSession session)
        : base(navigation)
    {
        _deposit = session.Deposit;
    }

    public string AmountText => _deposit is null ? string.Empty : KioskFormat.Money(_deposit.AmountCents);

    public string MaskedAccount => _deposit is null ? string.Empty : KioskFormat.MaskAccountNumber(_deposit.AccountNumber);

    public string DateText => _deposit is null ? string.Empty : KioskFormat.Date(_deposit.CreatedAt);

    public string Reference => _deposit?.Reference ?? string.Empty;

    public string FundsText => _deposit is null
        ? string.Empty
        : $"Usually within 1–2 business days (by {KioskFormat.Date(_deposit.FundsAvailableOn)})";

    public override string FlowLabel => "CHEQUE DEPOSIT";

    public override string StepText => "STEP 4 OF 4";

    public override double Progress => 1;

    public override void OnNavigatedTo()
    {
        if (_deposit is null)
        {
            Navigation.GoHome();
        }
    }

    [RelayCommand]
    private void GetReceipt() => Navigation.NavigateTo<ThankYouViewModel>();
}
