using HarborKiosk.Core.Models;

namespace HarborKiosk.Core.Hardware;

/// <summary>Abstraction over the cheque reader; a vendor SDK (Digital Check, Panini, etc.) implements this.</summary>
public interface ICheckScanner
{
    Task<CheckScanResult> WaitForChequeAsync(CancellationToken cancellationToken);
}

public sealed class SimulatedCheckScanner : ICheckScanner
{
    private readonly object _gate = new();
    private TaskCompletionSource<CheckScanResult>? _pending;

    public Task<CheckScanResult> WaitForChequeAsync(CancellationToken cancellationToken)
    {
        var pending = new TaskCompletionSource<CheckScanResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _pending?.TrySetCanceled();
            _pending = pending;
        }

        cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
        return pending.Task;
    }

    public void SimulateInsert()
    {
        TaskCompletionSource<CheckScanResult>? pending;
        lock (_gate)
        {
            pending = _pending;
            _pending = null;
        }

        pending?.TrySetResult(new CheckScanResult(FrontImage: null, BackImage: null, MicrLine: null));
    }
}
