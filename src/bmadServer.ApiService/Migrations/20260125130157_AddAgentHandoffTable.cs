using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentHandoffTable : Migration
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
                    FromAgent = table.Column<string>(type: "text", nullable: true),
                    ToAgent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkflowStep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ToAgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FromAgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_handoffs", x => x.Id);
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
