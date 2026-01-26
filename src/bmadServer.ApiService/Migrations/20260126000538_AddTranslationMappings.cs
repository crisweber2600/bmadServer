using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "translation_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translation_mappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_translation_mappings_IsActive",
                table: "translation_mappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_translation_mappings_TechnicalTerm",
                table: "translation_mappings",
                column: "TechnicalTerm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "translation_mappings");
        }
    }
}
