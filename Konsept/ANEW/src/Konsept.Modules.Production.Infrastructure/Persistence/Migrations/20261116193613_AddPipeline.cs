using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "progress",
                schema: "production",
                table: "jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "queue_states",
                schema: "production",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    halted = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_states", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "stage_outputs",
                schema: "production",
                columns: table => new
                {
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan = table.Column<string>(type: "jsonb", nullable: true),
                    codes = table.Column<string>(type: "jsonb", nullable: true),
                    local_device = table.Column<Guid>(type: "uuid", nullable: true),
                    local_done = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_outputs", x => x.concept_id);
                });

            migrationBuilder.CreateTable(
                name: "videos",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    concept_text = table.Column<string>(type: "jsonb", nullable: false),
                    code_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    image_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    audio_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    engine = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitles = table.Column<bool>(type: "boolean", nullable: false),
                    progress_bar = table.Column<bool>(type: "boolean", nullable: false),
                    ai_voice_notice = table.Column<bool>(type: "boolean", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_videos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stage_outputs_user",
                schema: "production",
                table: "stage_outputs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_videos_user_profile_ended",
                schema: "production",
                table: "videos",
                columns: new[] { "user_id", "profile_id", "ended_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queue_states",
                schema: "production");

            migrationBuilder.DropTable(
                name: "stage_outputs",
                schema: "production");

            migrationBuilder.DropTable(
                name: "videos",
                schema: "production");

            migrationBuilder.DropColumn(
                name: "progress",
                schema: "production",
                table: "jobs");
        }
    }
}
