using IcarryVendor.Web.Data;
using Microsoft.Data.Sqlite;

namespace IcarryVendor.Tests;

/// <summary>
/// A private in-memory Sqlite database loaded with the development schema and sample data.
/// The keep-alive connection holds the shared in-memory database open for the test's lifetime.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;

    public TestDatabase()
    {
        ConnectionString = $"Data Source=file:test-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();
        Factory = new DbConnectionFactory(new DatabaseOptions
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = ConnectionString,
        });
        DevDatabase.EnsureCreated(Factory, withSampleData: true);
    }

    public string ConnectionString { get; }

    public IDbConnectionFactory Factory { get; }

    public void Dispose() => _keepAlive.Dispose();
}
