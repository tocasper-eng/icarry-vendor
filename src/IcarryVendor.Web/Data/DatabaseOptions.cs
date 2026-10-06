namespace IcarryVendor.Web.Data;

public enum DatabaseProvider
{
    MySql,
    Sqlite,
}

/// <summary>Bound from the "Database" configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>MySql in production (the shared iCarry database); Sqlite for local development and tests.</summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MySql;

    public string ConnectionString { get; set; } = "";

    /// <summary>Create the development schema and sample data on startup (Sqlite only).</summary>
    public bool SeedDevelopmentData { get; set; }
}
