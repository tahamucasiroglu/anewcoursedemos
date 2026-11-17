using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "signatures",
                schema: "production",
                table: "stage_outputs",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "version",
                schema: "production",
                table: "jobs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "signatures",
                schema: "production",
                table: "stage_outputs");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "production",
                table: "jobs");
        }
    }
}
