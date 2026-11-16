using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAiGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ai_gateway");

            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "ai_gateway",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ciphertext = table.Column<string>(type: "text", nullable: false),
                    hint = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    validation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                    table.CheckConstraint("ck_api_keys_validation", "validation IN ('verified', 'unverified')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_api_keys_user_provider",
                schema: "ai_gateway",
                table: "api_keys",
                columns: new[] { "user_id", "provider_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "ai_gateway");
        }
    }
}
