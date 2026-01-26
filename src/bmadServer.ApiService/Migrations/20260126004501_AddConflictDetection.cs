using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddConflictDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conflict_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConflictType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conflict_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "decision_conflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId2 = table.Column<Guid>(type: "uuid", nullable: false),
                    ConflictType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OverrideJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_conflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_conflicts_decisions_DecisionId1",
                        column: x => x.DecisionId1,
                        principalTable: "decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_decision_conflicts_decisions_DecisionId2",
                        column: x => x.DecisionId2,
                        principalTable: "decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_decision_conflicts_users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conflict_rules_Configuration",
                table: "conflict_rules",
                column: "Configuration")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_conflict_rules_ConflictType",
                table: "conflict_rules",
                column: "ConflictType");

            migrationBuilder.CreateIndex(
                name: "IX_conflict_rules_CreatedAt",
                table: "conflict_rules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_conflict_rules_IsActive",
                table: "conflict_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_ConflictType",
                table: "decision_conflicts",
                column: "ConflictType");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_DecisionId1",
                table: "decision_conflicts",
                column: "DecisionId1");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_DecisionId1_DecisionId2",
                table: "decision_conflicts",
                columns: new[] { "DecisionId1", "DecisionId2" });

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_DecisionId2",
                table: "decision_conflicts",
                column: "DecisionId2");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_DetectedAt",
                table: "decision_conflicts",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_ResolvedBy",
                table: "decision_conflicts",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_Severity",
                table: "decision_conflicts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_decision_conflicts_Status",
                table: "decision_conflicts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conflict_rules");

            migrationBuilder.DropTable(
                name: "decision_conflicts");
        }
    }
}
