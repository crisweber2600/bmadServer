using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bmadServer.ApiService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
    public DbSet<WorkflowEvent> WorkflowEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions", t => 
            {
                // Check constraint: ExpiresAt must be after CreatedAt
                t.HasCheckConstraint("CK_Session_Expiry", 
                    "\"ExpiresAt\" > \"CreatedAt\"");
            });
            
            entity.HasKey(e => e.Id);
            
            // ConnectionId is nullable (cleared when session expires)
            entity.Property(e => e.ConnectionId).IsRequired(false);
            
            // Configure JSONB column for WorkflowState with JSON value converter
            // This supports both PostgreSQL JSONB and InMemory database for testing
            entity.Property(e => e.WorkflowState)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<WorkflowState>(v, (JsonSerializerOptions?)null));
            
            // GIN index for fast JSONB queries (PostgreSQL only)
            entity.HasIndex(e => e.WorkflowState)
                .HasMethod("gin");
            
            // Indexes for lookup performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConnectionId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsActive);
            
            // PostgreSQL row version for optimistic concurrency
            entity.Property<uint>("xmin")
                .IsRowVersion();
            
            // Foreign key to Users table
            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.ToTable("workflows");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Status).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("workflow_instances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkflowDefinitionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            // JSONB columns for PostgreSQL with JSON value converters for compatibility
            entity.Property(e => e.StepData)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            entity.Property(e => e.Context)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            
            // GIN indexes for JSONB columns (PostgreSQL only)
            entity.HasIndex(e => e.StepData)
                .HasMethod("gin");
            entity.HasIndex(e => e.Context)
                .HasMethod("gin");
        });

        modelBuilder.Entity<WorkflowEvent>(entity =>
        {
            entity.ToTable("workflow_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OldStatus)
                .HasConversion<string?>();
            entity.Property(e => e.NewStatus)
                .HasConversion<string?>();
            
            // Indexes
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Timestamp);
            
            // Foreign key to WorkflowInstance
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
