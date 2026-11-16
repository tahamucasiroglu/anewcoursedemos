using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_usd",
                schema: "production",
                table: "videos",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "duration_seconds",
                schema: "production",
                table: "videos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "format",
                schema: "production",
                table: "videos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "landscape");

            migrationBuilder.CreateIndex(
                name: "ix_videos_user_concept_ended",
                schema: "production",
                table: "videos",
                columns: new[] { "user_id", "concept_id", "ended_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_videos_user_concept_ended",
                schema: "production",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "cost_usd",
                schema: "production",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "duration_seconds",
                schema: "production",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "format",
                schema: "production",
                table: "videos");
        }
    }
}
