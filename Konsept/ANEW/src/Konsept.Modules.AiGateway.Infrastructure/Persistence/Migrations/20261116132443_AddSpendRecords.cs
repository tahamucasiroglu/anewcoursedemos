using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpendRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spend_records",
                schema: "ai_gateway",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    provider_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    effort = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    image_count = table.Column<int>(type: "integer", nullable: true),
                    outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    is_estimated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spend_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_spend_records_user_time",
                schema: "ai_gateway",
                table: "spend_records",
                columns: new[] { "user_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spend_records",
                schema: "ai_gateway");
        }
    }
}
