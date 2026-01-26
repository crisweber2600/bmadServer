using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentHandoffsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_handoffs");
        }
    }
}
