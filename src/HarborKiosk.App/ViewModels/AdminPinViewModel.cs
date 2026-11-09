using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public sealed partial class AdminPinViewModel : KeypadStepViewModelBase
{
    private const int MaxFailedAttempts = 3;

    private readonly AdminAccess _access;
    private int _failedAttempts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaskedPin), nameof(HasDigits))]
    [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
    private string _digits = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public AdminPinViewModel(NavigationService navigation, AdminAccess access)
        : base(navigation)
    {
        _access = access;
    }

    public string MaskedPin => string.Join("  ", new string('●', Digits.Length).ToCharArray());

    public bool HasDigits => Digits.Length > 0;

    public override string FlowLabel => "STAFF ACCESS";

    public override string StepText => "ENTER PIN";

    public override double Progress => 0.5;

    public override void AppendDigit(char digit)
    {
        if (Digits.Length >= AdminAccess.MaxPinLength)
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

    private bool CanUnlock() => AdminAccess.IsWellFormed(Digits);

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private void Unlock()
    {
        if (_access.Verify(Digits))
        {
            Navigation.NavigateTo<AdminViewModel>();
            return;
        }

        _failedAttempts++;
        if (_failedAttempts >= MaxFailedAttempts)
        {
            Navigation.GoHome();
            return;
        }

        Digits = string.Empty;
        ErrorMessage = "Incorrect PIN. Please try again.";
    }
}
