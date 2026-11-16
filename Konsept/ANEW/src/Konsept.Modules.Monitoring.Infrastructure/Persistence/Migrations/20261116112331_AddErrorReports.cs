using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "error_reports",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Page = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AgentVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Component = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Step = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Attachments = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_error_reports_CreatedAt",
                schema: "monitoring",
                table: "error_reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_error_reports_Number",
                schema: "monitoring",
                table: "error_reports",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_error_reports_UserId_CreatedAt",
                schema: "monitoring",
                table: "error_reports",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_reports",
                schema: "monitoring");
        }
    }
}
