using HarborKiosk.Core.Services;

namespace HarborKiosk.Core.Tests;

public class AccountServiceTests
{
    [Theory]
    [InlineData("123456789012", true)]
    [InlineData("12345678901", false)]
    [InlineData("1234567890123", false)]
    [InlineData("12345678901a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsWellFormed_requires_exactly_twelve_digits(string? input, bool expected)
    {
        Assert.Equal(expected, AccountService.IsWellFormed(input));
    }

    [Fact]
    public void FindActiveAccount_returns_seeded_account()
    {
        using var db = new TestDatabase();
        var service = new AccountService(db.Database);

        var account = service.FindActiveAccount("123456789012");

        Assert.NotNull(account);
        Assert.Equal("Alex Morgan", account!.HolderName);
    }

    [Fact]
    public void FindActiveAccount_returns_null_for_unknown_account()
    {
        using var db = new TestDatabase();
        var service = new AccountService(db.Database);

        Assert.Null(service.FindActiveAccount("999999999999"));
    }
}
