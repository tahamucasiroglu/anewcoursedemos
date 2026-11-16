using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                schema: "monitoring",
                table: "system_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                schema: "monitoring",
                table: "system_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileName",
                schema: "monitoring",
                table: "system_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "monitoring",
                table: "system_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_events_UserId_OccurredAt",
                schema: "monitoring",
                table: "system_events",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_system_events_UserId_OccurredAt",
                schema: "monitoring",
                table: "system_events");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                schema: "monitoring",
                table: "system_events");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                schema: "monitoring",
                table: "system_events");

            migrationBuilder.DropColumn(
                name: "ProfileName",
                schema: "monitoring",
                table: "system_events");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "monitoring",
                table: "system_events");
        }
    }
}
