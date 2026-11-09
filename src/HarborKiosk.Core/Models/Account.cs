namespace HarborKiosk.Core.Models;

public sealed record Account(long Id, string AccountNumber, string HolderName, bool IsActive);
