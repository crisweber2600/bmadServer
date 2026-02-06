using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddSparkCompatEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spark_compat_chats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Feature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_chats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_collaboration_events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PrId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    workflow_metadata_json = table.Column<string>(type: "text", nullable: true),
                    decision_metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_collaboration_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_decisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    value_json = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_presence_snapshots",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActiveChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsTyping = table.Column<bool>(type: "boolean", nullable: false),
                    TypingChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    cursor_position_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_presence_snapshots", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_pull_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approvals_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_pull_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    file_changes_json = table.Column<string>(type: "text", nullable: true),
                    workflow_context_json = table.Column<string>(type: "text", nullable: true),
                    attribution_json = table.Column<string>(type: "text", nullable: true),
                    persona_metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_messages_spark_compat_chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "spark_compat_chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_decision_conflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConflictType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_json = table.Column<string>(type: "text", nullable: true),
                    audit_metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_decision_conflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_decision_conflicts_spark_compat_decisions_Deci~",
                        column: x => x.DecisionId,
                        principalTable: "spark_compat_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_decision_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    value_json = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    audit_metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_decision_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_decision_versions_spark_compat_decisions_Decis~",
                        column: x => x.DecisionId,
                        principalTable: "spark_compat_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_line_comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PullRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ParentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    LineType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorAvatar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_line_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_line_comments_spark_compat_pull_requests_PullR~",
                        column: x => x.PullRequestId,
                        principalTable: "spark_compat_pull_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_pr_comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PullRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_pr_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_pr_comments_spark_compat_pull_requests_PullReq~",
                        column: x => x.PullRequestId,
                        principalTable: "spark_compat_pull_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_pr_file_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PullRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    additions_json = table.Column<string>(type: "text", nullable: false),
                    deletions_json = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_pr_file_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_pr_file_changes_spark_compat_pull_requests_Pul~",
                        column: x => x.PullRequestId,
                        principalTable: "spark_compat_pull_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spark_compat_line_comment_reactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineCommentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Emoji = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_compat_line_comment_reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spark_compat_line_comment_reactions_spark_compat_line_comme~",
                        column: x => x.LineCommentId,
                        principalTable: "spark_compat_line_comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_chats_CreatedByUserId",
                table: "spark_compat_chats",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_chats_UpdatedAt",
                table: "spark_compat_chats",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_spark_events_time",
                table: "spark_compat_collaboration_events",
                columns: new[] { "Timestamp", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_collaboration_events_ChatId",
                table: "spark_compat_collaboration_events",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_decision_conflicts_DecisionId_Status",
                table: "spark_compat_decision_conflicts",
                columns: new[] { "DecisionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ux_spark_decision_version",
                table: "spark_compat_decision_versions",
                columns: new[] { "DecisionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_decisions_ChatId",
                table: "spark_compat_decisions",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_decisions_ChatId_Title",
                table: "spark_compat_decisions",
                columns: new[] { "ChatId", "Title" });

            migrationBuilder.CreateIndex(
                name: "ux_spark_line_reaction",
                table: "spark_compat_line_comment_reactions",
                columns: new[] { "LineCommentId", "Emoji", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_line_comments_FileId",
                table: "spark_compat_line_comments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_line_comments_ParentId",
                table: "spark_compat_line_comments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_line_comments_PullRequestId",
                table: "spark_compat_line_comments",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "idx_spark_messages_chat_time",
                table: "spark_compat_messages",
                columns: new[] { "ChatId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_pr_comments_PullRequestId_Timestamp",
                table: "spark_compat_pr_comments",
                columns: new[] { "PullRequestId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_pr_file_changes_PullRequestId",
                table: "spark_compat_pr_file_changes",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_presence_snapshots_ActiveChatId",
                table: "spark_compat_presence_snapshots",
                column: "ActiveChatId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_presence_snapshots_LastSeenAt",
                table: "spark_compat_presence_snapshots",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_pull_requests_ChatId",
                table: "spark_compat_pull_requests",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_pull_requests_Status",
                table: "spark_compat_pull_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_pull_requests_UpdatedAt",
                table: "spark_compat_pull_requests",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spark_compat_collaboration_events");

            migrationBuilder.DropTable(
                name: "spark_compat_decision_conflicts");

            migrationBuilder.DropTable(
                name: "spark_compat_decision_versions");

            migrationBuilder.DropTable(
                name: "spark_compat_line_comment_reactions");

            migrationBuilder.DropTable(
                name: "spark_compat_messages");

            migrationBuilder.DropTable(
                name: "spark_compat_pr_comments");

            migrationBuilder.DropTable(
                name: "spark_compat_pr_file_changes");

            migrationBuilder.DropTable(
                name: "spark_compat_presence_snapshots");

            migrationBuilder.DropTable(
                name: "spark_compat_decisions");

            migrationBuilder.DropTable(
                name: "spark_compat_line_comments");

            migrationBuilder.DropTable(
                name: "spark_compat_chats");

            migrationBuilder.DropTable(
                name: "spark_compat_pull_requests");
        }
    }
}
