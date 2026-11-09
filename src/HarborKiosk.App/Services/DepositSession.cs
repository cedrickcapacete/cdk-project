using HarborKiosk.Core.Models;

namespace HarborKiosk.App.Services;

public enum KioskFlow
{
    Deposit,
    History,
}

public sealed class DepositSession
{
    public KioskFlow Flow { get; set; }

    public Account? Account { get; set; }

    public long AmountCents { get; set; }

    public Deposit? Deposit { get; set; }

    public void Reset()
    {
        Flow = KioskFlow.Deposit;
        Account = null;
        AmountCents = 0;
        Deposit = null;
    }
}
