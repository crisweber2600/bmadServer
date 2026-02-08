using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddPullRequestBranchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceBranch",
                table: "spark_compat_pull_requests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetBranch",
                table: "spark_compat_pull_requests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "spark_compat_presence_snapshots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "chat");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "spark_compat_presence_snapshots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "online");

            migrationBuilder.AddColumn<string>(
                name: "translations_json",
                table: "spark_compat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "spark_compat_chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_spark_compat_presence_snapshots_Domain",
                table: "spark_compat_presence_snapshots",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "idx_spark_chats_domain",
                table: "spark_compat_chats",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "idx_spark_chats_is_deleted",
                table: "spark_compat_chats",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_spark_compat_presence_snapshots_Domain",
                table: "spark_compat_presence_snapshots");

            migrationBuilder.DropIndex(
                name: "idx_spark_chats_domain",
                table: "spark_compat_chats");

            migrationBuilder.DropIndex(
                name: "idx_spark_chats_is_deleted",
                table: "spark_compat_chats");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SourceBranch",
                table: "spark_compat_pull_requests");

            migrationBuilder.DropColumn(
                name: "TargetBranch",
                table: "spark_compat_pull_requests");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "spark_compat_presence_snapshots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "spark_compat_presence_snapshots");

            migrationBuilder.DropColumn(
                name: "translations_json",
                table: "spark_compat_messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "spark_compat_chats");
        }
    }
}
