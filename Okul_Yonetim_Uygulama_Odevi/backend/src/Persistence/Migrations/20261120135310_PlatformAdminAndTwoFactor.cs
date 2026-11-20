using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformAdminAndTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_platform_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "two_factor_enabled_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "two_factor_last_step",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "two_factor_secret_cipher",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "two_factor_verified",
                table: "user_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Mevcut okullar etkin kalır (spec 0007): varsayılan true.
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "two_factor_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pending_secret_cipher = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_two_factor_challenges", x => x.id);
                    table.ForeignKey(
                        name: "fk_two_factor_challenges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenges_token_hash",
                table: "two_factor_challenges",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenges_user_id",
                table: "two_factor_challenges",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "two_factor_challenges");

            migrationBuilder.DropColumn(
                name: "is_platform_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_last_step",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_secret_cipher",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_verified",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tenants");
        }
    }
}
