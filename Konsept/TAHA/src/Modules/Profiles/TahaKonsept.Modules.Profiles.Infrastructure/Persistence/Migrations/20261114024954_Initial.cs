using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profiles");

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_duration_sec = table.Column<int>(type: "integer", nullable: false),
                    channel_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    default_tone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_audience = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_narrative_style = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    template_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_profiles_user_id_is_active",
                schema: "profiles",
                table: "profiles",
                columns: new[] { "user_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profiles");
        }
    }
}
