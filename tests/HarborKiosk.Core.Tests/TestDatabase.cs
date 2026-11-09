using HarborKiosk.Core.Data;
using HarborKiosk.Core.Services;
using Microsoft.Data.Sqlite;

namespace HarborKiosk.Core.Tests;

public sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        Directory = Path.Combine(Path.GetTempPath(), "harborkiosk-tests", Guid.NewGuid().ToString("N"));
        Database = new KioskDatabase(Path.Combine(Directory, "kiosk.db"));
        Database.Initialize();
        Database.SeedDemoAccounts();
    }

    public string Directory { get; }

    public KioskDatabase Database { get; }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        System.IO.Directory.Delete(Directory, recursive: true);
    }
}

public sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset now) => Now = now;

    public DateTimeOffset Now { get; set; }
}
