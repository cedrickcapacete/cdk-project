using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HarborKiosk.App;

public sealed class KioskOptions
{
    public bool KioskMode { get; init; }

    public bool PrintToDefaultPrinter { get; init; }

    public bool SeedDemoAccounts { get; init; } = true;

    public string DataDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HarborKiosk");

    public string KioskId { get; init; } = "HB-KIOSK-001";

    public long MaxDepositCents { get; init; } = 9_999_999_999;

    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan ReceiptScreenTimeout { get; init; } = TimeSpan.FromSeconds(20);

    public const string DefaultAdminPin = "2468";

    public string AdminPin { get; init; } = DefaultAdminPin;

    public static KioskOptions FromArgs(string[] args)
    {
        bool Has(string flag) => args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

        string? Value(string name) => args
            .Where(a => a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            .Select(a => a[(name.Length + 1)..])
            .LastOrDefault();

        var cfg = LoadConfig();

        bool kioskMode = Has("--kiosk")
            || string.Equals(cfg.Environment, "production", StringComparison.OrdinalIgnoreCase);

        string adminPin = Value("--admin-pin")
            ?? Environment.GetEnvironmentVariable("HARBOR_ADMIN_PIN")
            ?? cfg.AdminPin
            ?? DefaultAdminPin;

        return new KioskOptions
        {
            KioskMode = kioskMode,
            PrintToDefaultPrinter = Has("--print"),
            AdminPin = adminPin,
            KioskId = cfg.KioskId ?? "HB-KIOSK-001",
        };
    }

    private static AppSettings LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private sealed class AppSettings
    {
        public string? Environment { get; set; }
        public string? AdminPin { get; set; }
        public string? KioskId { get; set; }
    }
}
