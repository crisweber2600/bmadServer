using bmadServer.ApiService.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bmadServer.BDD.Tests.TestSupport;

/// <summary>
/// Provides test database context for BDD specification tests.
/// For BDD tests, we primarily use mock state rather than actual database operations.
/// The DbContext is provided for service initialization but EnsureCreated is skipped
/// to avoid EF Core model validation issues with JsonDocument properties.
/// </summary>
public static class SqliteTestDbContext
{
    /// <summary>
    /// Creates a ServiceProvider with SQLite-backed ApplicationDbContext.
    /// NOTE: Database schema is NOT created - BDD tests should use mock state.
    /// For actual integration testing, use TestWebApplicationFactory instead.
    /// </summary>
    /// <param name="testName">Unique test name for database isolation</param>
    /// <returns>Tuple of ServiceProvider and the open SqliteConnection (must be kept alive)</returns>
    public static (IServiceProvider Provider, SqliteConnection Connection) Create(string testName)
    {
        // Create unique in-memory database with shared cache
        // CA2000: Connection is intentionally returned to caller for disposal
#pragma warning disable CA2000
        var connection = new SqliteConnection($"DataSource={testName};Mode=Memory;Cache=Shared");
#pragma warning restore CA2000
        connection.Open();
        
        // Disable foreign key constraints for simpler test setup
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();
        }
        
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(connection);
            // Disable model validation to avoid JsonDocument issues
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
        });
        
        var provider = services.BuildServiceProvider();
        
        // NOTE: We intentionally skip EnsureCreated() here because:
        // 1. BDD tests are specification tests that use mock state, not actual DB operations
        // 2. The EF Core model has JsonDocument properties that fail validation
        // 3. For real integration testing, use TestWebApplicationFactory instead
        
        return (provider, connection);
    }
    
    /// <summary>
    /// Creates a standalone ApplicationDbContext with SQLite for simple tests.
    /// NOTE: Database schema is NOT created - use mock state for BDD tests.
    /// </summary>
    /// <param name="testName">Unique test name for database isolation</param>
    /// <returns>Tuple of DbContext and the open SqliteConnection (must be kept alive)</returns>
    public static (ApplicationDbContext DbContext, SqliteConnection Connection) CreateDbContext(string testName)
    {
        // CA2000: Connection is intentionally returned to caller for disposal
#pragma warning disable CA2000
        var connection = new SqliteConnection($"DataSource={testName};Mode=Memory;Cache=Shared");
#pragma warning restore CA2000
        connection.Open();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();
        }
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var dbContext = new ApplicationDbContext(options);
        // NOTE: Skip EnsureCreated() - BDD tests use mock state
        
        return (dbContext, connection);
    }
}
