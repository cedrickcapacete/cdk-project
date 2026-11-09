using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Hardware;
using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.App.ViewModels;

public enum InsertStage
{
    Waiting,
    Processing,
    Failed,
}

public sealed partial class InsertChequeViewModel : StepViewModelBase
{
    private readonly ICheckScanner _scanner;
    private readonly DepositService _deposits;
    private readonly DepositSession _session;
    private CancellationTokenSource? _scanCancellation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusHeadline), nameof(StatusText), nameof(IsWaiting), nameof(IsProcessing),
        nameof(HasFailed), nameof(CanSimulateInsert), nameof(CanGoBack), nameof(CanExit))]
    private InsertStage _stage;

    public InsertChequeViewModel(NavigationService navigation, ICheckScanner scanner, DepositService deposits, DepositSession session)
        : base(navigation)
    {
        _scanner = scanner;
        _deposits = deposits;
        _session = session;
    }

    public string AmountText => KioskFormat.Money(_session.AmountCents);

    public string StatusHeadline => Stage switch
    {
        InsertStage.Processing => "READING YOUR CHEQUE",
        InsertStage.Failed => "WE COULDN'T COMPLETE THIS DEPOSIT",
        _ => "READY FOR YOUR CHEQUE",
    };

    public string StatusText => Stage switch
    {
        InsertStage.Processing => "Processing your deposit…",
        InsertStage.Failed => "Please take your cheque and try again.",
        _ => "Waiting for cheque",
    };

    public bool IsWaiting => Stage == InsertStage.Waiting;

    public bool IsProcessing => Stage == InsertStage.Processing;

    public bool HasFailed => Stage == InsertStage.Failed;

    public bool CanSimulateInsert => _scanner is SimulatedCheckScanner && IsWaiting;

    public override string FlowLabel => "CHEQUE DEPOSIT";

    public override string StepText => "STEP 3 OF 4";

    public override double Progress => 0.75;

    public override bool CanGoBack => !IsProcessing;

    public override bool CanExit => !IsProcessing;

    public override bool IsIdleTimeoutEnabled => !IsProcessing;

    public override void OnNavigatedTo() => _ = WaitForChequeAsync();

    public override void OnNavigatedFrom() => _scanCancellation?.Cancel();

    protected override void OnBack() => Navigation.NavigateTo<AmountViewModel>();

    [RelayCommand]
    private void SimulateInsert() => (_scanner as SimulatedCheckScanner)?.SimulateInsert();

    [RelayCommand]
    private Task Retry() => WaitForChequeAsync();

    private async Task WaitForChequeAsync()
    {
        if (_session.Account is not { } account)
        {
            Navigation.GoHome();
            return;
        }

        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;
        Stage = InsertStage.Waiting;

        CheckScanResult scan;
        try
        {
            scan = await _scanner.WaitForChequeAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            Stage = InsertStage.Failed;
            return;
        }

        Stage = InsertStage.Processing;
        var amountCents = _session.AmountCents;
        try
        {
            _session.Deposit = await Task.Run(() => _deposits.CreateDeposit(account, amountCents, scan));
            Navigation.NavigateTo<CompleteViewModel>();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            Stage = InsertStage.Failed;
        }
    }
}
