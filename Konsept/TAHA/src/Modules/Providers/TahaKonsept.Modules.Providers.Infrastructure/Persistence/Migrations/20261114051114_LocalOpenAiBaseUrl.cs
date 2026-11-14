using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocalOpenAiBaseUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "protected_base_url",
                schema: "providers",
                table: "api_keys",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "protected_base_url",
                schema: "providers",
                table: "api_keys");
        }
    }
}
