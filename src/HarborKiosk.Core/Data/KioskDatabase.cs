using Microsoft.Data.Sqlite;

namespace HarborKiosk.Core.Data;

public sealed class KioskDatabase
{
    private const string Schema = @"
        CREATE TABLE IF NOT EXISTS accounts (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            account_number  TEXT    NOT NULL UNIQUE,
            holder_name     TEXT    NOT NULL,
            is_active       INTEGER NOT NULL DEFAULT 1,
            created_at      TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS deposits (
            id                  INTEGER PRIMARY KEY AUTOINCREMENT,
            reference           TEXT    NOT NULL UNIQUE,
            account_id          INTEGER NOT NULL REFERENCES accounts(id),
            amount_cents        INTEGER NOT NULL CHECK (amount_cents > 0),
            status              TEXT    NOT NULL,
            created_at          TEXT    NOT NULL,
            funds_available_on  TEXT    NOT NULL,
            front_image_path    TEXT,
            back_image_path     TEXT,
            micr_line           TEXT,
            receipt_printed     INTEGER NOT NULL DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS ix_deposits_account_created
            ON deposits (account_id, created_at DESC);";

    private static readonly (string Number, string Holder)[] DemoAccounts =
    {
        ("123456789012", "Alex Morgan"),
        ("100200300400", "Jamie Chen"),
        ("555566667777", "Sam Rivera"),
    };

    private readonly string _connectionString;

    public KioskDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
        }.ToString();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;" + Schema;
        command.ExecuteNonQuery();
    }

    public void SeedDemoAccounts()
    {
        using var connection = Open();
        foreach (var (number, holder) in DemoAccounts)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO accounts (account_number, holder_name, is_active, created_at)
                VALUES ($number, $holder, 1, $now);";
            command.Parameters.AddWithValue("$number", number);
            command.Parameters.AddWithValue("$holder", holder);
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
        }
    }
}
