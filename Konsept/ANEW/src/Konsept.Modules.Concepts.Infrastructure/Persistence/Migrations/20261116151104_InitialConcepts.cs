using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Concepts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialConcepts : Migration
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
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    title_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    scenes = table.Column<string>(type: "jsonb", nullable: false),
                    word_repetition_warning = table.Column<bool>(type: "boolean", nullable: false),
                    duration_mismatch_warning = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_concepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topic_suggestions",
                schema: "concepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    title_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_suggestions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_concepts_user_profile_created",
                schema: "concepts",
                table: "concepts",
                columns: new[] { "user_id", "profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_topic_suggestions_user_profile",
                schema: "concepts",
                table: "topic_suggestions",
                columns: new[] { "user_id", "profile_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "concepts",
                schema: "concepts");

            migrationBuilder.DropTable(
                name: "topic_suggestions",
                schema: "concepts");
        }
    }
}
