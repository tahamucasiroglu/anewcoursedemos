using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "providers");

            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    protected_key = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    region = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    last_test_succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    last_tested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_settings",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_limit_usd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    stop_on_exceed = table.Column<bool>(type: "boolean", nullable: false),
                    warn_at_percent = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "call_logs",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    model_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_model_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_read_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_write_tokens = table.Column<long>(type: "bigint", nullable: false),
                    units = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cost_usd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_call_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "model_prices",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    input_per_m_tok = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    output_per_m_tok = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    cache_read_per_m_tok = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    cache_write_per_m_tok = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    per_image = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    per_k_chars = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    per_minute = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_model_prices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_user_id_platform",
                schema: "providers",
                table: "api_keys",
                columns: new[] { "user_id", "platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_settings_user_id",
                schema: "providers",
                table: "budget_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_user_id_created_at_utc",
                schema: "providers",
                table: "call_logs",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_model_prices_user_id_model_key_effective_from_utc",
                schema: "providers",
                table: "model_prices",
                columns: new[] { "user_id", "model_key", "effective_from_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "budget_settings",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "call_logs",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "model_prices",
                schema: "providers");
        }
    }
}
