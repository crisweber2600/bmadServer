using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddConflictsAndBufferedInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternativesConsidered",
                table: "workflow_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "workflow_events",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputType",
                table: "workflow_events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "workflow_events",
                type: "jsonb",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_workflow_events_user_time",
                table: "workflow_events");

            migrationBuilder.DropIndex(
                name: "IX_workflow_events_AlternativesConsidered",
                table: "workflow_events");

            migrationBuilder.DropIndex(
                name: "IX_workflow_events_Payload",
                table: "workflow_events");

            migrationBuilder.DropColumn(
                name: "AlternativesConsidered",
                table: "workflow_events");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "workflow_events");

            migrationBuilder.DropColumn(
                name: "InputType",
                table: "workflow_events");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "workflow_events");
        }
    }
}
