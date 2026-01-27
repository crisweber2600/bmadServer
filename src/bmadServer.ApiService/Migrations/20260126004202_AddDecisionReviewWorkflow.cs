using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bmadServer.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "decisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "decision_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_reviews_decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_decision_reviews_users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "decision_review_responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_review_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_review_responses_decision_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "decision_reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_decision_review_responses_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_RespondedAt",
                table: "decision_review_responses",
                column: "RespondedAt");

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_ReviewerId",
                table: "decision_review_responses",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_ReviewId",
                table: "decision_review_responses",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_review_responses_ReviewId_ReviewerId",
                table: "decision_review_responses",
                columns: new[] { "ReviewId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_Deadline",
                table: "decision_reviews",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_DecisionId",
                table: "decision_reviews",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_RequestedAt",
                table: "decision_reviews",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_RequestedBy",
                table: "decision_reviews",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_decision_reviews_Status",
                table: "decision_reviews",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decision_review_responses");

            migrationBuilder.DropTable(
                name: "decision_reviews");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "decisions");
        }
    }
}
