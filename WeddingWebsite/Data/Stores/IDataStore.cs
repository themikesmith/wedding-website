using System.Data.Common;

namespace WeddingWebsite.Data.Stores;

/// <summary>
/// Common interface for all data store implementations, providing shared database access functionality.
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Creates a database connection appropriate for the configured database type.
    /// Automatically detects PostgreSQL or SQLite based on connection string format.
    /// </summary>
    DbConnection CreateConnection();

    /// <summary>
    /// Adds a parameter to a database command, supporting both SQLite and PostgreSQL.
    /// </summary>
    void AddParameter(DbCommand cmd, string name, object? value);
}
