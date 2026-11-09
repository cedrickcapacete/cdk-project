using HarborKiosk.Core.Data;
using HarborKiosk.Core.Models;

namespace HarborKiosk.Core.Services;

public sealed class AccountService
{
    public const int AccountNumberLength = 12;

    private readonly KioskDatabase _database;

    public AccountService(KioskDatabase database)
    {
        _database = database;
    }

    public static bool IsWellFormed(string? accountNumber) =>
        accountNumber is { Length: AccountNumberLength } && accountNumber.All(c => c is >= '0' and <= '9');

    public Account? FindActiveAccount(string accountNumber)
    {
        if (!IsWellFormed(accountNumber))
        {
            return null;
        }

        using var connection = _database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, account_number, holder_name, is_active
            FROM accounts
            WHERE account_number = $number AND is_active = 1;";
        command.Parameters.AddWithValue("$number", accountNumber);

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Account(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3))
            : null;
    }
}
