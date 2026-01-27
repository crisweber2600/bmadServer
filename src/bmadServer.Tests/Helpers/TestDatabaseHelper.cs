using bmadServer.ApiService.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.Tests.Helpers;

/// <summary>
/// Helper class for creating SQLite in-memory database contexts for unit tests.
/// SQLite in-memory properly supports JsonDocument value converters, unlike EF Core InMemory provider.
/// </summary>
public static class TestDatabaseHelper
{
    /// <summary>
    /// Creates SQLite DbContextOptions for the ApplicationDbContext.
    /// Note: The returned connection must be kept open for the duration of the test.
    /// Foreign keys are disabled to match InMemory behavior for test compatibility.
    /// </summary>
    /// <param name="connection">The SQLite connection (output). Caller must dispose.</param>
    /// <returns>DbContextOptions configured for SQLite in-memory</returns>
    public static DbContextOptions<ApplicationDbContext> CreateSqliteOptions(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        // Disable foreign key constraints to match InMemory provider behavior
        // This allows tests to create entities without setting up all related records
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();
        }
        
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
    }
    
    /// <summary>
    /// Creates an ApplicationDbContext with SQLite in-memory database.
    /// The context owns the connection and will dispose it.
    /// </summary>
    public static (ApplicationDbContext Context, SqliteConnection Connection) CreateSqliteContext()
    {
        var options = CreateSqliteOptions(out var connection);
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}
