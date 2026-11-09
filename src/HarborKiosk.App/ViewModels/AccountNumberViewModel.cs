using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public sealed partial class AccountNumberViewModel : KeypadStepViewModelBase
{
    private const int MaxFailedAttempts = 3;

    private readonly AccountService _accounts;
    private readonly DepositSession _session;
    private int _failedAttempts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText), nameof(HasDigits), nameof(CounterText))]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string _digits = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public AccountNumberViewModel(NavigationService navigation, AccountService accounts, DepositSession session)
        : base(navigation)
    {
        _accounts = accounts;
        _session = session;
    }

    public string DisplayText => KioskFormat.GroupAccountNumber(Digits);

    public bool HasDigits => Digits.Length > 0;

    public string CounterText => $"{Digits.Length}/{AccountService.AccountNumberLength} digits";

    public string Subtitle => IsHistoryFlow
        ? "Enter your account number to see your recent kiosk deposits."
        : "You'll find this on your bank statement or online banking.";

    public override string FlowLabel => IsHistoryFlow ? "DEPOSIT HISTORY" : "CHEQUE DEPOSIT";

    public override string StepText => IsHistoryFlow ? "VERIFY ACCOUNT" : "STEP 1 OF 4";

    public override double Progress => IsHistoryFlow ? 0.5 : 0.25;

    private bool IsHistoryFlow => _session.Flow == KioskFlow.History;

    public override void OnNavigatedTo()
    {
        if (_session.Account is { } account)
        {
            Digits = account.AccountNumber;
        }
    }

    public override void AppendDigit(char digit)
    {
        if (Digits.Length >= AccountService.AccountNumberLength)
        {
            return;
        }

        ErrorMessage = null;
        Digits += digit;
    }

    public override void DeleteDigit()
    {
        if (Digits.Length == 0)
        {
            return;
        }

        ErrorMessage = null;
        Digits = Digits[..^1];
    }

    private bool CanContinue() => AccountService.IsWellFormed(Digits);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        var account = _accounts.FindActiveAccount(Digits);
        if (account is null)
        {
            _failedAttempts++;
            if (_failedAttempts >= MaxFailedAttempts)
            {
                Navigation.GoHome();
                return;
            }

            Digits = string.Empty;
            ErrorMessage = "We couldn't find that account. Please check the number and try again.";
            return;
        }

        _session.Account = account;
        if (IsHistoryFlow)
        {
            Navigation.NavigateTo<HistoryViewModel>();
        }
        else
        {
            Navigation.NavigateTo<AmountViewModel>();
        }
    }
}
