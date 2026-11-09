using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public sealed partial class AmountViewModel : KeypadStepViewModelBase
{
    private const long MaxEntryCents = 99_999_999;

    private readonly DepositService _deposits;
    private readonly DepositSession _session;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmountText))]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private long _amountCents;

    [ObservableProperty]
    private string? _errorMessage;

    public AmountViewModel(NavigationService navigation, DepositService deposits, DepositSession session)
        : base(navigation)
    {
        _deposits = deposits;
        _session = session;
    }

    public string AmountText => KioskFormat.MoneyNumber(AmountCents);

    public string LimitText => $"Up to {KioskFormat.Money(_deposits.MaxAmountCents)} per cheque";

    public override string FlowLabel => "CHEQUE DEPOSIT";

    public override string StepText => "STEP 2 OF 4";

    public override double Progress => 0.5;

    public override bool CanGoBack => true;

    public override void OnNavigatedTo() => AmountCents = _session.AmountCents;

    public override void AppendDigit(char digit)
    {
        var next = AmountCents * 10 + (digit - '0');
        if (next > MaxEntryCents)
        {
            return;
        }

        AmountCents = next;
    }

    public override void DeleteDigit() => AmountCents /= 10;

    partial void OnAmountCentsChanged(long value) =>
        ErrorMessage = value > 0 ? _deposits.ValidateAmount(value) : null;

    protected override void OnBack() => Navigation.NavigateTo<AccountNumberViewModel>();

    private bool CanContinue() => _deposits.ValidateAmount(AmountCents) is null;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        _session.AmountCents = AmountCents;
        Navigation.NavigateTo<InsertChequeViewModel>();
    }
}
