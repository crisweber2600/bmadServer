using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionVersionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "decisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "decisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "decisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "decision_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "jsonb", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Question = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    Reasoning = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Context = table.Column<string>(type: "jsonb", nullable: true)
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
                        name: "FK_decision_versions_users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_Context",
                table: "decision_versions",
                column: "Context")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_DecisionId",
                table: "decision_versions",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_DecisionId_VersionNumber",
                table: "decision_versions",
                columns: new[] { "DecisionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_ModifiedAt",
                table: "decision_versions",
                column: "ModifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_ModifiedBy",
                table: "decision_versions",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_Options",
                table: "decision_versions",
                column: "Options")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_Value",
                table: "decision_versions",
                column: "Value")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_decision_versions_VersionNumber",
                table: "decision_versions",
                column: "VersionNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decision_versions");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "decisions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "decisions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "decisions");
        }
    }
}
