using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConflictRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ConflictType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Configuration = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "translation_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translation_mappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PersonaType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StepData = table.Column<string>(type: "jsonb", nullable: true),
                    Context = table.Column<string>(type: "jsonb", nullable: true),
                    shared_context = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "text", nullable: true),
                    ReplacedByTokenId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: true),
                    WorkflowState = table.Column<string>(type: "jsonb", nullable: true),
                    SessionPersona = table.Column<int>(type: "integer", nullable: true),
                    PersonaSwitchCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                    table.CheckConstraint("CK_Session_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.Role });
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_handoffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ToAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkflowStepId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_handoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_handoffs_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_message_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceAgent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetAgent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_message_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_message_logs_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProposedResponse = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedResponse = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "text", nullable: false),
                    DecisionType = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    Context = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockReason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DecisionMakerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decisions_users_DecisionMakerId",
                        column: x => x.DecisionMakerId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_decisions_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queued_inputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Queued"),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queued_inputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_queued_inputs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_queued_inputs_workflow_instances_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CheckpointType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StateSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_checkpoints_users_TriggeredBy",
                        column: x => x.TriggeredBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_checkpoints_workflow_instances_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldStatus = table.Column<string>(type: "text", nullable: true),
                    NewStatus = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    InputType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AlternativesConsidered = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_events_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_participants_workflow_instances_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_step_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Input = table.Column<string>(type: "jsonb", nullable: true),
                    Output = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_step_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_step_histories_workflow_instances_WorkflowInstance~",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    inputs = table.Column<string>(type: "jsonb", nullable: false),
                    resolution = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EscalationRetries = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conflicts_workflows_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decision_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewerIds = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_reviews_decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_decision_reviews_users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "decision_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeReason = table.Column<string>(type: "text", nullable: true),
                    Question = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    Context = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_versions_decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_decision_versions_users_ModifierId",
                        column: x => x.ModifierId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DecisionConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId2 = table.Column<Guid>(type: "uuid", nullable: false),
                    ConflictType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    OverrideJustification = table.Column<string>(type: "text", nullable: true),
                    Decision1Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision2Id = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolverId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionConflicts_decisions_Decision1Id",
                        column: x => x.Decision1Id,
                        principalTable: "decisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DecisionConflicts_decisions_Decision2Id",
                        column: x => x.Decision2Id,
                        principalTable: "decisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DecisionConflicts_users_ResolverId",
                        column: x => x.ResolverId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "buffered_inputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buffered_inputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buffered_inputs_conflicts_ConflictId",
                        column: x => x.ConflictId,
                        principalTable: "conflicts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_buffered_inputs_workflows_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decision_review_responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseType = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_review_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_review_responses_decision_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "decision_reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_decision_review_responses_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_handoffs_Timestamp",
                table: "agent_handoffs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_agent_handoffs_WorkflowInstanceId",
                table: "agent_handoffs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_handoffs_WorkflowInstanceId_Timestamp",
                table: "agent_handoffs",
                columns: new[] { "WorkflowInstanceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_agent_message_logs_Content",
                table: "agent_message_logs",
                column: "Content")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_agent_message_logs_CorrelationId",
                table: "agent_message_logs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_message_logs_Timestamp",
                table: "agent_message_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_agent_message_logs_WorkflowInstanceId",
                table: "agent_message_logs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowInstanceId",
                table: "ApprovalRequests",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_buffered_inputs_ConflictId",
                table: "buffered_inputs",
                column: "ConflictId");

            migrationBuilder.CreateIndex(
                name: "IX_buffered_inputs_WorkflowInstanceId_FieldName_IsApplied",
                table: "buffered_inputs",
                columns: new[] { "WorkflowInstanceId", "FieldName", "IsApplied" });

            migrationBuilder.CreateIndex(
                name: "IX_conflicts_ExpiresAt",
                table: "conflicts",
                column: "ExpiresAt",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_conflicts_Status",
                table: "conflicts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_conflicts_WorkflowInstanceId",
                table: "conflicts",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_ReviewerId",
                table: "decision_review_responses",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_ReviewId",
                table: "decision_review_responses",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_DecisionId",
                table: "decision_reviews",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_RequesterId",
                table: "decision_reviews",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_DecisionId",
                table: "decision_versions",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_ModifierId",
                table: "decision_versions",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionConflicts_Decision1Id",
                table: "DecisionConflicts",
                column: "Decision1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionConflicts_Decision2Id",
                table: "DecisionConflicts",
                column: "Decision2Id");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionConflicts_ResolverId",
                table: "DecisionConflicts",
                column: "ResolverId");

            migrationBuilder.CreateIndex(
                name: "IX_decisions_DecisionMakerId",
                table: "decisions",
                column: "DecisionMakerId");

            migrationBuilder.CreateIndex(
                name: "IX_decisions_WorkflowInstanceId",
                table: "decisions",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "idx_queued_inputs_user",
                table: "queued_inputs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_queued_inputs_workflow_status",
                table: "queued_inputs",
                columns: new[] { "WorkflowId", "Status", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_queued_inputs_Content",
                table: "queued_inputs",
                column: "Content")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ConnectionId",
                table: "sessions",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ExpiresAt",
                table: "sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_IsActive",
                table: "sessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_UserId",
                table: "sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_WorkflowState",
                table: "sessions",
                column: "WorkflowState")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_translation_mappings_IsActive",
                table: "translation_mappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_translation_mappings_TechnicalTerm",
                table: "translation_mappings",
                column: "TechnicalTerm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId",
                table: "user_roles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_checkpoints_version",
                table: "workflow_checkpoints",
                columns: new[] { "WorkflowId", "Version" });

            migrationBuilder.CreateIndex(
                name: "idx_checkpoints_workflow_time",
                table: "workflow_checkpoints",
                columns: new[] { "WorkflowId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_checkpoints_Metadata",
                table: "workflow_checkpoints",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_checkpoints_StateSnapshot",
                table: "workflow_checkpoints",
                column: "StateSnapshot")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_checkpoints_TriggeredBy",
                table: "workflow_checkpoints",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "idx_workflow_events_user_time",
                table: "workflow_events",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_AlternativesConsidered",
                table: "workflow_events",
                column: "AlternativesConsidered")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_Payload",
                table: "workflow_events",
                column: "Payload")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_Timestamp",
                table: "workflow_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_WorkflowInstanceId",
                table: "workflow_events",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_CancelledAt",
                table: "workflow_instances",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_Context",
                table: "workflow_instances",
                column: "Context")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_CreatedAt",
                table: "workflow_instances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_PausedAt",
                table: "workflow_instances",
                column: "PausedAt");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_shared_context",
                table: "workflow_instances",
                column: "shared_context")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_Status",
                table: "workflow_instances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_StepData",
                table: "workflow_instances",
                column: "StepData")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_UserId",
                table: "workflow_instances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_participants_UserId",
                table: "workflow_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_participants_WorkflowId",
                table: "workflow_participants",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_participants_WorkflowId_UserId",
                table: "workflow_participants",
                columns: new[] { "WorkflowId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_Input",
                table: "workflow_step_histories",
                column: "Input")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_Output",
                table: "workflow_step_histories",
                column: "Output")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_StartedAt",
                table: "workflow_step_histories",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_Status",
                table: "workflow_step_histories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_StepId",
                table: "workflow_step_histories",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_histories_WorkflowInstanceId",
                table: "workflow_step_histories",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_handoffs");

            migrationBuilder.DropTable(
                name: "agent_message_logs");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "buffered_inputs");

            migrationBuilder.DropTable(
                name: "ConflictRules");

            migrationBuilder.DropTable(
                name: "decision_review_responses");

            migrationBuilder.DropTable(
                name: "decision_versions");

            migrationBuilder.DropTable(
                name: "DecisionConflicts");

            migrationBuilder.DropTable(
                name: "queued_inputs");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "translation_mappings");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "workflow_checkpoints");

            migrationBuilder.DropTable(
                name: "workflow_events");

            migrationBuilder.DropTable(
                name: "workflow_participants");

            migrationBuilder.DropTable(
                name: "workflow_step_histories");

            migrationBuilder.DropTable(
                name: "conflicts");

            migrationBuilder.DropTable(
                name: "decision_reviews");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "decisions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "workflow_instances");
        }
    }
}
