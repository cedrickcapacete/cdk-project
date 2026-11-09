using System;
using System.IO;
using System.Linq;

namespace HarborKiosk.App;

public sealed class KioskOptions
{
    public bool KioskMode { get; init; }

    public bool PrintToDefaultPrinter { get; init; }

    public bool SeedDemoAccounts { get; init; } = true;

    public string DataDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HarborKiosk");

    public string KioskId { get; init; } = "HB-KIOSK-001";

    public long MaxDepositCents { get; init; } = 500_000;

    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan ReceiptScreenTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>--kiosk: fullscreen, topmost, no cursor, close blocked (Ctrl+Shift+F12 exits). --print: send receipts to the default Windows printer.</summary>
    public static KioskOptions FromArgs(string[] args)
    {
        bool Has(string flag) => args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

        return new KioskOptions
        {
            KioskMode = Has("--kiosk"),
            PrintToDefaultPrinter = Has("--print"),
        };
    }
}
