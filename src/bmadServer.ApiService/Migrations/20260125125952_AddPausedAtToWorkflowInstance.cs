using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddPausedAtToWorkflowInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "workflow_instances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_PausedAt",
                table: "workflow_instances",
                column: "PausedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_instances_PausedAt",
                table: "workflow_instances");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "workflow_instances");
        }
    }
}
