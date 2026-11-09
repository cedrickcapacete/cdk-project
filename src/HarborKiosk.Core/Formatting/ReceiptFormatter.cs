using System.Text;
using HarborKiosk.Core.Models;

namespace HarborKiosk.Core.Formatting;

public static class ReceiptFormatter
{
    private const int Width = 40;

    public static string Build(Deposit deposit, string kioskId)
    {
        var receipt = new StringBuilder();
        Center(receipt, "HARBOR BANK");
        Center(receipt, "Cheque Deposit Receipt");
        Rule(receipt);
        Line(receipt, "Date", KioskFormat.DateTime(deposit.CreatedAt));
        Line(receipt, "Kiosk", kioskId);
        Line(receipt, "Account", KioskFormat.MaskAccountNumber(deposit.AccountNumber));
        Line(receipt, "Reference", deposit.Reference);
        Rule(receipt);
        Line(receipt, "Deposit amount", KioskFormat.Money(deposit.AmountCents));
        Line(receipt, "Status", "Pending verification");
        Line(receipt, "Funds available", KioskFormat.Date(deposit.FundsAvailableOn));
        Rule(receipt);
        Center(receipt, "Deposits are subject to verification.");
        Center(receipt, "Thank you for banking with Harbor Bank.");
        return receipt.ToString();
    }

    private static void Center(StringBuilder receipt, string text) =>
        receipt.AppendLine(text.PadLeft((Width + text.Length) / 2));

    private static void Rule(StringBuilder receipt) => receipt.AppendLine(new string('-', Width));

    private static void Line(StringBuilder receipt, string label, string value) =>
        receipt.AppendLine(label + value.PadLeft(Math.Max(1, Width - label.Length)));
}
