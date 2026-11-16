using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "concept_settings",
                schema: "profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concept_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concept_effort = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    dedupe_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dedupe_effort = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    topic_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    topic_effort = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    tone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    narration = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    word_check = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_concept_settings", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_concept_settings_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profiles",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_concept_settings_user",
                schema: "profiles",
                table: "concept_settings",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "concept_settings",
                schema: "profiles");
        }
    }
}
