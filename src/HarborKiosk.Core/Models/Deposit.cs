namespace HarborKiosk.Core.Models;

public enum DepositStatus
{
    Pending,
    Accepted,
    Returned,
}

public sealed record Deposit(
    long Id,
    string Reference,
    long AccountId,
    string AccountNumber,
    long AmountCents,
    DepositStatus Status,
    DateTimeOffset CreatedAt,
    DateOnly FundsAvailableOn,
    string? FrontImagePath,
    string? BackImagePath,
    string? MicrLine,
    bool ReceiptPrinted);
