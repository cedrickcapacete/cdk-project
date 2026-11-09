using System.Text.RegularExpressions;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;
using HarborKiosk.Core.Services;

namespace HarborKiosk.Core.Tests;

public class DepositServiceTests
{
    private static readonly CheckScanResult NoImages = new(null, null, null);

    // Friday, 2026-11-06 10:00 local
    private static readonly DateTimeOffset Friday = new(2026, 11, 6, 10, 0, 0, TimeSpan.FromHours(-5));

    private static (TestDatabase Db, DepositService Deposits, Account Account, FixedClock Clock) Create(long maxCents = 500_000)
    {
        var db = new TestDatabase();
        var clock = new FixedClock(Friday);
        var deposits = new DepositService(db.Database, clock, new DepositPolicy(maxCents, Path.Combine(db.Directory, "images")));
        var account = new AccountService(db.Database).FindActiveAccount("123456789012")!;
        return (db, deposits, account, clock);
    }

    [Fact]
    public void CreateDeposit_persists_pending_deposit_with_reference()
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;

        var deposit = deposits.CreateDeposit(account, 2_500, NoImages);

        Assert.Matches(new Regex("^HB-9012-[A-Z2-9]{4}$"), deposit.Reference);
        Assert.Equal(DepositStatus.Pending, deposit.Status);
        var stored = Assert.Single(deposits.GetRecentDeposits(account));
        Assert.Equal(deposit.Reference, stored.Reference);
        Assert.Equal(2_500, stored.AmountCents);
        Assert.False(stored.ReceiptPrinted);
    }

    [Fact]
    public void CreateDeposit_funds_availability_skips_weekend()
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;

        var deposit = deposits.CreateDeposit(account, 1_000, NoImages);

        Assert.Equal(new DateOnly(2026, 11, 10), deposit.FundsAvailableOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(500_001)]
    public void CreateDeposit_rejects_invalid_amounts(long cents)
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;

        Assert.NotNull(deposits.ValidateAmount(cents));
        Assert.Throws<ArgumentOutOfRangeException>(() => deposits.CreateDeposit(account, cents, NoImages));
    }

    [Fact]
    public void CreateDeposit_saves_scanned_images()
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;

        var deposit = deposits.CreateDeposit(account, 1_000, new CheckScanResult(new byte[] { 1, 2 }, new byte[] { 3 }, "⑆123⑆"));

        Assert.True(File.Exists(deposit.FrontImagePath));
        Assert.True(File.Exists(deposit.BackImagePath));
        Assert.Equal(deposit.FrontImagePath, deposits.GetRecentDeposits(account)[0].FrontImagePath);
    }

    [Fact]
    public void GetRecentDeposits_returns_newest_first_and_respects_limit()
    {
        var (db, deposits, account, clock) = Create();
        using var _db = db;

        for (var i = 1; i <= 4; i++)
        {
            clock.Now = Friday.AddMinutes(i);
            deposits.CreateDeposit(account, i * 100, NoImages);
        }

        var recent = deposits.GetRecentDeposits(account, limit: 3);

        Assert.Equal(new long[] { 400, 300, 200 }, recent.Select(d => d.AmountCents));
    }

    [Fact]
    public void MarkReceiptPrinted_updates_flag()
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;
        var deposit = deposits.CreateDeposit(account, 1_000, NoImages);

        deposits.MarkReceiptPrinted(deposit.Id);

        Assert.True(deposits.GetRecentDeposits(account)[0].ReceiptPrinted);
    }

    [Fact]
    public void Receipt_masks_account_number()
    {
        var (db, deposits, account, _) = Create();
        using var _db = db;
        var deposit = deposits.CreateDeposit(account, 2_500, NoImages);

        var text = ReceiptFormatter.Build(deposit, "HB-KIOSK-001");

        Assert.Contains("•••• 9012", text);
        Assert.DoesNotContain("123456789012", text);
        Assert.Contains("₱25.00", text);
        Assert.Contains(deposit.Reference, text);
    }
}
