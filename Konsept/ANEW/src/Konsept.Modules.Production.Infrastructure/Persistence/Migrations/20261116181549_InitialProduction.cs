using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "production");

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    fixed_settings = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "production",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtitles = table.Column<bool>(type: "boolean", nullable: false),
                    progress_bar = table.Column<bool>(type: "boolean", nullable: false),
                    code_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    code_effort = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    engine = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    image_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    audio_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.profile_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_user_profile_position",
                schema: "production",
                table: "jobs",
                columns: new[] { "user_id", "profile_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_one_active_per_concept",
                schema: "production",
                table: "jobs",
                column: "concept_id",
                unique: true,
                filter: "status <> 5");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_one_running_per_user",
                schema: "production",
                table: "jobs",
                column: "user_id",
                unique: true,
                filter: "status IN (1, 3)");

            migrationBuilder.CreateIndex(
                name: "ix_settings_user",
                schema: "production",
                table: "settings",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jobs",
                schema: "production");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "production");
        }
    }
}
