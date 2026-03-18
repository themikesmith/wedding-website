using Microsoft.EntityFrameworkCore;

namespace WeddingWebsite.Data;

public static class DbContextConfiguration
{
    /// <summary>
    /// Configures the database provider based on the connection string format.
    /// Supports both PostgreSQL and SQLite databases.
    /// </summary>
    public static DbContextOptionsBuilder ConfigureDatabaseProvider(
        this DbContextOptionsBuilder optionsBuilder, 
        string connectionString)
    {
        // Check for PostgreSQL connection string formats
        if (IsPostgresConnection(connectionString))
        {
            return optionsBuilder.UseNpgsql(connectionString);
        }
        // Check for SQLite connection string formats
        else if (IsSqliteConnection(connectionString))
        {
            return optionsBuilder.UseSqlite(connectionString);
        }
        
        throw new InvalidOperationException(
            $"Connection string does not match known database formats (PostgreSQL or SQLite). " +
            $"PostgreSQL expects 'Server=', 'Host=', ':5432', or 'postgresql://' prefix. " +
            $"SQLite expects 'DataSource' or '.db' extension.");
    }

    private static bool IsPostgresConnection(string connectionString)
    {
        return connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains(":5432", StringComparison.OrdinalIgnoreCase) ||
               connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSqliteConnection(string connectionString)
    {
        return connectionString.Contains("DataSource", StringComparison.OrdinalIgnoreCase) ||
               connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }
}
