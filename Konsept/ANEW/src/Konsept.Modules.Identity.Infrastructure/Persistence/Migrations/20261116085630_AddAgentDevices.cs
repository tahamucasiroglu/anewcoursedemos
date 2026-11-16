using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_devices",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    agent_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_devices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_devices_user_id",
                schema: "identity",
                table: "agent_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_agent_devices_token_hash",
                schema: "identity",
                table: "agent_devices",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_devices",
                schema: "identity");
        }
    }
}
