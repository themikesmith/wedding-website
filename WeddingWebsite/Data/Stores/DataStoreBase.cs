using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace WeddingWebsite.Data.Stores;

/// <summary>
/// Abstract base class for data store implementations, providing common database connection
/// and parameter binding functionality across SQLite and PostgreSQL.
/// </summary>
public abstract class DataStoreBase : IDataStore
{
    protected readonly string ConnectionString;

    protected DataStoreBase(IConfiguration configuration)
    {
        // Try environment variable first
        var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(envConnectionString))
        {
            ConnectionString = envConnectionString;
        }
        else
        {
            ConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
    }

    /// <summary>
    /// Creates a database connection appropriate for the configured database type.
    /// Automatically detects PostgreSQL or SQLite based on connection string format.
    /// </summary>
    public DbConnection CreateConnection()
    {
        var connString = ConnectionString;

        // PostgreSQL detection: URI format or Host= parameter
        if (connString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connString.StartsWith("server=", StringComparison.OrdinalIgnoreCase) ||
            connString.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new NpgsqlConnection(connString);
        }

        // Default to SQLite
        return new SqliteConnection(connString);
    }

    /// <summary>
    /// Adds a parameter to a database command, supporting both SQLite and PostgreSQL.
    /// </summary>
    public void AddParameter(DbCommand cmd, string name, object? value)
    {
        if (cmd is SqliteCommand sqliteCmd)
        {
            sqliteCmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        else if (cmd is NpgsqlCommand npgsqlCmd)
        {
            npgsqlCmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        else
        {
            throw new NotSupportedException($"Command type {cmd.GetType()} is not supported");
        }
    }
}
