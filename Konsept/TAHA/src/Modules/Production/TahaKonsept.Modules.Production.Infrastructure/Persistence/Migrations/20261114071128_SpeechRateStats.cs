using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpeechRateStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "speech_rate_stats",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    speech_model_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    voice_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sample_weight = table.Column<double>(type: "double precision", nullable: false),
                    total_words = table.Column<double>(type: "double precision", nullable: false),
                    total_seconds = table.Column<double>(type: "double precision", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speech_rate_stats", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_speech_rate_stats_user_id_speech_model_key_voice_key_langua",
                schema: "production",
                table: "speech_rate_stats",
                columns: new[] { "user_id", "speech_model_key", "voice_key", "language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "speech_rate_stats",
                schema: "production");
        }
    }
}
