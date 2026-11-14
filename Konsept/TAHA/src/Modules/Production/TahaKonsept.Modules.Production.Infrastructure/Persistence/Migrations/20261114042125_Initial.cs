using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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
                    queue_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    waiting_for_agent = table.Column<bool>(type: "boolean", nullable: false),
                    video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_video_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    fps = table.Column<int>(type: "integer", nullable: false),
                    settings_json = table.Column<string>(type: "text", nullable: false),
                    document_json = table.Column<string>(type: "text", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_queues",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    budget_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_queues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_settings",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settings_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pronunciations",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    written = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    written_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    spoken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pronunciations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "queue_items",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    video_id = table.Column<Guid>(type: "uuid", nullable: true),
                    settings_json = table.Column<string>(type: "text", nullable: true),
                    document_json = table.Column<string>(type: "text", nullable: true),
                    base_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_video_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "videos",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    fps = table.Column<int>(type: "integer", nullable: false),
                    duration_sec = table.Column<double>(type: "double precision", nullable: false),
                    total_cost_usd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cost_breakdown_json = table.Column<string>(type: "text", nullable: false),
                    settings_json = table.Column<string>(type: "text", nullable: false),
                    document_json = table.Column<string>(type: "text", nullable: false),
                    scenes_json = table.Column<string>(type: "text", nullable: false),
                    agent_video_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    parent_video_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_videos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stage_runs",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    stage = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    scene_index = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    input_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    output_hash = table.Column<string>(type: "character varying(136)", maxLength: 136, nullable: true),
                    output_json = table.Column<string>(type: "text", nullable: true),
                    artifacts_json = table.Column<string>(type: "text", nullable: true),
                    reused = table.Column<bool>(type: "boolean", nullable: false),
                    reused_from_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reuse_candidate = table.Column<bool>(type: "boolean", nullable: false),
                    reuse_disabled = table.Column<bool>(type: "boolean", nullable: false),
                    command_id = table.Column<Guid>(type: "uuid", nullable: true),
                    progress = table.Column<double>(type: "double precision", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    repair_count = table.Column<int>(type: "integer", nullable: false),
                    repair_hint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_progress_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stage_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_stage_runs_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "production",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_user_id_status",
                schema: "production",
                table: "jobs",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_production_queues_user_id",
                schema: "production",
                table: "production_queues",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_production_settings_user_id",
                schema: "production",
                table: "production_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pronunciations_user_id_written_key",
                schema: "production",
                table: "pronunciations",
                columns: new[] { "user_id", "written_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_items_user_id_status_order",
                schema: "production",
                table: "queue_items",
                columns: new[] { "user_id", "status", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_stage_runs_command_id",
                schema: "production",
                table: "stage_runs",
                column: "command_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_runs_job_id_sequence",
                schema: "production",
                table: "stage_runs",
                columns: new[] { "job_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_videos_user_id_concept_id",
                schema: "production",
                table: "videos",
                columns: new[] { "user_id", "concept_id" });

            migrationBuilder.CreateIndex(
                name: "ix_videos_user_id_created_at_utc",
                schema: "production",
                table: "videos",
                columns: new[] { "user_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_queues",
                schema: "production");

            migrationBuilder.DropTable(
                name: "production_settings",
                schema: "production");

            migrationBuilder.DropTable(
                name: "pronunciations",
                schema: "production");

            migrationBuilder.DropTable(
                name: "queue_items",
                schema: "production");

            migrationBuilder.DropTable(
                name: "stage_runs",
                schema: "production");

            migrationBuilder.DropTable(
                name: "videos",
                schema: "production");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "production");
        }
    }
}
