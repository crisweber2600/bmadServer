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
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
    public DbSet<WorkflowEvent> WorkflowEvents { get; set; }
    public DbSet<WorkflowStepHistory> WorkflowStepHistories { get; set; }
    public DbSet<WorkflowParticipant> WorkflowParticipants { get; set; }
    public DbSet<WorkflowCheckpoint> WorkflowCheckpoints { get; set; }
    public DbSet<QueuedInput> QueuedInputs { get; set; }
    public DbSet<Conflict> Conflicts { get; set; }
    public DbSet<BufferedInput> BufferedInputs { get; set; }

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

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.Role });
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>();
            entity.HasIndex(e => e.UserId);
            
            entity.HasOne(e => e.User)
                .WithMany()
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
            entity.HasIndex(e => e.PausedAt);
            entity.HasIndex(e => e.CancelledAt);
            
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
            
            // Attribution fields (Story 7.3)
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.InputType).HasMaxLength(50);
            
            // JSONB columns for PostgreSQL with JSON value converters for compatibility
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            entity.Property(e => e.AlternativesConsidered)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Timestamp);
            // Index for attribution queries (Story 7.3)
            entity.HasIndex(e => new { e.UserId, e.Timestamp })
                .HasDatabaseName("idx_workflow_events_user_time");
            
            // GIN indexes for JSONB columns (PostgreSQL only)
            entity.HasIndex(e => e.Payload)
                .HasMethod("gin");
            entity.HasIndex(e => e.AlternativesConsidered)
                .HasMethod("gin");
            
            // Foreign key to WorkflowInstance
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowStepHistory>(entity =>
        {
            entity.ToTable("workflow_step_histories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StepName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            
            // JSONB columns for PostgreSQL with JSON value converters for compatibility
            entity.Property(e => e.Input)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            entity.Property(e => e.Output)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.StepId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
            
            // GIN indexes for JSONB columns (PostgreSQL only)
            entity.HasIndex(e => e.Input)
                .HasMethod("gin");
            entity.HasIndex(e => e.Output)
                .HasMethod("gin");
            
            // Foreign key to WorkflowInstance
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowParticipant>(entity =>
        {
            entity.ToTable("workflow_participants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            // Unique constraint: one user per workflow
            entity.HasIndex(e => new { e.WorkflowId, e.UserId })
                .IsUnique();
            
            // Indexes for lookup performance
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.UserId);
            
            // Foreign keys
            entity.HasOne(e => e.Workflow)
                .WithMany()
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowCheckpoint>(entity =>
        {
            entity.ToTable("workflow_checkpoints");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CheckpointType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            // JSONB columns for PostgreSQL with JSON value converters for compatibility
            entity.Property(e => e.StateSnapshot)
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v));
            
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => new { e.WorkflowId, e.CreatedAt })
                .HasDatabaseName("idx_checkpoints_workflow_time");
            entity.HasIndex(e => new { e.WorkflowId, e.Version })
                .HasDatabaseName("idx_checkpoints_version");
            
            // GIN indexes for JSONB columns (PostgreSQL only)
            entity.HasIndex(e => e.StateSnapshot)
                .HasMethod("gin");
            entity.HasIndex(e => e.Metadata)
                .HasMethod("gin");
            
            // Foreign keys
            entity.HasOne(e => e.Workflow)
                .WithMany()
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.TriggeredByUser)
                .WithMany()
                .HasForeignKey(e => e.TriggeredBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QueuedInput>(entity =>
        {
            entity.ToTable("queued_inputs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InputType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(InputStatus.Queued);
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);
            
            // Auto-increment sequence number for FIFO ordering
            entity.Property(e => e.SequenceNumber)
                .UseIdentityAlwaysColumn();
            
            // JSONB column for PostgreSQL with JSON value converter for compatibility
            entity.Property(e => e.Content)
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => new { e.WorkflowId, e.Status, e.SequenceNumber })
                .HasDatabaseName("idx_queued_inputs_workflow_status");
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_queued_inputs_user");
            
            // GIN index for JSONB column (PostgreSQL only)
            entity.HasIndex(e => e.Content)
                .HasMethod("gin");
            
            // Foreign keys
            entity.HasOne(e => e.Workflow)
                .WithMany()
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conflict>(entity =>
        {
            entity.ToTable("conflicts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.InputsJson)
                .HasColumnName("inputs")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(e => e.ResolutionJson)
                .HasColumnName("resolution")
                .HasColumnType("jsonb");
            
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt)
                .HasFilter("\"Status\" = 'Pending'");
            
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BufferedInput>(entity =>
        {
            entity.ToTable("buffered_inputs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Value).IsRequired();
            
            entity.HasIndex(e => new { e.WorkflowInstanceId, e.FieldName, e.IsApplied });
            entity.HasIndex(e => e.ConflictId);
            
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Conflict)
                .WithMany()
                .HasForeignKey(e => e.ConflictId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
