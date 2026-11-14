using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "monitoring");

            migrationBuilder.CreateTable(
                name: "system_events",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_system_events_OccurredAt",
                schema: "monitoring",
                table: "system_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_system_events_UserId",
                schema: "monitoring",
                table: "system_events",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_events",
                schema: "monitoring");
        }
    }
}
