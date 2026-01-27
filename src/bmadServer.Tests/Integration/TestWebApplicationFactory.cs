using bmadServer.ApiService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Test web application factory for integration tests.
/// Creates an in-memory test server with SQLite database.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<bmadServer.ApiService.Program>
{
    private readonly SqliteConnection _connection;
    private readonly string _dbName;
    
    public TestWebApplicationFactory()
    {
        // Use a unique file-based SQLite database per factory instance
        // This provides better concurrency than :memory: with shared cache
        _dbName = $"test_{Guid.NewGuid():N}.db";
        _connection = new SqliteConnection($"DataSource={_dbName};Mode=Memory;Cache=Shared");
        _connection.Open();
        
        // Disable foreign key constraints on the initial connection
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext registrations
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.IsGenericType && 
                 d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();
            
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add SQLite database for testing with shared cache mode and FK disabled
            var connectionString = $"DataSource={_dbName};Mode=Memory;Cache=Shared;Foreign Keys=False";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        });

        builder.UseEnvironment("Test");
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Create database schema after host is built
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        
        // Disable foreign keys on the db context connection as well
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            // Delete the temp database file if it exists
            if (File.Exists(_dbName))
            {
                try { File.Delete(_dbName); } catch { }
            }
        }
    }
}
