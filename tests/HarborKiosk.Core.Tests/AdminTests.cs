using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.Core.Tests;

public class AdminTests
{
    private static readonly CheckScanResult NoImages = new(null, null, null);
    private static readonly TimeSpan Est = TimeSpan.FromHours(-5);

    [Fact]
    public void AdminAccess_verifies_only_the_configured_pin()
    {
        var access = new AdminAccess("2468");

        Assert.True(access.Verify("2468"));
        Assert.False(access.Verify("2469"));
        Assert.False(access.Verify("24680"));
        Assert.False(access.Verify(""));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("12a4")]
    public void AdminAccess_rejects_malformed_configured_pin(string pin)
    {
        Assert.Throws<ArgumentException>(() => new AdminAccess(pin));
    }

    [Fact]
    public void Report_totals_deposits_within_range_by_day()
    {
        using var db = new TestDatabase();
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 2, 9, 0, 0, Est));
        var deposits = new DepositService(db.Database, clock, new DepositPolicy(500_000, Path.Combine(db.Directory, "images")));
        var accounts = new AccountService(db.Database);
        var alex = accounts.FindActiveAccount("123456789012")!;
        var jamie = accounts.FindActiveAccount("100200300400")!;

        deposits.CreateDeposit(alex, 1_000, NoImages);                      // Nov 2, out of range
        clock.Now = new DateTimeOffset(2026, 11, 5, 23, 30, 0, Est);
        deposits.CreateDeposit(alex, 2_500, NoImages);                      // Nov 5, late evening
        clock.Now = new DateTimeOffset(2026, 11, 6, 10, 0, 0, Est);
        var printed = deposits.CreateDeposit(jamie, 4_000, NoImages);       // Nov 6
        deposits.MarkReceiptPrinted(printed.Id);
        deposits.CreateDeposit(alex, 500, NoImages);                        // Nov 6

        var from = new DateOnly(2026, 11, 3);
        var to = new DateOnly(2026, 11, 6);
        var report = DepositReport.Build(deposits.GetDepositsBetween(from, to), from, to);

        Assert.Equal(3, report.Count);
        Assert.Equal(7_000, report.TotalCents);
        Assert.Equal(2_333, report.AverageCents);
        Assert.Equal(3, report.PendingCount);
        Assert.Equal(1, report.ReceiptsPrinted);
        Assert.Equal(4, report.Days.Count);
        Assert.Equal(new long[] { 0, 0, 2_500, 4_500 }, report.Days.Select(d => d.TotalCents));
    }

    [Fact]
    public void Report_csv_masks_account_numbers()
    {
        using var db = new TestDatabase();
        var today = new DateTimeOffset(2026, 11, 6, 10, 0, 0, Est);
        var deposits = new DepositService(db.Database, new FixedClock(today), new DepositPolicy(500_000, Path.Combine(db.Directory, "images")));
        var alex = new AccountService(db.Database).FindActiveAccount("123456789012")!;
        var deposit = deposits.CreateDeposit(alex, 12_345, NoImages);

        var day = DateOnly.FromDateTime(today.Date);
        var csv = DepositReport.Build(deposits.GetLatestDeposits(10), day, day).ToCsv();

        var lines = csv.TrimEnd().Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith(deposit.Reference + ",", lines[1]);
        Assert.Contains("**** 9012", lines[1]);
        Assert.Contains(",123.45,", lines[1]);
        Assert.DoesNotContain("123456789012", csv);
    }

    [Fact]
    public void GetLatestDeposits_spans_all_accounts()
    {
        using var db = new TestDatabase();
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 6, 10, 0, 0, Est));
        var deposits = new DepositService(db.Database, clock, new DepositPolicy(500_000, Path.Combine(db.Directory, "images")));
        var accounts = new AccountService(db.Database);
        deposits.CreateDeposit(accounts.FindActiveAccount("123456789012")!, 100, NoImages);
        clock.Now = clock.Now.AddMinutes(1);
        deposits.CreateDeposit(accounts.FindActiveAccount("555566667777")!, 200, NoImages);

        var latest = deposits.GetLatestDeposits(5);

        Assert.Equal(new long[] { 200, 100 }, latest.Select(d => d.AmountCents));
    }
}
