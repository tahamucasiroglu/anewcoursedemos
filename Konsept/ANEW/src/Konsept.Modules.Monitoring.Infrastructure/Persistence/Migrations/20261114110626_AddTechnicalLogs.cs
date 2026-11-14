using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "technical_logs",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Exception = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AgentVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_technical_logs_CorrelationId",
                schema: "monitoring",
                table: "technical_logs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_technical_logs_OccurredAt",
                schema: "monitoring",
                table: "technical_logs",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "technical_logs",
                schema: "monitoring");
        }
    }
}
