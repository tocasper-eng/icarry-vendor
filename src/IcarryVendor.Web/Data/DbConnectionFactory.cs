using System.Data.Common;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace IcarryVendor.Web.Data;

public interface IDbConnectionFactory
{
    /// <summary>Returns a new, unopened connection. Dapper opens and closes it as needed.</summary>
    DbConnection Create();
}

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseProvider _provider;
    private readonly string _connectionString;

    public DbConnectionFactory(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString is not configured.");
        }

        _provider = options.Provider;
        _connectionString = options.Provider == DatabaseProvider.MySql
            ? new MySqlConnectionStringBuilder(options.ConnectionString)
            {
                // The legacy schema stores flags (is_on, is_del, shop_admin...) as TINYINT; read them as numbers.
                TreatTinyAsBoolean = false,
            }.ConnectionString
            : options.ConnectionString;
    }

    public DbConnection Create() => _provider switch
    {
        DatabaseProvider.MySql => new MySqlConnection(_connectionString),
        DatabaseProvider.Sqlite => new SqliteConnection(_connectionString),
        _ => throw new NotSupportedException($"Unsupported database provider {_provider}."),
    };
}
