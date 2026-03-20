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

        // SQLite detection: Data Source= keyword, :memory: literal, or file extension
        if (connString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
            connString.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase) ||
            connString.IndexOf(":memory:", StringComparison.OrdinalIgnoreCase) >= 0 ||
            connString.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
            connString.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
            connString.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase))
        {
            return new SqliteConnection(connString);
        }

        // Neither pattern matched — fail loudly rather than silently guessing.
        throw new InvalidOperationException(
            $"Could not determine database provider from connection string. " +
            $"Expected PostgreSQL (contains 'Host=' or starts with 'postgresql://') " +
            $"or SQLite (contains 'Data Source=' or ends with '.db'). " +
            $"Connection string prefix: '{connString[..Math.Min(60, connString.Length)]}'");
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
