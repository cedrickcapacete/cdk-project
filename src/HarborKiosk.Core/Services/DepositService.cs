using System.Globalization;
using System.Security.Cryptography;
using HarborKiosk.Core.Data;
using HarborKiosk.Core.Formatting;
using HarborKiosk.Core.Models;
using Microsoft.Data.Sqlite;

namespace HarborKiosk.Core.Services;

public sealed record DepositPolicy(long MaxAmountCents, string ImageDirectory);

public sealed class DepositService
{
    public const int FundsAvailabilityBusinessDays = 2;

    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int SqliteConstraintError = 19;

    private const string SelectColumns = @"
        SELECT d.id, d.reference, d.account_id, a.account_number, d.amount_cents, d.status,
               d.created_at, d.funds_available_on, d.front_image_path, d.back_image_path,
               d.micr_line, d.receipt_printed
        FROM deposits d
        JOIN accounts a ON a.id = d.account_id";

    private readonly KioskDatabase _database;
    private readonly IClock _clock;
    private readonly DepositPolicy _policy;

    public DepositService(KioskDatabase database, IClock clock, DepositPolicy policy)
    {
        _database = database;
        _clock = clock;
        _policy = policy;
    }

    public long MaxAmountCents => _policy.MaxAmountCents;

    public string? ValidateAmount(long amountCents)
    {
        if (amountCents <= 0)
        {
            return "Enter the amount written on your cheque.";
        }

        if (amountCents > _policy.MaxAmountCents)
        {
            return $"Kiosk deposits are limited to {KioskFormat.Money(_policy.MaxAmountCents)} per cheque.";
        }

        return null;
    }

    public Deposit CreateDeposit(Account account, long amountCents, CheckScanResult scan)
    {
        var error = ValidateAmount(amountCents);
        if (error is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCents), error);
        }

        var createdAt = _clock.Now;
        var fundsAvailableOn = BusinessDays.Add(DateOnly.FromDateTime(createdAt.Date), FundsAvailabilityBusinessDays);

        using var connection = _database.Open();
        var (id, reference) = InsertWithUniqueReference(connection, account, amountCents, scan.MicrLine, createdAt, fundsAvailableOn);

        var frontPath = SaveImage(reference, "front", scan.FrontImage);
        var backPath = SaveImage(reference, "back", scan.BackImage);
        if (frontPath is not null || backPath is not null)
        {
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE deposits SET front_image_path = $front, back_image_path = $back WHERE id = $id;";
            update.Parameters.AddWithValue("$front", (object?)frontPath ?? DBNull.Value);
            update.Parameters.AddWithValue("$back", (object?)backPath ?? DBNull.Value);
            update.Parameters.AddWithValue("$id", id);
            update.ExecuteNonQuery();
        }

        return new Deposit(id, reference, account.Id, account.AccountNumber, amountCents, DepositStatus.Pending,
            createdAt, fundsAvailableOn, frontPath, backPath, scan.MicrLine, ReceiptPrinted: false);
    }

    public IReadOnlyList<Deposit> GetRecentDeposits(Account account, int limit = 10)
    {
        using var connection = _database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + @"
            WHERE d.account_id = $accountId
            ORDER BY d.created_at DESC, d.id DESC
            LIMIT $limit;";
        command.Parameters.AddWithValue("$accountId", account.Id);
        command.Parameters.AddWithValue("$limit", limit);
        return ReadAll(command);
    }

    public IReadOnlyList<Deposit> GetLatestDeposits(int limit)
    {
        using var connection = _database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + @"
            ORDER BY d.created_at DESC, d.id DESC
            LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);
        return ReadAll(command);
    }

    /// <summary>Deposits whose local creation date falls within [from, to].</summary>
    public IReadOnlyList<Deposit> GetDepositsBetween(DateOnly from, DateOnly to)
    {
        using var connection = _database.Open();
        using var command = connection.CreateCommand();
        // created_at is ISO-8601 with an offset, so compare loosely in SQL (a day either side) and exactly below.
        command.CommandText = SelectColumns + @"
            WHERE d.created_at >= $floor AND d.created_at < $ceiling
            ORDER BY d.created_at DESC, d.id DESC;";
        command.Parameters.AddWithValue("$floor", from.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$ceiling", to.AddDays(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        return ReadAll(command)
            .Where(d =>
            {
                var day = DateOnly.FromDateTime(d.CreatedAt.Date);
                return day >= from && day <= to;
            })
            .ToList();
    }

    public void MarkReceiptPrinted(long depositId)
    {
        using var connection = _database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE deposits SET receipt_printed = 1 WHERE id = $id;";
        command.Parameters.AddWithValue("$id", depositId);
        command.ExecuteNonQuery();
    }

    private static (long Id, string Reference) InsertWithUniqueReference(
        SqliteConnection connection, Account account, long amountCents, string? micrLine,
        DateTimeOffset createdAt, DateOnly fundsAvailableOn)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            var reference = NewReference(account.AccountNumber);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO deposits (reference, account_id, amount_cents, status, created_at, funds_available_on, micr_line)
                VALUES ($reference, $accountId, $amount, $status, $createdAt, $fundsOn, $micr);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$reference", reference);
            command.Parameters.AddWithValue("$accountId", account.Id);
            command.Parameters.AddWithValue("$amount", amountCents);
            command.Parameters.AddWithValue("$status", DepositStatus.Pending.ToString());
            command.Parameters.AddWithValue("$createdAt", createdAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$fundsOn", fundsAvailableOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$micr", (object?)micrLine ?? DBNull.Value);

            try
            {
                return ((long)command.ExecuteScalar()!, reference);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintError && attempt < maxAttempts)
            {
            }
        }
    }

    private static string NewReference(string accountNumber)
    {
        Span<char> suffix = stackalloc char[4];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];
        }

        return $"HB-{accountNumber[^4..]}-{suffix.ToString()}";
    }

    private string? SaveImage(string reference, string side, byte[]? image)
    {
        if (image is null || image.Length == 0)
        {
            return null;
        }

        Directory.CreateDirectory(_policy.ImageDirectory);
        var path = Path.Combine(_policy.ImageDirectory, $"{reference}-{side}.tif");
        File.WriteAllBytes(path, image);
        return path;
    }

    private static List<Deposit> ReadAll(SqliteCommand command)
    {
        var deposits = new List<Deposit>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            deposits.Add(Map(reader));
        }

        return deposits;
    }

    private static Deposit Map(SqliteDataReader reader) => new(
        Id: reader.GetInt64(0),
        Reference: reader.GetString(1),
        AccountId: reader.GetInt64(2),
        AccountNumber: reader.GetString(3),
        AmountCents: reader.GetInt64(4),
        Status: Enum.Parse<DepositStatus>(reader.GetString(5)),
        CreatedAt: DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        FundsAvailableOn: DateOnly.ParseExact(reader.GetString(7), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        FrontImagePath: reader.IsDBNull(8) ? null : reader.GetString(8),
        BackImagePath: reader.IsDBNull(9) ? null : reader.GetString(9),
        MicrLine: reader.IsDBNull(10) ? null : reader.GetString(10),
        ReceiptPrinted: reader.GetBoolean(11));
}
