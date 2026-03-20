using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            return optionsBuilder.UseNpgsql(NormalizePostgresConnectionString(connectionString));
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
        if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains(":5432", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSqliteConnection(string connectionString)
    {
        return connectionString.Contains("DataSource", StringComparison.OrdinalIgnoreCase) ||
               connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
        };

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var userInfoParts = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
            builder.Username = Uri.UnescapeDataString(userInfoParts[0]);
            if (userInfoParts.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
            }
        }

        ApplyQueryParameters(uri.Query, builder);
        return builder.ConnectionString;
    }

    private static void ApplyQueryParameters(string query, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var trimmedQuery = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return;
        }

        var pairs = trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2, StringSplitOptions.None);
            if (keyValue.Length == 0 || string.IsNullOrWhiteSpace(keyValue[0]))
            {
                continue;
            }

            var key = Uri.UnescapeDataString(keyValue[0]);
            var value = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]) : string.Empty;
            builder[key] = value;
        }
    }
}
