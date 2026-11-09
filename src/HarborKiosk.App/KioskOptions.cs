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

    public const string DefaultAdminPin = "2468";

    public string AdminPin { get; init; } = DefaultAdminPin;

    /// <summary>
    /// --kiosk: fullscreen, topmost, no cursor, close blocked (Ctrl+Shift+F12 exits). --print: send receipts to the default Windows printer.
    /// --admin-pin=NNNN (or HARBOR_ADMIN_PIN): staff PIN for the admin screen.
    /// </summary>
    public static KioskOptions FromArgs(string[] args)
    {
        bool Has(string flag) => args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

        string? Value(string name) => args
            .Where(a => a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            .Select(a => a[(name.Length + 1)..])
            .LastOrDefault();

        return new KioskOptions
        {
            KioskMode = Has("--kiosk"),
            PrintToDefaultPrinter = Has("--print"),
            AdminPin = Value("--admin-pin") ?? Environment.GetEnvironmentVariable("HARBOR_ADMIN_PIN") ?? DefaultAdminPin,
        };
    }
}
