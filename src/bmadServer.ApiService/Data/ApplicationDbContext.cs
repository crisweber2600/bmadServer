using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Data.Entities.SparkCompat;
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
    public DbSet<AgentHandoff> AgentHandoffs { get; set; }
    public DbSet<Decision> Decisions { get; set; }
    public DbSet<DecisionVersion> DecisionVersions { get; set; }
    public DbSet<DecisionConflict> DecisionConflicts { get; set; }
    public DbSet<DecisionReview> DecisionReviews { get; set; }
    public DbSet<DecisionReviewResponse> DecisionReviewResponses { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<AgentMessageLog> AgentMessageLogs { get; set; }
    public DbSet<ConflictRule> ConflictRules { get; set; }
    public DbSet<TranslationMapping> TranslationMappings { get; set; }
    public DbSet<SparkCompatChat> SparkCompatChats { get; set; }
    public DbSet<SparkCompatMessage> SparkCompatMessages { get; set; }
    public DbSet<SparkCompatPresenceSnapshot> SparkCompatPresenceSnapshots { get; set; }
    public DbSet<SparkCompatPullRequest> SparkCompatPullRequests { get; set; }
    public DbSet<SparkCompatPullRequestFileChange> SparkCompatPullRequestFileChanges { get; set; }
    public DbSet<SparkCompatPullRequestComment> SparkCompatPullRequestComments { get; set; }
    public DbSet<SparkCompatLineComment> SparkCompatLineComments { get; set; }
    public DbSet<SparkCompatLineCommentReaction> SparkCompatLineCommentReactions { get; set; }
    public DbSet<SparkCompatCollaborationEvent> SparkCompatCollaborationEvents { get; set; }
    public DbSet<SparkCompatDecision> SparkCompatDecisions { get; set; }
    public DbSet<SparkCompatDecisionVersion> SparkCompatDecisionVersions { get; set; }
    public DbSet<SparkCompatDecisionConflict> SparkCompatDecisionConflicts { get; set; }

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
            entity.Property(e => e.AvatarUrl).HasMaxLength(1000);
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

        // Configure Decision entity with DecisionVersion relationship
        modelBuilder.Entity<Decision>(entity =>
        {
            entity.ToTable("decisions");
            entity.HasKey(e => e.Id);

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

            // Configure the relationship with DecisionVersion
            entity.HasMany(e => e.Versions)
                .WithOne(v => v.Decision)
                .HasForeignKey(v => v.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DecisionVersion entity
        modelBuilder.Entity<DecisionVersion>(entity =>
        {
            entity.ToTable("decision_versions");
            entity.HasKey(e => e.Id);

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
        });

        modelBuilder.Entity<ConflictRule>(entity =>
        {
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : v.RootElement.GetRawText(),
                    v => v == null ? null : JsonDocument.Parse(v));
        });

        // Configure DecisionReview entity with Responses relationship
        modelBuilder.Entity<DecisionReview>(entity =>
        {
            entity.ToTable("decision_reviews");
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.Responses)
                .WithOne(r => r.Review)
                .HasForeignKey(r => r.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DecisionReviewResponse entity
        modelBuilder.Entity<DecisionReviewResponse>(entity =>
        {
            entity.ToTable("decision_review_responses");
            entity.HasKey(e => e.Id);
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

        modelBuilder.Entity<SparkCompatChat>(entity =>
        {
            entity.ToTable("spark_compat_chats");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Domain).HasMaxLength(200);
            entity.Property(e => e.Service).HasMaxLength(200);
            entity.Property(e => e.Feature).HasMaxLength(200);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("idx_spark_chats_is_deleted");
            entity.HasIndex(e => e.Domain)
                .HasDatabaseName("idx_spark_chats_domain");
        });

        modelBuilder.Entity<SparkCompatMessage>(entity =>
        {
            entity.ToTable("spark_compat_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.ChatId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.FileChangesJson).HasColumnName("file_changes_json");
            entity.Property(e => e.WorkflowContextJson).HasColumnName("workflow_context_json");
            entity.Property(e => e.AttributionJson).HasColumnName("attribution_json");
            entity.Property(e => e.PersonaMetadataJson).HasColumnName("persona_metadata_json");
            entity.Property(e => e.TranslationsJson).HasColumnName("translations_json");
            entity.HasIndex(e => new { e.ChatId, e.Timestamp })
                .HasDatabaseName("idx_spark_messages_chat_time");
            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatPresenceSnapshot>(entity =>
        {
            entity.ToTable("spark_compat_presence_snapshots");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AvatarUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32).HasDefaultValue("online");
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(32).HasDefaultValue("chat");
            entity.Property(e => e.ActiveChatId).HasMaxLength(64);
            entity.Property(e => e.TypingChatId).HasMaxLength(64);
            entity.Property(e => e.CursorPositionJson).HasColumnName("cursor_position_json");
            entity.HasIndex(e => e.LastSeenAt);
            entity.HasIndex(e => e.ActiveChatId);
            entity.HasIndex(e => e.Domain);
        });

        modelBuilder.Entity<SparkCompatPullRequest>(entity =>
        {
            entity.ToTable("spark_compat_pull_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.ChatId).HasMaxLength(64);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.SourceBranch).HasMaxLength(256).HasDefaultValue(string.Empty);
            entity.Property(e => e.TargetBranch).HasMaxLength(256).HasDefaultValue(string.Empty);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ApprovalsJson).IsRequired().HasColumnName("approvals_json");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<SparkCompatPullRequestFileChange>(entity =>
        {
            entity.ToTable("spark_compat_pr_file_changes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PullRequestId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.AdditionsJson).IsRequired().HasColumnName("additions_json");
            entity.Property(e => e.DeletionsJson).IsRequired().HasColumnName("deletions_json");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasOne(e => e.PullRequest)
                .WithMany(pr => pr.FileChanges)
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatPullRequestComment>(entity =>
        {
            entity.ToTable("spark_compat_pr_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.PullRequestId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.PullRequestId, e.Timestamp });
            entity.HasOne(e => e.PullRequest)
                .WithMany(pr => pr.Comments)
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatLineComment>(entity =>
        {
            entity.ToTable("spark_compat_line_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.PullRequestId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.FileId).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.ParentId).HasMaxLength(64);
            entity.Property(e => e.LineType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AuthorAvatar).HasMaxLength(1000);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasOne(e => e.PullRequest)
                .WithMany(pr => pr.LineComments)
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatLineCommentReaction>(entity =>
        {
            entity.ToTable("spark_compat_line_comment_reactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LineCommentId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Emoji).IsRequired().HasMaxLength(32);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => new { e.LineCommentId, e.Emoji, e.UserId })
                .IsUnique()
                .HasDatabaseName("ux_spark_line_reaction");
            entity.HasOne(e => e.LineComment)
                .WithMany(comment => comment.Reactions)
                .HasForeignKey(e => e.LineCommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatCollaborationEvent>(entity =>
        {
            entity.ToTable("spark_compat_collaboration_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(64);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ChatId).HasMaxLength(64);
            entity.Property(e => e.PrId).HasMaxLength(64);
            entity.Property(e => e.MetadataJson).HasColumnName("metadata_json");
            entity.Property(e => e.WorkflowMetadataJson).HasColumnName("workflow_metadata_json");
            entity.Property(e => e.DecisionMetadataJson).HasColumnName("decision_metadata_json");
            entity.HasIndex(e => new { e.Timestamp, e.Id })
                .HasDatabaseName("idx_spark_events_time");
            entity.HasIndex(e => e.ChatId);
        });

        modelBuilder.Entity<SparkCompatDecision>(entity =>
        {
            entity.ToTable("spark_compat_decisions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.ChatId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.ValueJson).IsRequired().HasColumnName("value_json");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => new { e.ChatId, e.Title });
        });

        modelBuilder.Entity<SparkCompatDecisionVersion>(entity =>
        {
            entity.ToTable("spark_compat_decision_versions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ValueJson).IsRequired().HasColumnName("value_json");
            entity.Property(e => e.AuditMetadataJson).HasColumnName("audit_metadata_json");
            entity.HasIndex(e => new { e.DecisionId, e.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ux_spark_decision_version");
            entity.HasOne(e => e.Decision)
                .WithMany(decision => decision.Versions)
                .HasForeignKey(e => e.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SparkCompatDecisionConflict>(entity =>
        {
            entity.ToTable("spark_compat_decision_conflicts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ConflictType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ResolutionJson).HasColumnName("resolution_json");
            entity.Property(e => e.AuditMetadataJson).HasColumnName("audit_metadata_json");
            entity.HasIndex(e => new { e.DecisionId, e.Status });
            entity.HasOne(e => e.Decision)
                .WithMany(decision => decision.Conflicts)
                .HasForeignKey(e => e.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TranslationMapping>(entity =>
        {
            entity.ToTable("translation_mappings");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TechnicalTerm)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.BusinessTerm)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Context)
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Unique index on TechnicalTerm (case-insensitive for PostgreSQL)
            entity.HasIndex(e => e.TechnicalTerm)
                .IsUnique();

            entity.HasIndex(e => e.IsActive);
        });
    }
}
