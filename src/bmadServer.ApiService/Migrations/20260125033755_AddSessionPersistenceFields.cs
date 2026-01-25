using System;
using Microsoft.EntityFrameworkCore.Migrations;
using bmadServer.ApiService.Models;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPersistenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<WorkflowState>(
                name: "WorkflowState",
                table: "sessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "sessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

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
                name: "IX_sessions_WorkflowState",
                table: "sessions",
                column: "WorkflowState")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Session_Expiry",
                table: "sessions",
                sql: "\"ExpiresAt\" > \"CreatedAt\"");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_sessions_ConnectionId",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_ExpiresAt",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_IsActive",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_WorkflowState",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Session_Expiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "WorkflowState",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "sessions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
