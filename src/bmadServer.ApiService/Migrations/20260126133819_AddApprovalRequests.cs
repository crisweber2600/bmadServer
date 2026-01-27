using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProposedResponse = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedResponse = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_requests_workflow_instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_RequestedAt",
                table: "approval_requests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_Status",
                table: "approval_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_WorkflowInstanceId",
                table: "approval_requests",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_WorkflowInstanceId_Status",
                table: "approval_requests",
                columns: new[] { "WorkflowInstanceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_requests");
        }
    }
}
