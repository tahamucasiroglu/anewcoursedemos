using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfiles : Migration
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
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                    table.CheckConstraint("ck_profiles_duration", "duration_seconds BETWEEN 15 AND 900");
                    table.CheckConstraint("ck_profiles_format", "format IN ('landscape', 'portrait')");
                    table.CheckConstraint("ck_profiles_language", "language IN ('tr', 'en')");
                });

            migrationBuilder.CreateTable(
                name: "selections",
                schema: "profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_selections", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_profiles_user_name_key",
                schema: "profiles",
                table: "profiles",
                columns: new[] { "user_id", "name_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profiles");

            migrationBuilder.DropTable(
                name: "selections",
                schema: "profiles");
        }
    }
}
