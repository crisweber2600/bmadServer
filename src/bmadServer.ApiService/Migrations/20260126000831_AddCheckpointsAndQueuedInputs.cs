using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckpointsAndQueuedInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "workflow_participants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queued_inputs");

            migrationBuilder.DropTable(
                name: "workflow_checkpoints");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "workflow_participants",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
