using System.Globalization;
using System.Text;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;

namespace HarborKiosk.Core.Services;

public sealed record DailyTotal(DateOnly Day, int Count, long TotalCents);

public sealed record DepositReport(
    DateOnly From,
    DateOnly To,
    int Count,
    long TotalCents,
    int PendingCount,
    int ReceiptsPrinted,
    IReadOnlyList<DailyTotal> Days,
    IReadOnlyList<Deposit> Deposits)
{
    public long AverageCents => Count == 0 ? 0 : TotalCents / Count;

    public static DepositReport Build(IEnumerable<Deposit> deposits, DateOnly from, DateOnly to)
    {
        var list = deposits
            .Where(d => InRange(d, from, to))
            .OrderByDescending(d => d.CreatedAt)
            .ToList();

        var byDay = list.ToLookup(Day);
        var days = new List<DailyTotal>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var items = byDay[day];
            days.Add(new DailyTotal(day, items.Count(), items.Sum(d => d.AmountCents)));
        }

        return new DepositReport(
            from,
            to,
            list.Count,
            list.Sum(d => d.AmountCents),
            list.Count(d => d.Status == DepositStatus.Pending),
            list.Count(d => d.ReceiptPrinted),
            days,
            list);
    }

    /// <summary>One row per deposit; account numbers are masked so the export is safe to carry off the kiosk.</summary>
    public string ToCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("reference,created_at,account,amount,status,funds_available_on,receipt_printed");
        foreach (var d in Deposits)
        {
            csv.Append(d.Reference).Append(',')
                .Append(d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)).Append(',')
                .Append(KioskFormat.MaskAccountNumber(d.AccountNumber).Replace("•", "*")).Append(',')
                .Append((d.AmountCents / 100m).ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                .Append(d.Status).Append(',')
                .Append(d.FundsAvailableOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                .Append(d.ReceiptPrinted ? "yes" : "no")
                .AppendLine();
        }

        return csv.ToString();
    }

    private static DateOnly Day(Deposit deposit) => DateOnly.FromDateTime(deposit.CreatedAt.Date);

    private static bool InRange(Deposit deposit, DateOnly from, DateOnly to)
    {
        var day = Day(deposit);
        return day >= from && day <= to;
    }
}
