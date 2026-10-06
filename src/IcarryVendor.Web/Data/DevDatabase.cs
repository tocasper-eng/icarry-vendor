using System.Reflection;
using Dapper;

namespace IcarryVendor.Web.Data;

/// <summary>
/// Builds a small Sqlite copy of the iCarry tables this app uses, for local development and tests.
/// The real schema lives in the shared production MySQL database and is never created by this app.
/// </summary>
public static class DevDatabase
{
    public static void EnsureCreated(IDbConnectionFactory factory, bool withSampleData)
    {
        using var connection = factory.Create();
        connection.Open();

        var exists = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'vendor_account'");
        if (exists > 0)
        {
            return;
        }

        connection.Execute(ReadScript("schema.sql"));
        if (withSampleData)
        {
            connection.Execute(ReadScript("seed.sql"));
        }
    }

    private static string ReadScript(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"IcarryVendor.Web.Data.Dev.{name}")
            ?? throw new InvalidOperationException($"Embedded script {name} not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
