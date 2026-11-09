using System.Globalization;

namespace HarborKiosk.Core.Formatting;

public static class KioskFormat
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public const string CurrencySymbol = "₱";

    public const string CurrencyCode = "PHP";

    // Explicit format so the peso sign doesn't depend on the OS's ICU/NLS data for en-PH.
    private static readonly NumberFormatInfo PesoFormat = CreatePesoFormat();

    public static string Money(long cents) => (cents / 100m).ToString("C2", PesoFormat);

    public static string MoneyNumber(long cents) => (cents / 100m).ToString("N2", PesoFormat);

    private static NumberFormatInfo CreatePesoFormat()
    {
        var format = (NumberFormatInfo)UsCulture.NumberFormat.Clone();
        format.CurrencySymbol = CurrencySymbol;
        return NumberFormatInfo.ReadOnly(format);
    }

    public static string GroupAccountNumber(string digits)
    {
        var groups = Enumerable.Range(0, (digits.Length + 3) / 4)
            .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4)));
        return string.Join(" ", groups);
    }

    public static string MaskAccountNumber(string accountNumber) =>
        "•••• " + (accountNumber.Length >= 4 ? accountNumber[^4..] : accountNumber);

    public static string Date(DateTimeOffset value) => value.ToString("MMM d, yyyy", UsCulture);

    public static string Date(DateOnly value) => value.ToString("MMM d, yyyy", UsCulture);

    public static string DateTime(DateTimeOffset value) => value.ToString("MMM d, yyyy  h:mm tt", UsCulture);
}
