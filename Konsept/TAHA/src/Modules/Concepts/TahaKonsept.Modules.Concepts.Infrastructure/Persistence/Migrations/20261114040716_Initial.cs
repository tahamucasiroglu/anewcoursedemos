using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "concepts");

            migrationBuilder.CreateTable(
                name: "concepts",
                schema: "concepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    origin = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    model_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    revision_of_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "concepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_model_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    concept_effort = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    topic_model_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    word_check_model_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    word_repetition_check = table.Column<bool>(type: "boolean", nullable: false),
                    tone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    audience = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    narrative_style = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    fact_grounding = table.Column<bool>(type: "boolean", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topic_memory",
                schema: "concepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    keywords = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    hash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    concept_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topic_memory", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_concepts_user_id_status",
                schema: "concepts",
                table: "concepts",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_settings_user_id",
                schema: "concepts",
                table: "settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_topic_memory_concept_id",
                schema: "concepts",
                table: "topic_memory",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_topic_memory_user_id_profile_id",
                schema: "concepts",
                table: "topic_memory",
                columns: new[] { "user_id", "profile_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "concepts",
                schema: "concepts");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "concepts");

            migrationBuilder.DropTable(
                name: "topic_memory",
                schema: "concepts");
        }
    }
}
