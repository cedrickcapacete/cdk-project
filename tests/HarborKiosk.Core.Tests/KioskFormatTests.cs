using HarborKiosk.Core.Formatting;

namespace HarborKiosk.Core.Tests;

public class KioskFormatTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1234", "1234")]
    [InlineData("12345", "1234 5")]
    [InlineData("123456789012", "1234 5678 9012")]
    public void GroupAccountNumber_groups_by_four(string digits, string expected)
    {
        Assert.Equal(expected, KioskFormat.GroupAccountNumber(digits));
    }

    [Theory]
    [InlineData(0, "$0.00")]
    [InlineData(2_500, "$25.00")]
    [InlineData(123_456_78, "$123,456.78")]
    public void Money_formats_cents_as_dollars(long cents, string expected)
    {
        Assert.Equal(expected, KioskFormat.Money(cents));
    }

    [Fact]
    public void MaskAccountNumber_shows_last_four()
    {
        Assert.Equal("•••• 9012", KioskFormat.MaskAccountNumber("123456789012"));
    }
}
