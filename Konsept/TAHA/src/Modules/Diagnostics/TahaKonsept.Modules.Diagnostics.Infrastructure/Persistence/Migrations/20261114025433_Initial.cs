using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "diagnostics");

            migrationBuilder.CreateTable(
                name: "activity_logs",
                schema: "diagnostics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "error_reports",
                schema: "diagnostics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    details = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: true),
                    page_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    app_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    machine = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status_changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_created_at_utc",
                schema: "diagnostics",
                table: "activity_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_level_created_at_utc",
                schema: "diagnostics",
                table: "activity_logs",
                columns: new[] { "level", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_user_id_created_at_utc",
                schema: "diagnostics",
                table: "activity_logs",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_error_reports_created_at_utc",
                schema: "diagnostics",
                table: "error_reports",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_error_reports_status_created_at_utc",
                schema: "diagnostics",
                table: "error_reports",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_logs",
                schema: "diagnostics");

            migrationBuilder.DropTable(
                name: "error_reports",
                schema: "diagnostics");
        }
    }
}
