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
    public DbSet<AgentMessageLog> AgentMessageLogs { get; set; }
    public DbSet<AgentHandoff> AgentHandoffs { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<Decision> Decisions { get; set; }
    public DbSet<DecisionVersion> DecisionVersions { get; set; }
    public DbSet<DecisionReview> DecisionReviews { get; set; }
    public DbSet<DecisionReviewResponse> DecisionReviewResponses { get; set; }
    public DbSet<DecisionReviewInvitation> DecisionReviewInvitations { get; set; }
    public DbSet<DecisionConflict> DecisionConflicts { get; set; }
    public DbSet<ConflictRule> ConflictRules { get; set; }

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
            entity.Property(e => e.SharedContextJson)
                .HasColumnName("shared_context")
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
            entity.HasIndex(e => e.SharedContextJson)
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
            
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.StepId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
            
            entity.HasIndex(e => e.Input)
                .HasMethod("gin");
            entity.HasIndex(e => e.Output)
                .HasMethod("gin");
            
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentMessageLog>(entity =>
        {
            entity.ToTable("agent_message_logs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SourceAgent).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TargetAgent).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v));
            
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CorrelationId);
            
            entity.HasIndex(e => e.Content)
                .HasMethod("gin");
            
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentHandoff>(entity =>
        {
            entity.ToTable("agent_handoffs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FromAgentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ToAgentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WorkflowStepId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.Timestamp).IsRequired();
            
            // Indexes for common queries
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.WorkflowInstanceId, e.Timestamp });
            
            // Foreign key to WorkflowInstance
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("approval_requests");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AgentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProposedResponse).IsRequired();
            entity.Property(e => e.ConfidenceScore).IsRequired();
            entity.Property(e => e.Reasoning).HasMaxLength(2000);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.RequestedAt).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);
            entity.Property(e => e.StepId).HasMaxLength(100);
            entity.Property(e => e.Version).IsConcurrencyToken();
            
            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
            entity.HasIndex(e => new { e.WorkflowInstanceId, e.Status });
            
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Decision>(entity =>
        {
            entity.ToTable("decisions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.WorkflowInstanceId).IsRequired();
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DecisionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DecidedBy).IsRequired();
            entity.Property(e => e.DecidedAt).IsRequired();
            entity.Property(e => e.Question).HasMaxLength(1000);
            entity.Property(e => e.Reasoning).HasMaxLength(2000);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.LockReason).HasMaxLength(1000);
            
            // JSONB columns for Value, Options, Context
            entity.Property(e => e.Value)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            entity.Property(e => e.Options)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            entity.Property(e => e.Context)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // GIN indexes for JSONB columns (PostgreSQL only)
            entity.HasIndex(e => e.Value).HasMethod("gin");
            entity.HasIndex(e => e.Options).HasMethod("gin");
            entity.HasIndex(e => e.Context).HasMethod("gin");
            
            // Regular indexes for common queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StepId);
            entity.HasIndex(e => e.DecidedAt);
            entity.HasIndex(e => e.LockedAt);
            entity.HasIndex(e => e.WorkflowInstanceId);
            
            // Foreign keys
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany()
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.DecisionMaker)
                .WithMany()
                .HasForeignKey(e => e.DecidedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DecisionVersion>(entity =>
        {
            entity.ToTable("decision_versions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.DecisionId).IsRequired();
            entity.Property(e => e.VersionNumber).IsRequired();
            entity.Property(e => e.ModifiedBy).IsRequired();
            entity.Property(e => e.ModifiedAt).IsRequired();
            entity.Property(e => e.Question).HasMaxLength(1000);
            entity.Property(e => e.Reasoning).HasMaxLength(2000);
            entity.Property(e => e.ChangeReason).HasMaxLength(1000);
            
            // JSONB columns
            entity.Property(e => e.Value)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            entity.Property(e => e.Options)
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
            entity.HasIndex(e => e.DecisionId);
            entity.HasIndex(e => e.ModifiedAt);
            entity.HasIndex(e => e.VersionNumber);
            
            // Foreign keys
            entity.HasOne(e => e.Decision)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DecisionReview>(entity =>
        {
            entity.ToTable("decision_reviews");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.DecisionId).IsRequired();
            entity.Property(e => e.RequestedBy).IsRequired();
            entity.Property(e => e.RequestedAt).IsRequired();
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            // Indexes
            entity.HasIndex(e => e.DecisionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
            
            // Foreign keys
            entity.HasOne(e => e.Decision)
                .WithMany()
                .HasForeignKey(e => e.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Requester)
                .WithMany()
                .HasForeignKey(e => e.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DecisionReviewInvitation>(entity =>
        {
            entity.ToTable("decision_review_invitations");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ReviewId).IsRequired();
            entity.Property(e => e.ReviewerId).IsRequired();
            entity.Property(e => e.InvitedAt).IsRequired();
            
            // Unique constraint - one invitation per reviewer per review
            entity.HasIndex(e => new { e.ReviewId, e.ReviewerId }).IsUnique();
            
            // Indexes
            entity.HasIndex(e => e.ReviewId);
            entity.HasIndex(e => e.ReviewerId);
            
            // Foreign keys
            entity.HasOne(e => e.Review)
                .WithMany(r => r.Invitations)
                .HasForeignKey(e => e.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DecisionReviewResponse>(entity =>
        {
            entity.ToTable("decision_review_responses");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ReviewId).IsRequired();
            entity.Property(e => e.ReviewerId).IsRequired();
            entity.Property(e => e.ResponseType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Comments).HasMaxLength(2000);
            entity.Property(e => e.RespondedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.ReviewId);
            entity.HasIndex(e => e.ReviewerId);
            entity.HasIndex(e => e.RespondedAt);
            
            // Foreign keys
            entity.HasOne(e => e.Review)
                .WithMany(r => r.Responses)
                .HasForeignKey(e => e.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DecisionConflict>(entity =>
        {
            entity.ToTable("decision_conflicts");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.DecisionId1).IsRequired();
            entity.Property(e => e.DecisionId2).IsRequired();
            entity.Property(e => e.ConflictType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DetectedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Resolution).HasMaxLength(2000);
            entity.Property(e => e.OverrideJustification).HasMaxLength(2000);
            
            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.DecisionId1);
            entity.HasIndex(e => e.DecisionId2);
            
            // Foreign keys
            entity.HasOne(e => e.Decision1)
                .WithMany()
                .HasForeignKey(e => e.DecisionId1)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Decision2)
                .WithMany()
                .HasForeignKey(e => e.DecisionId2)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Resolver)
                .WithMany()
                .HasForeignKey(e => e.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConflictRule>(entity =>
        {
            entity.ToTable("conflict_rules");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ConflictType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // JSONB for Configuration
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
            
            // Indexes
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ConflictType);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
