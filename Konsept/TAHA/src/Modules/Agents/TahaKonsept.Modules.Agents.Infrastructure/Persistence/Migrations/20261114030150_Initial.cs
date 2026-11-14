using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agents");

            migrationBuilder.CreateTable(
                name: "agent_devices",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    agent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    os_description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    paired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lan_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    video_server_port = table.Column<int>(type: "integer", nullable: true),
                    stream_key_protected = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    capabilities_json = table.Column<string>(type: "jsonb", nullable: true),
                    token_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_pairings",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    machine_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    agent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    last_polled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    agent_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_pairings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stored_artifacts",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    relative_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    scene_index = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_artifacts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_devices_token_family_id",
                schema: "agents",
                table: "agent_devices",
                column: "token_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_devices_user_id_is_revoked",
                schema: "agents",
                table: "agent_devices",
                columns: new[] { "user_id", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_device_pairings_device_code_hash",
                schema: "agents",
                table: "device_pairings",
                column: "device_code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_pairings_expires_at_utc",
                schema: "agents",
                table: "device_pairings",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_device_pairings_user_code",
                schema: "agents",
                table: "device_pairings",
                column: "user_code");

            migrationBuilder.CreateIndex(
                name: "ix_stored_artifacts_user_id_job_id",
                schema: "agents",
                table: "stored_artifacts",
                columns: new[] { "user_id", "job_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_devices",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "device_pairings",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "stored_artifacts",
                schema: "agents");
        }
    }
}
